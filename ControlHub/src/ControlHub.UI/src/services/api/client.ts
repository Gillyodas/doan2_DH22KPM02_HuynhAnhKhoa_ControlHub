import { toast } from "sonner"

const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? ""

function toCamelCase(str: string): string {
  return str.charAt(0).toLowerCase() + str.slice(1)
}

function deepCamelCase(obj: unknown): unknown {
  if (Array.isArray(obj)) return obj.map(deepCamelCase)
  if (obj !== null && typeof obj === "object") {
    return Object.fromEntries(
      Object.entries(obj as Record<string, unknown>).map(([k, v]) => [
        toCamelCase(k),
        deepCamelCase(v),
      ])
    )
  }
  return obj
}

type ProblemDetails = {
  title?: string
  status?: number
  detail?: string
  traceId?: string // Found at root level in recent tests
  extensions?: {
    code?: string
    traceId?: string
  }
}

async function handleResponseError(res: Response) {
  let errorMsg = `Request failed (${res.status})`
  let traceId: string | undefined

  console.log(`[Client] Handling error for status: ${res.status}`)

  try {
    const clone = res.clone()
    const text = await clone.text()
    console.log(`[Client] Raw Response Body:`, text)

    try {
      const json = JSON.parse(text) as ProblemDetails
      errorMsg = json?.detail || json?.title || errorMsg

      // Handle traceId at root OR in extensions
      traceId = json?.traceId || json?.extensions?.traceId
      console.log(`[Client] Extracted traceId:`, traceId)
    } catch {
      console.log(`[Client] Failed to parse JSON`)
    }
  } catch (e) {
    console.log(`[Client] Error reading response body:`, e)
  }

  if (traceId) {
    console.log(`[Client] Triggering Global Toast for traceId: ${traceId}`)
    toast.error("System Error Occurred", {
      description: errorMsg,
      duration: 10000, // Make it stay longer
      action: {
        label: "AI Analyze",
        onClick: () => {
          console.log(`[Client] AI Analyze clicked, navigating to ${traceId}`)
          window.location.href = `/control-hub/ai-audit?correlationId=${traceId}`
        }
      }
    })
  } else {
    console.log(`[Client] No traceId found, skipping global toast.`)
  }

  throw new Error(errorMsg)
}

export async function fetchJson<T>(
  path: string,
  options?: {
    method?: string
    body?: unknown
    accessToken?: string
    headers?: Record<string, string>
  }
): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: options?.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      ...(options?.accessToken ? { Authorization: `Bearer ${options.accessToken}` } : {}),
      ...(options?.headers ?? {}),
    },
    body: options?.body ? JSON.stringify(options.body) : undefined,
  })

  if (!res.ok) {
    await handleResponseError(res)
  }

  if (res.status === 204) {
    return undefined as T
  }

  return deepCamelCase(await res.json()) as T
}

export async function fetchVoid(
  path: string,
  options?: {
    method?: string
    body?: unknown
    accessToken?: string
    headers?: Record<string, string>
  }
): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: options?.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      ...(options?.accessToken ? { Authorization: `Bearer ${options.accessToken}` } : {}),
      ...(options?.headers ?? {}),
    },
    body: options?.body ? JSON.stringify(options.body) : undefined,
  })

  if (!res.ok) {
    await handleResponseError(res)
  }
}
