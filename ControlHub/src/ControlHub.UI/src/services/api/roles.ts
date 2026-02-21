import { fetchJson } from "./client"
import type {
  Role,
  PagedResult,
  CreateRolesRequest,
  CreateRolesResponse,
  UpdateRoleRequest,
} from "./types"

/**
 * Create new roles
 * Endpoint: POST /api/role/roles
 */
export async function createRoles(
  req: CreateRolesRequest,
  accessToken: string
): Promise<CreateRolesResponse> {
  return fetchJson<CreateRolesResponse>("/api/Role/roles", {
    method: "POST",
    body: req,
    accessToken,
  })
}

/**
 * Get paginated list of roles
 * Endpoint: GET /api/role
 */
export async function getRoles(
  params: {
    pageIndex?: number
    pageSize?: number
    searchTerm?: string
  },
  accessToken: string
): Promise<PagedResult<Role>> {
  const queryParams = new URLSearchParams()
  if (params.pageIndex) queryParams.append("pageIndex", params.pageIndex.toString())
  if (params.pageSize) queryParams.append("pageSize", params.pageSize.toString())
  if (params.searchTerm) queryParams.append("searchTerm", params.searchTerm)

  const queryString = queryParams.toString()
  const url = `/api/Role${queryString ? `?${queryString}` : ""}`

  return fetchJson<PagedResult<Role>>(url, {
    method: "GET",
    accessToken,
  })
}

/**
 * Get role by ID
 * Endpoint: GET /api/role/{id}
 */
export async function getRoleById(
  roleId: string,
  accessToken: string
): Promise<Role> {
  return fetchJson<Role>(`/api/Role/${roleId}`, {
    method: "GET",
    accessToken,
  })
}

/**
 * Update role
 * Endpoint: PUT /api/role/{id}
 */
export async function updateRole(
  roleId: string,
  data: UpdateRoleRequest,
  accessToken: string
): Promise<Role> {
  return fetchJson<Role>(`/api/Role/${roleId}`, {
    method: "PUT",
    body: data,
    accessToken,
  })
}

/**
 * Delete role
 * Endpoint: DELETE /api/role/{id}
 */
export async function deleteRole(
  roleId: string,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Role/${roleId}`, {
    method: "DELETE",
    accessToken,
  })
}

/**
 * Add permissions to a role
 * Endpoint: POST /api/role/roles/{id}/permissions
 */
export async function addPermissionsForRole(
  roleId: string,
  permissionIds: string[],
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Role/${roleId}/permissions`, {
    method: "PUT",
    body: permissionIds,
    accessToken,
  })
}
