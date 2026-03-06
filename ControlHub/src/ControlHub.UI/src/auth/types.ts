export type IdentifierType = 0 | 1 | 2 | 99 | number

export type AuthData = {
  accountId: string | number
  username: string
  accessToken: string
  refreshToken: string
}

export type SignInRequest = {
  value: string
  password: string
  type: IdentifierType
  identifierConfigId?: string
}

export type ForgotPasswordRequest = {
  value: string
  type: IdentifierType
}

export type ResetPasswordRequest = {
  token: string
  password: string
}

export type RegisterRequest = {
  value: string
  password: string
  type: IdentifierType
  identifierConfigId?: string
}

export type RegisterSuperAdminRequest = RegisterRequest & {
  masterKey: string
}

export type RegisterRole = "User" | "Admin" | "SupperAdmin"
