import { useState, useRef, useEffect, Component, ReactNode } from 'react';
import ReactMarkdown from 'react-markdown';
import { Send, Loader2, CheckCircle, XCircle, History, RotateCcw, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useV3Audit } from '@/hooks/use-v3-audit';
import { V3InvestigateResponse } from '@/services/api/audit';
import { AuditHistoryEntry } from '@/lib/audit-history';

// ── Error Boundary — prevents ReactMarkdown crashes from causing a black screen ──
interface EBState { hasError: boolean; message: string }
class MarkdownErrorBoundary extends Component<{ children: ReactNode }, EBState> {
  state: EBState = { hasError: false, message: '' };

  static getDerivedStateFromError(error: unknown): EBState {
    return { hasError: true, message: error instanceof Error ? error.message : String(error) };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
          Failed to render response. Raw content may contain invalid markdown.
          <details className="mt-1 text-xs opacity-70">
            <summary>Details</summary>
            {this.state.message}
          </details>
        </div>
      );
    }
    return this.props.children;
  }
}

// ── Safe helpers — guard every field that could be missing on a partial API response ──
function safeString(value: unknown): string {
  if (typeof value === 'string') return value;
  if (value == null) return '';
  return String(value);
}

function safeArray<T>(value: unknown): T[] {
  return Array.isArray(value) ? (value as T[]) : [];
}

function safeConfidence(value: unknown): number {
  const n = typeof value === 'number' ? value : parseFloat(String(value));
  return isNaN(n) ? 0 : Math.max(0, Math.min(1, n));
}

// ── Result display — extracted so it can be reused for history restores ──
function InvestigationResult({
  result,
  trace,
}: {
  result: V3InvestigateResponse;
  trace: { events: unknown[]; summary: string } | null;
}) {
  const [showTrace, setShowTrace] = useState(false);

  const plan = safeArray<string>(result.plan);
  const executionResults = safeArray<string>(result.executionResults);
  const confidence = safeConfidence(result.confidence);
  const answer = safeString(result.answer);
  const passed = result.verificationPassed === true;
  const iterations = typeof result.iterations === 'number' ? result.iterations : 0;

  return (
    <div className="space-y-4">
      {/* Status Overview */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            {passed ? (
              <CheckCircle className="h-5 w-5 text-green-500" />
            ) : (
              <XCircle className="h-5 w-5 text-red-500" />
            )}
            Kết quả điều tra
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
            <div className="text-center p-3 bg-muted rounded-lg">
              <p className="text-2xl font-bold">{iterations}</p>
              <p className="text-sm text-muted-foreground">Iterations</p>
            </div>
            <div className="text-center p-3 bg-muted rounded-lg">
              <p className="text-2xl font-bold">{plan.length}</p>
              <p className="text-sm text-muted-foreground">Steps</p>
            </div>
            <div className="text-center p-3 bg-muted rounded-lg">
              <p className="text-2xl font-bold">{(confidence * 100).toFixed(0)}%</p>
              <p className="text-sm text-muted-foreground">Confidence</p>
            </div>
            <div className="text-center p-3 bg-muted rounded-lg">
              <Badge variant={passed ? 'default' : 'destructive'}>
                {passed ? 'Passed' : 'Failed'}
              </Badge>
              <p className="text-sm text-muted-foreground mt-1">Verification</p>
            </div>
          </div>

          {/* Confidence bar */}
          <div className="mb-4">
            <div className="flex justify-between mb-1">
              <span className="text-sm">Confidence Score</span>
              <span className="text-sm font-medium">{(confidence * 100).toFixed(1)}%</span>
            </div>
            <div className="w-full bg-muted rounded-full h-2">
              <div
                className="bg-primary h-2 rounded-full transition-all"
                style={{ width: `${confidence * 100}%` }}
              />
            </div>
          </div>

          {/* Backend error field (agent-level, not HTTP) */}
          {result.error && (
            <div className="rounded border border-yellow-200 bg-yellow-50 p-2 text-xs text-yellow-800 dark:bg-yellow-950 dark:text-yellow-200">
              Agent error: {result.error}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Execution Plan */}
      {plan.length > 0 && (
        <Collapsible defaultOpen>
          <Card>
            <CardHeader>
              <CollapsibleTrigger className="flex w-full items-center justify-between">
                <CardTitle className="text-base">📋 Execution Plan ({plan.length} steps)</CardTitle>
              </CollapsibleTrigger>
            </CardHeader>
            <CollapsibleContent>
              <CardContent>
                <ol className="list-decimal list-inside space-y-2">
                  {plan.map((step, i) => (
                    <li key={i} className="text-sm">{safeString(step)}</li>
                  ))}
                </ol>
              </CardContent>
            </CollapsibleContent>
          </Card>
        </Collapsible>
      )}

      {/* Answer */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">📝 Answer</CardTitle>
        </CardHeader>
        <CardContent>
          <MarkdownErrorBoundary>
            <div className="prose dark:prose-invert max-w-none">
              <ReactMarkdown>{answer || '_No answer was returned by the agent._'}</ReactMarkdown>
            </div>
          </MarkdownErrorBoundary>
        </CardContent>
      </Card>

      {/* Execution Results (collapsed by default to keep screen clean) */}
      {executionResults.length > 0 && (
        <Collapsible>
          <Card>
            <CardHeader>
              <CollapsibleTrigger className="flex w-full items-center justify-between">
                <CardTitle className="text-base">🔍 Execution Results ({executionResults.length})</CardTitle>
              </CollapsibleTrigger>
            </CardHeader>
            <CollapsibleContent>
              <CardContent className="space-y-3">
                {executionResults.map((r, i) => (
                  <MarkdownErrorBoundary key={i}>
                    <div className="prose prose-sm dark:prose-invert max-w-none border-l-2 pl-3">
                      <ReactMarkdown>{safeString(r)}</ReactMarkdown>
                    </div>
                  </MarkdownErrorBoundary>
                ))}
              </CardContent>
            </CollapsibleContent>
          </Card>
        </Collapsible>
      )}

      {/* Agent Trace */}
      {trace && (
        <Collapsible open={showTrace} onOpenChange={setShowTrace}>
          <Card>
            <CardHeader>
              <CollapsibleTrigger className="flex w-full items-center justify-between">
                <CardTitle className="flex items-center gap-2 text-base">
                  <History className="h-4 w-4" />
                  Agent Trace ({safeArray(trace.events).length} events)
                </CardTitle>
              </CollapsibleTrigger>
            </CardHeader>
            <CollapsibleContent>
              <CardContent>
                <div className="bg-muted rounded-lg p-4 font-mono text-xs overflow-x-auto">
                  <pre>{safeString(trace.summary)}</pre>
                </div>
              </CardContent>
            </CollapsibleContent>
          </Card>
        </Collapsible>
      )}
    </div>
  );
}

function relativeTime(iso: string): string {
  const mins = Math.floor((Date.now() - new Date(iso).getTime()) / 60_000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  if (mins < 1440) return `${Math.floor(mins / 60)}h ago`;
  return `${Math.floor(mins / 1440)}d ago`;
}

// ── History Combobox ──
function HistorySelect({
  history,
  selectedId,
  onSelect,
  onDelete,
  onClearAll,
}: {
  history: AuditHistoryEntry[];
  selectedId: string;
  onSelect: (entry: AuditHistoryEntry) => void;
  onDelete: (id: string) => void;
  onClearAll: () => void;
}) {
  return (
    <Card>
      <CardHeader className="py-3 px-4">
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2 text-sm font-medium">
            <History className="h-4 w-4" />
            Investigation History
            {history.length > 0 && (
              <Badge variant="secondary" className="ml-1 text-xs">{history.length}/10</Badge>
            )}
          </CardTitle>
          {history.length > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 px-2 text-xs text-muted-foreground hover:text-destructive"
              onClick={onClearAll}
            >
              Clear all
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="px-4 pb-4 pt-0 space-y-3">
        {history.length === 0 && (
          <p className="text-xs text-muted-foreground py-2">
            No investigations saved yet. Run an investigation above to save it here.
          </p>
        )}
        {history.length > 0 && (
        <div className="space-y-3">
        {/* Controlled Select — value kept in sync with parent so re-renders don't reset it */}
        <Select
          value={selectedId}
          onValueChange={(id) => {
            const entry = history.find((e) => e.id === id);
            if (entry) onSelect(entry);
          }}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select a past investigation to view…" />
          </SelectTrigger>
          <SelectContent>
            {history.map((entry) => {
              const conf = typeof entry.result.confidence === 'number'
                ? `${(entry.result.confidence * 100).toFixed(0)}%`
                : '—';
              return (
                <SelectItem key={entry.id} value={entry.id}>
                  {`[${relativeTime(entry.savedAt)} · ${conf}] ${entry.query.length > 55 ? entry.query.slice(0, 55) + '…' : entry.query}`}
                </SelectItem>
              );
            })}
          </SelectContent>
        </Select>

        {/* Clickable rows — clicking the row restores; trash icon deletes only */}
        <div className="space-y-1">
          {history.map((entry) => {
            const conf = typeof entry.result.confidence === 'number'
              ? `${(entry.result.confidence * 100).toFixed(0)}%`
              : '—';
            const isActive = entry.id === selectedId;
            return (
              <div
                key={entry.id}
                role="button"
                tabIndex={0}
                onClick={() => onSelect(entry)}
                onKeyDown={(e) => e.key === 'Enter' && onSelect(entry)}
                className={`flex cursor-pointer items-center gap-2 rounded border px-2 py-1.5 text-xs transition-colors
                  ${isActive
                    ? 'border-primary bg-primary/10 text-foreground'
                    : 'bg-muted/30 hover:bg-muted/60 text-muted-foreground'
                  }`}
              >
                <span
                  className={`h-2 w-2 flex-shrink-0 rounded-full ${
                    entry.result.verificationPassed ? 'bg-green-500' : 'bg-red-400'
                  }`}
                />
                <span className="flex-1 truncate" title={entry.query}>
                  {entry.query}
                </span>
                <span className="flex-shrink-0 tabular-nums">
                  {relativeTime(entry.savedAt)} · {conf}
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-5 w-5 flex-shrink-0 hover:text-destructive"
                  onClick={(e) => { e.stopPropagation(); onDelete(entry.id); }}
                  title="Delete"
                >
                  <Trash2 className="h-3 w-3" />
                </Button>
              </div>
            );
          })}
        </div>
        </div>
        )}
      </CardContent>
    </Card>
  );
}

// ── Main Panel ──
export function V3InvestigationPanel() {
  const [query, setQuery] = useState('');
  const [correlationId, setCorrelationId] = useState('');
  const [selectedHistoryId, setSelectedHistoryId] = useState('');
  const resultRef = useRef<HTMLDivElement>(null);

  const {
    result,
    trace,
    isLoading,
    error,
    history,
    investigate,
    fetchTrace,
    reset,
    restoreFromHistory,
    deleteHistoryEntry,
    clearHistory,
  } = useV3Audit();

  // Scroll result into view whenever it changes
  useEffect(() => {
    if (result && !isLoading) {
      resultRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }, [result, isLoading]);

  const handleInvestigate = async () => {
    if (!query.trim()) return;
    setSelectedHistoryId('');
    await investigate({
      query: query.trim(),
      correlationId: correlationId.trim() || undefined,
    });
    await fetchTrace();
  };

  const handleRestoreFromHistory = (entry: AuditHistoryEntry) => {
    setSelectedHistoryId(entry.id);
    restoreFromHistory(entry);
  };

  const handleDeleteEntry = (id: string) => {
    if (selectedHistoryId === id) setSelectedHistoryId('');
    deleteHistoryEntry(id);
  };

  const handleClearHistory = () => {
    setSelectedHistoryId('');
    clearHistory();
  };

  const handleReset = () => {
    setQuery('');
    setCorrelationId('');
    setSelectedHistoryId('');
    reset();
  };

  return (
    <div className="space-y-6">
      {/* Input Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            🤖 V3.0 Agentic Investigation
            <Badge variant="outline" className="ml-2">Plan → Execute → Verify → Reflect</Badge>
          </CardTitle>
          <CardDescription>
            AI Agent sẽ phân tích câu hỏi, tạo kế hoạch, thực thi và tự đánh giá kết quả.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Textarea
            placeholder="Nhập câu hỏi để Agent điều tra... (VD: Tại sao login bị lỗi 401?)"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="min-h-[100px]"
          />
          <div className="flex gap-4">
            <Input
              placeholder="Correlation ID (optional)"
              value={correlationId}
              onChange={(e) => setCorrelationId(e.target.value)}
              className="flex-1"
            />
            <Button onClick={handleInvestigate} disabled={isLoading || !query.trim()}>
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Đang điều tra...
                </>
              ) : (
                <>
                  <Send className="mr-2 h-4 w-4" />
                  Bắt đầu
                </>
              )}
            </Button>
            {result && (
              <Button variant="outline" onClick={handleReset}>
                <RotateCcw className="mr-2 h-4 w-4" />
                Reset
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* History Combobox */}
      <HistorySelect
        history={history}
        selectedId={selectedHistoryId}
        onSelect={handleRestoreFromHistory}
        onDelete={handleDeleteEntry}
        onClearAll={handleClearHistory}
      />

      {/* Error */}
      {error && (
        <Card className="border-red-500 bg-red-50 dark:bg-red-950">
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-red-600 dark:text-red-400">
              <XCircle className="h-5 w-5" />
              <span className="font-medium">Lỗi: {error}</span>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Loading */}
      {isLoading && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex flex-col items-center gap-4 py-8">
              <Loader2 className="h-12 w-12 animate-spin text-primary" />
              <div className="text-center">
                <p className="font-medium">Agent đang điều tra...</p>
                <p className="text-sm text-muted-foreground">Plan → Execute → Verify → Reflect</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Result */}
      {result && !isLoading && (
        <div ref={resultRef}>
          {selectedHistoryId && (
            <p className="mb-2 text-xs text-muted-foreground flex items-center gap-1">
              <History className="h-3 w-3" />
              Restored from history
            </p>
          )}
          <InvestigationResult result={result} trace={trace} />
        </div>
      )}
    </div>
  );
}

export default V3InvestigationPanel;
