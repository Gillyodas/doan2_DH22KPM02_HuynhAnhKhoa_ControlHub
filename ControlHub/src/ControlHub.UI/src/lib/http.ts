import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios"
import { jwtDecode } from "jwt-decode"

import { clearAuth, loadAuth, saveAuth } from "@/auth/storage"

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

const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? ""

/**
 * Check if JWT token is expired or expiring soon
 * @param token - JWT access token
 * @param bufferMs - Buffer time in milliseconds (default 60s)
 */
function isTokenExpired(token: string, bufferMs = 60 * 1000): boolean {
  try {
    const decoded = jwtDecode<{ exp: number }>(token)
    return decoded.exp * 1000 < Date.now() + bufferMs
  } catch {
    return true
  }
}

type RefreshResponse = {
  accessToken?: string
  refreshToken?: string
  AccessToken?: string
  RefreshToken?: string
}

type FailedQueueItem = {
  resolve: (token: string) => void
  reject: (error: unknown) => void
}

type RetriableAxiosRequestConfig = AxiosRequestConfig & {
  _retry?: boolean
}

let isRefreshing = false
let failedQueue: FailedQueueItem[] = []

function processQueue(error: unknown, token: string | null) {
  const queue = failedQueue
  failedQueue = []

  for (const p of queue) {
    if (error) {
      p.reject(error)
    } else if (token) {
      p.resolve(token)
    } else {
      p.reject(new Error("Refresh token failed"))
    }
  }
}

function isAuthEndpoint(url?: string) {
  if (!url) return false
  return url.includes("/api/auth/signin") || url.includes("/api/auth/refresh")
}

async function refreshAccessToken() {
  const auth = loadAuth()
  if (!auth?.refreshToken || !auth?.accessToken || !auth?.accountId) {
    throw new Error("Missing auth data")
  }

  // NOTE: Use a plain axios call without interceptors to avoid recursion
  const res = await axios.post<RefreshResponse>(
    `${API_BASE}/api/auth/refresh`,
    {
      refreshToken: auth.refreshToken,
      accessToken: auth.accessToken,
      accID: auth.accountId,
    },
    {
      headers: {
        "Content-Type": "application/json",
        ...(auth.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : null),
      },
    },
  )

  const data = res.data
  const accessToken = data?.accessToken ?? data?.AccessToken
  const refreshToken = data?.refreshToken ?? data?.RefreshToken

  if (!accessToken || !refreshToken) {
    throw new Error("Refresh response missing tokens")
  }

  saveAuth({ ...auth, accessToken, refreshToken })

  return { accessToken, refreshToken }
}

export const api = axios.create({
  baseURL: API_BASE,
})

api.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  // Skip auth endpoints to avoid infinite loops
  if (isAuthEndpoint(config.url)) {
    return config
  }

  if (!config.headers) config.headers = new AxiosHeaders()

  const auth = loadAuth()

  // Proactive refresh: if token is expiring soon, refresh before making request
  if (auth?.accessToken && isTokenExpired(auth.accessToken)) {
    if (isRefreshing) {
      // Wait for ongoing refresh to complete
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token: string) => {
            config.headers.set("Authorization", `Bearer ${token}`)
            resolve(config)
          },
          reject,
        })
      })
    }

    isRefreshing = true
    try {
      console.log("[http] Token expiring soon, proactively refreshing...")
      const { accessToken } = await refreshAccessToken()
      processQueue(null, accessToken)
      config.headers.set("Authorization", `Bearer ${accessToken}`)
      return config
    } catch (error) {
      processQueue(error, null)
      console.warn("[http] Proactive refresh failed, will retry on 401")
      // Let response interceptor handle it
    } finally {
      isRefreshing = false
    }
  }

  // Attach existing token if available
  if (!config.headers.get("Authorization") && auth?.accessToken) {
    config.headers.set("Authorization", `Bearer ${auth.accessToken}`)
  }

  return config
})

api.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableAxiosRequestConfig | undefined

    if (!originalRequest) {
      return Promise.reject(error)
    }

    const status = error.response?.status

    if (status !== 401 || originalRequest._retry || isAuthEndpoint(originalRequest.url)) {
      return Promise.reject(error)
    }

    originalRequest._retry = true

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token: string) => {
            if (!originalRequest.headers) {
              originalRequest.headers = new AxiosHeaders()
            }
            ; (originalRequest.headers as AxiosHeaders).set("Authorization", `Bearer ${token}`)
            resolve(api(originalRequest))
          },
          reject,
        })
      })
    }

    isRefreshing = true

    try {
      const { accessToken } = await refreshAccessToken()

      api.defaults.headers.common.Authorization = `Bearer ${accessToken}`
      processQueue(null, accessToken)

      if (!originalRequest.headers) {
        originalRequest.headers = new AxiosHeaders()
      }
      ; (originalRequest.headers as AxiosHeaders).set("Authorization", `Bearer ${accessToken}`)

      return api(originalRequest)
    } catch (refreshErr) {
      processQueue(refreshErr, null)
      clearAuth()
      window.location.replace(`${import.meta.env.VITE_BASE_URL || '/control-hub'}/login`)
      return Promise.reject(refreshErr)
    } finally {
      isRefreshing = false
    }
  },
)

// Helper functions for API calls
export async function fetchJson<T>(url: string, options: {
  method?: string
  body?: unknown
  accessToken?: string
  headers?: Record<string, string>
}): Promise<T> {
  const config: AxiosRequestConfig = {
    method: options.method || "GET",
    url: `${API_BASE}${url}`,
    headers: {
      "Content-Type": "application/json",
      ...options.headers,
    },
  }

  if (options.accessToken) {
    config.headers = {
      ...config.headers,
      Authorization: `Bearer ${options.accessToken}`,
    }
  }

  if (options.body) {
    config.data = options.body
  }

  try {
    const response = await api.request<T>(config)
    return deepCamelCase(response.data) as T
  } catch (error: unknown) {
    // Parse validation errors from backend
    if (error && typeof error === 'object' && 'response' in error) {
      const axiosError = error as { response?: { data?: { errors?: Record<string, unknown>; title?: string } } }

      if (axiosError.response?.data?.errors) {
        const validationErrors = axiosError.response.data.errors
        const errorMessages = Object.entries(validationErrors)
          .map(([field, messages]) => {
            const msgs = Array.isArray(messages) ? messages : [messages]
            return `${field}: ${msgs.join(", ")}`
          })
          .join("; ")
        throw new Error(errorMessages)
      }

      if (axiosError.response?.data?.title) {
        throw new Error(axiosError.response.data.title)
      }
    }

    throw error
  }
}

export async function fetchVoid(url: string, options: {
  method?: string
  body?: unknown
  accessToken?: string
  headers?: Record<string, string>
}): Promise<void> {
  await fetchJson(url, options)
}
