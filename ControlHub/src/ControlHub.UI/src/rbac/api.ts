type CreateRoleInput = {
  name: string
  description: string
  permissionIds: string[]
}

type CreateRolesResponse = {
  message: string
  successCount: number
  failureCount: number
  failedRoles?: string[]
}

type CreatePermissionInput = {
  code: string
  description?: string
}

type PagedResult<T> = {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

type ApiPermission = {
  id: string
  code: string
  description: string
}

type ApiRole = {
  id: string
  name: string
  description: string
  isActive?: boolean
  permissions?: ApiPermission[]
}

import { api } from "@/lib/http"

type ProblemDetails = {
  title?: string
  status?: number
  detail?: string
  extensions?: {
    code?: string
  }
}

function readAxiosErrorMessage(e: unknown) {
  const ax = e as { response?: { status?: number; data?: ProblemDetails } }
  const status = ax?.response?.status
  const data = ax?.response?.data

  return data?.detail || data?.title || (status ? `Request failed (${status})` : "Request failed")
}

async function postJson<T>(path: string, body: unknown, accessToken: string) {
  try {
    const res = await api.post<T>(path, body, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    })
    return res.data
  } catch (e) {
    throw new Error(readAxiosErrorMessage(e))
  }
}

async function getJson<T>(path: string, accessToken: string) {
  try {
    const res = await api.get<T>(path, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    })
    return res.data
  } catch (e) {
    throw new Error(readAxiosErrorMessage(e))
  }
}

export async function createRoles(roles: CreateRoleInput[], accessToken: string): Promise<CreateRolesResponse> {
  return postJson<CreateRolesResponse>("/api/Role/roles", { roles }, accessToken)
}

export async function createPermissions(permissions: CreatePermissionInput[], accessToken: string): Promise<void> {
  await postJson<void>("/api/Permission/permissions", { permissions }, accessToken)
}

export async function addPermissionsForRole(roleId: string, permissionIds: string[], accessToken: string) {
  return postJson<{ message: string; successCount: number; failureCount: number; failedRoles?: string[] }>(
    `/api/Role/roles/${roleId}/permissions`,
    { permissionIds },
    accessToken,
  )
}

export async function getRoles(
  accessToken: string,
  opts?: { pageIndex?: number; pageSize?: number; searchTerm?: string },
): Promise<PagedResult<ApiRole>> {
  const pageIndex = opts?.pageIndex ?? 1
  const pageSize = opts?.pageSize ?? 1000
  const searchTerm = opts?.searchTerm

  const params = new URLSearchParams({
    pageIndex: String(pageIndex),
    pageSize: String(pageSize),
  })
  if (searchTerm) params.set("searchTerm", searchTerm)

  return getJson<PagedResult<ApiRole>>(`/api/Role?${params.toString()}`, accessToken)
}

export async function getPermissions(
  accessToken: string,
  opts?: { pageIndex?: number; pageSize?: number; searchTerm?: string },
): Promise<PagedResult<ApiPermission>> {
  const pageIndex = opts?.pageIndex ?? 1
  const pageSize = opts?.pageSize ?? 1000
  const searchTerm = opts?.searchTerm

  const params = new URLSearchParams({
    pageIndex: String(pageIndex),
    pageSize: String(pageSize),
  })
  if (searchTerm) params.set("searchTerm", searchTerm)

  return getJson<PagedResult<ApiPermission>>(`/api/Permission?${params.toString()}`, accessToken)
}
