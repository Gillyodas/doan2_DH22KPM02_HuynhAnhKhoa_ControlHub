export type IdentifierType = 0 | 1 | 2 | 99

export type PagedResult<T> = {
  items: T[]
  pageIndex: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type AuthData = {
  accountId: string
  username: string
  accessToken: string
  refreshToken: string
}

export type SignInRequest = {
  value: string
  password: string
  type: IdentifierType
}

export type RegisterRequest = {
  value: string
  password: string
  type: IdentifierType
}

export type RegisterAdminRequest = RegisterRequest

export type RegisterSuperAdminRequest = RegisterRequest & {
  masterKey: string
}

export type RefreshTokenRequest = {
  refreshToken: string
  accID: string
  accessToken: string
}

export type RefreshTokenResponse = {
  accessToken: string
  refreshToken: string
}

export type SignOutRequest = {
  accessToken: string
  refreshToken: string
}

export type ChangePasswordRequest = {
  curPass: string
  newPass: string
}

export type ForgotPasswordRequest = {
  value: string
  type: IdentifierType
}

export type ResetPasswordRequest = {
  token: string
  password: string
}

export type UpdateUsernameRequest = {
  username: string
}

export type UpdateUsernameResponse = {
  username: string
}

export type Permission = {
  id: string
  name: string
  description?: string
  createdAt: string
  updatedAt?: string
}

export type CreatePermissionsRequest = {
  permissions: string[]
}

export type Role = {
  id: string
  name: string
  description?: string
  permissions?: Permission[]
  createdAt: string
  updatedAt?: string
}

export type CreateRolesRequest = {
  roles: string[]
}

export type CreateRolesResponse = {
  message: string
  successCount: number
  failureCount: number
  failedRoles?: string[]
}

export type AddPermissionsForRoleRequest = {
  roleId: string
  permissionIds: string[]
}

export type AddPermissionsForRoleResponse = {
  message: string
  successCount: number
  failureCount: number
  failedRoles?: string[]
}

export type AccountDto = {
  id: string
  username: string
  roleName: string
  isActive: boolean
}

// ============================================
// User Management Types
// ============================================

export type User = {
  id: string
  accountId: string
  username: string
  email?: string
  roles: Role[]
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export type GetUsersParams = {
  pageIndex?: number
  pageSize?: number
  searchTerm?: string
}

export type UpdateUserRequest = {
  username?: string
  email?: string
}

export type AssignRoleRequest = {
  roleId: string
}

// ============================================
// Profile Types
// ============================================

export type Profile = {
  id: string
  accountId: string
  username: string
  email?: string
  displayName?: string
  avatarUrl?: string
  createdAt: string
  updatedAt?: string
}

export type UpdateProfileRequest = {
  displayName?: string
  email?: string
}

// ============================================
// Role Management Extended Types
// ============================================

export type UpdateRoleRequest = {
  name: string
  description?: string
}
