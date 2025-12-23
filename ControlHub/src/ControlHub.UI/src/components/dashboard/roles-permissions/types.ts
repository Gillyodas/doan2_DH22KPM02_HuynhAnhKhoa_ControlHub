export type Permission = {
  id: string
  code: string
  description: string
}

export type Role = {
  id: string
  name: string
  description: string
  permissionIds: string[]
}

export type RoleDraft = {
  name: string
  description: string
  permissionIds: string[]
}

export type PermissionDraft = {
  code: string
  description: string
}

export type ToastState = {
  id: string
  title: string
  description?: string
  variant?: "success" | "error" | "info"
}
