import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios"

import { clearAuth, loadAuth, saveAuth } from "@/auth/storage"

const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? ""

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
  return url.includes("/api/Auth/signin") || url.includes("/api/Auth/refresh")
}

async function refreshAccessToken() {
  const auth = loadAuth()
  if (!auth?.refreshToken || !auth?.accessToken || !auth?.accountId) {
    throw new Error("Missing auth data")
  }

  // NOTE: Use a plain axios call without interceptors to avoid recursion
  const res = await axios.post<RefreshResponse>(
    `${API_BASE}/api/Auth/refresh`,
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

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const auth = loadAuth()
  if (!config.headers) config.headers = new AxiosHeaders()

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
            ;(originalRequest.headers as AxiosHeaders).set("Authorization", `Bearer ${token}`)
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
      ;(originalRequest.headers as AxiosHeaders).set("Authorization", `Bearer ${accessToken}`)

      return api(originalRequest)
    } catch (refreshErr) {
      processQueue(refreshErr, null)
      clearAuth()
      window.location.replace("/login")
      return Promise.reject(refreshErr)
    } finally {
      isRefreshing = false
    }
  },
)
