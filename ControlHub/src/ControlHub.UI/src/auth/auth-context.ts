import * as React from "react"

import type { AuthData, RegisterRequest, RegisterRole, SignInRequest } from "./types"

export type AuthContextValue = {
  auth: AuthData | null
  isAuthenticated: boolean
  signIn: (req: SignInRequest) => Promise<void>
  register: (role: RegisterRole, req: RegisterRequest, options?: { masterKey?: string }) => Promise<void>
  signOut: () => void
  updateAuth: (updates: Partial<AuthData>) => void
}

export const AuthContext = React.createContext<AuthContextValue | null>(null)
