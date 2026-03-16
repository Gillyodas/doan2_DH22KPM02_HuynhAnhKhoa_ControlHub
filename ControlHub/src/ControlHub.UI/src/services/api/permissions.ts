import { fetchJson, fetchVoid } from "./client"
import type {
  Permission,
  PagedResult,
  CreatePermissionsRequest,
} from "./types"

export async function createPermissions(
  req: CreatePermissionsRequest,
  accessToken: string
): Promise<void> {
  return fetchVoid("/api/permission/permissions", {
    method: "POST",
    body: req,
    accessToken,
  })
}

export async function getPermissions(
  params: {
    pageIndex?: number
    pageSize?: number
    searchTerm?: string
  },
  accessToken: string
): Promise<PagedResult<Permission>> {
  const queryParams = new URLSearchParams()
  if (params.pageIndex) queryParams.append("pageIndex", params.pageIndex.toString())
  if (params.pageSize) queryParams.append("pageSize", params.pageSize.toString())
  if (params.searchTerm) queryParams.append("searchTerm", params.searchTerm)

  const queryString = queryParams.toString()
  const url = `/api/permission${queryString ? `?${queryString}` : ""}`

  return fetchJson<PagedResult<Permission>>(url, {
    method: "GET",
    accessToken,
  })
}
