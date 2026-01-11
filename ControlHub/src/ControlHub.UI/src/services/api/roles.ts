import { fetchJson } from "./client"
import type {
  Role,
  PagedResult,
  CreateRolesRequest,
  CreateRolesResponse,
  AddPermissionsForRoleRequest,
  AddPermissionsForRoleResponse,
} from "./types"

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

export async function addPermissionsForRole(
  roleId: string,
  req: AddPermissionsForRoleRequest,
  accessToken: string
): Promise<AddPermissionsForRoleResponse> {
  return fetchJson<AddPermissionsForRoleResponse>(`/api/Role/roles/${roleId}/permissions`, {
    method: "POST",
    body: req,
    accessToken,
  })
}

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
