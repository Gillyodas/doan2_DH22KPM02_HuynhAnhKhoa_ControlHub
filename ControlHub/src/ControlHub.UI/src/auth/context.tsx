import * as React from "react"

import type { AuthData, RegisterRequest, RegisterRole, SignInRequest } from "./types"
import * as api from "./api"
import type { AuthContextValue } from "./auth-context"
import { AuthContext } from "./auth-context"
import { clearAuth, loadAuth, saveAuth } from "./storage"
import { authApi } from "@/services/api"
import { useVisibilityRefresh } from "@/hooks/use-visibility-refresh"

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = React.useState<AuthData | null>(() => loadAuth())

  // Expose this function to be called from axios interceptor
  const updateAuth = React.useCallback((updates: Partial<AuthData>) => {
    setAuth(prev => {
      if (!prev) return prev
      const updated = { ...prev, ...updates }
      saveAuth(updated)
      return updated
    })
  }, [])

  const signIn = React.useCallback(async (req: SignInRequest) => {
    const data = await api.signIn(req)
    saveAuth(data)
    setAuth(data)
  }, [])

  const register = React.useCallback(
    async (role: RegisterRole, req: RegisterRequest, options?: { masterKey?: string }) => {
      await api.register(role, req, { masterKey: options?.masterKey, accessToken: auth?.accessToken })
    },
    [auth?.accessToken],
  )

  const signOut = React.useCallback(async () => {
    if (auth?.accessToken && auth?.refreshToken) {
      try {
        await authApi.signOut(
          {
            accessToken: auth.accessToken,
            refreshToken: auth.refreshToken,
          },
          auth.accessToken
        )
      } catch (error) {
        console.error("Failed to sign out from server:", error)
      }
    }
    clearAuth()
    setAuth(null)
  }, [auth])

  const value: AuthContextValue = React.useMemo(
    () => ({
      auth,
      isAuthenticated: Boolean(auth?.accessToken),
      signIn,
      register,
      signOut,
      updateAuth, // Expose updateAuth in context
    }),
    [auth, register, signIn, signOut, updateAuth],
  )

  // Handle token refresh when tab becomes visible
  const handleVisibilityRefresh = React.useCallback(async () => {
    if (!auth?.accessToken || !auth?.refreshToken || !auth?.accountId) return

    try {
      const result = await authApi.refreshAccessToken({
        accessToken: auth.accessToken,
        refreshToken: auth.refreshToken,
        accID: String(auth.accountId),
      })
      updateAuth({
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
      })
    } catch {
      signOut()
    }
  }, [auth, updateAuth, signOut])

  useVisibilityRefresh(handleVisibilityRefresh)

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
