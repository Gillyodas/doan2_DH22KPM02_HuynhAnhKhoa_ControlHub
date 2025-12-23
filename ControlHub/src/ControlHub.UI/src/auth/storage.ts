import type { AuthData } from "./types"

const STORAGE_KEY = "controlhub.auth"

export function loadAuth(): AuthData | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as AuthData
    if (!parsed?.accessToken || !parsed?.refreshToken) return null
    return parsed
  } catch {
    return null
  }
}

export function saveAuth(auth: AuthData) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
}

export function clearAuth() {
  localStorage.removeItem(STORAGE_KEY)
}

export function hasAuthToken() {
  return Boolean(loadAuth()?.accessToken)
}
