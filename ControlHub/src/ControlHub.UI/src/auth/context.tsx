import * as React from "react"

import type { AuthData, RegisterRequest, RegisterRole, SignInRequest } from "./types"
import * as api from "./api"
import type { AuthContextValue } from "./auth-context"
import { AuthContext } from "./auth-context"
import { clearAuth, loadAuth, saveAuth } from "./storage"

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = React.useState<AuthData | null>(() => loadAuth())
  
  // Expose this function to be called from axios interceptor
  const updateAuth = React.useCallback((updates: Partial<AuthData>) => {
    if (!auth) return
    const updated = { ...auth, ...updates }
    saveAuth(updated)
    setAuth(updated)
  }, [auth])

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

  const signOut = React.useCallback(() => {
    clearAuth()
    setAuth(null)
  }, [])

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

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
