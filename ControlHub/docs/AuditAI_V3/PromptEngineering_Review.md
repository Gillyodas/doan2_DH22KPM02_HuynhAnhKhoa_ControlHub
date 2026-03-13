# AuditAgentV3 — Prompt Engineering Review

**Date:** 2026-03-13
**Scope:** `Infrastructure/AI/V3/Agentic/Nodes/` — PlannerNode, ExecutorNode, VerifierNode, ReflectorNode

---

## Problem Statement

The AI responses from `AuditAgentV3` are too general and fail to identify the core issue or
cause-and-effect relationship of the problem being investigated.

---

## Root Cause Analysis of Prompt Deficiencies

### 1. ExecutorNode — Critical (Primary Cause)

#### Output Field Leakage
The prompt contains:
```
"IMPORTANT: Your 'solution' field MUST contain the final diagnosis.
Your 'steps' field should contain the 3 sections above as separate items."
```
The LLM has no concept of `solution`/`steps`/`explanation` — these are `ReasoningResult` C# properties.
Unless `IReasoningModel` explicitly parses structured JSON with a defined schema, this instruction is
meaningless to the model and results in generic or empty structured output.

#### No Causal Chain Enforcement
The prompt asks *"WHY did this happen? Quote specific logs."* — but does not force the model to
trace a causal chain. Without explicit structure, the model produces associative summaries rather
than `trigger → cascade → terminal failure` reasoning.

#### Output-to-Code Mapping Mismatch
The prompt says "3 sections in `steps`", but the code maps:
- `analysis.Solution` → `## Problem Summary`
- `analysis.Explanation` → `## Root Cause Analysis`
- `analysis.Steps` → `## Recommendation`

The code expects three sections in three *different* fields, but the prompt instructs the LLM to
put all three into `steps`. This structural contradiction causes the fallback path
(`analysis.Solution == "Partial Diagnosis"`) to fire frequently.

---

### 2. PlannerNode — Weak Hypothesis Structure

```
"Create a detailed technical investigation plan for: {query}."
```

This is a generic instruction with no causal framing. The resulting plan steps are generic headings
("Review logs", "Check endpoints") rather than falsifiable hypotheses
("Hypothesis: 401 errors caused by expired JWT tokens — verify by comparing token issuance
timestamps against error timestamps").

Pre-retrieved documents are passed to the planner but there is **no instruction to analyse them**
during planning. The planner ignores available evidence.

---

### 3. ReflectorNode — Prompt/Logic Disconnect

The prompt asks:
```
"3. Whether it's worth retrying (considering iteration X/Y)"
```
The retry decision is never parsed from the LLM's answer. The code uses `result.Confidence > 0.5f`
as the retry gate — but `Confidence` is a self-assessed certainty score, not an explicit retry
recommendation. The prompt elicits information the code then ignores.

---

## Proposed Fixes

### ExecutorNode — Highest Priority

Replace generic field-name instructions with explicit section delimiters and enforced
chain-of-thought:

```
## Your Task:
Analyze the evidence below and produce a structured diagnosis.

Think step-by-step through the causal chain BEFORE writing your final answer:
  a) What was the first observable symptom? (highest-severity log entry)
  b) What event triggered it? (look for preceding WARNINGs or state changes)
  c) What is the root cause? (the underlying condition that allowed the trigger)

Then write your final diagnosis using EXACTLY these section headers:

### PROBLEM_SUMMARY
[One sentence: what failed, exact error code, HTTP status, endpoint]

### ROOT_CAUSE
[Causal chain: Trigger → Cascading effect → Terminal failure.
Quote the specific log entries (with timestamps and log levels) that prove each link.]

### RECOMMENDATION
[Numbered steps to resolve. Reference runbook if available.]

CONSTRAINT: Only cite error codes, endpoints, and status codes from the
Auto-Extracted Evidence above. Do not introduce values not present in the evidence.
```

Update output parsing to extract sections by header rather than relying on field mapping:

```csharp
var rawResponse = analysis.RawResponse ?? analysis.Solution;
var problemSummary = ExtractSection(rawResponse, "PROBLEM_SUMMARY");
var rootCause      = ExtractSection(rawResponse, "ROOT_CAUSE");
var recommendation = ExtractSection(rawResponse, "RECOMMENDATION");
```

---

### PlannerNode

Inject pre-retrieved evidence into context and mandate hypothesis-driven steps:

```
You are a senior incident investigator.

Query: {enhancedQuery}

## Pre-Retrieved Evidence (use to form hypotheses):
{top 5 docs, truncated to 200 chars each}

Create an investigation plan where each step is a FALSIFIABLE HYPOTHESIS in the form:
"Hypothesis: [suspected cause] — Evidence to check: [specific log pattern/field]
 — Confirms if: [expected observation] — Refutes if: [disconfirming observation]"

The final step MUST be 'Root Cause Synthesis' that chains confirmed hypotheses
into a single causal narrative.
```

---

### ReflectorNode

Add explicit `RETRY_CONFIDENCE` token so the retry gate reads from the model's answer:

```
Answer these questions:
1. WHAT WENT WRONG: What specific gap caused the failure?
2. CORRECTION: What different approach should be taken on retry?
3. CONFIDENCE IN RETRY (0.0–1.0): How likely is a retry to succeed given the available evidence?
   (Output as a float on its own line prefixed with "RETRY_CONFIDENCE:")

Note: A retry will only occur if RETRY_CONFIDENCE > 0.5 and iterations remain.
```

Parse the token in `ReflectAsync`:

```csharp
var confidenceLine = result.Solution.Split('\n')
    .FirstOrDefault(l => l.TrimStart().StartsWith("RETRY_CONFIDENCE:"));
if (float.TryParse(confidenceLine?.Split(':')[1].Trim(), out var parsedConf))
    shouldRetry = parsedConf > 0.5f && state.Iteration < state.MaxIterations - 1;
```

---

## Summary

| Node | Problem | Fix |
|---|---|---|
| **Executor** | Internal field names in prompt; no causal chain; output/code mapping mismatch | Section delimiters; explicit chain-of-thought; parse by header |
| **Planner** | Generic steps; ignores pre-retrieved evidence | Hypothesis-driven format; inject evidence into prompt |
| **Reflector** | Retry decision not parsed; confidence used as proxy | Explicit `RETRY_CONFIDENCE:` token |
| **All** | No system-level role in Planner/Reflector | Add consistent role framing |

The single highest-leverage change is fixing the **Executor's causal chain enforcement** and
removing the **field-name leakage** — these two issues alone explain why the model produces
generic summaries instead of identifying root causes.
