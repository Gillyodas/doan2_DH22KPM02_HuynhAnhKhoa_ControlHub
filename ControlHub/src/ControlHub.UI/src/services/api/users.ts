import { fetchJson } from "./client"
import type {
  User,
  PagedResult,
  GetUsersParams,
  UpdateUserRequest,
  UpdateUsernameRequest,
  UpdateUsernameResponse,
} from "./types"

/**
 * Get paginated list of users
 * Endpoint: GET /api/user
 */
export async function getUsers(
  params: GetUsersParams,
  accessToken: string
): Promise<PagedResult<User>> {
  const queryParams = new URLSearchParams()
  if (params.pageIndex) queryParams.append("pageIndex", params.pageIndex.toString())
  if (params.pageSize) queryParams.append("pageSize", params.pageSize.toString())
  if (params.searchTerm) queryParams.append("searchTerm", params.searchTerm)

  const queryString = queryParams.toString()
  const url = `/api/User${queryString ? `?${queryString}` : ""}`

  return fetchJson<PagedResult<User>>(url, {
    method: "GET",
    accessToken,
  })
}

/**
 * Get user by ID
 * Endpoint: GET /api/user/{id}
 */
export async function getUserById(
  userId: string,
  accessToken: string
): Promise<User> {
  return fetchJson<User>(`/api/User/${userId}`, {
    method: "GET",
    accessToken,
  })
}

/**
 * Update user details
 * Endpoint: PUT /api/user/{id}
 */
export async function updateUser(
  userId: string,
  data: UpdateUserRequest,
  accessToken: string
): Promise<User> {
  return fetchJson<User>(`/api/User/${userId}`, {
    method: "PUT",
    body: data,
    accessToken,
  })
}

/**
 * Update username (legacy function preserved for backward compatibility)
 * Endpoint: PATCH /api/user/users/{id}/username
 */
export async function updateUsername(
  userId: string,
  req: UpdateUsernameRequest,
  accessToken: string
): Promise<UpdateUsernameResponse> {
  return fetchJson<UpdateUsernameResponse>(`/api/User/users/${userId}/username`, {
    method: "PATCH",
    body: req,
    accessToken,
  })
}

/**
 * Delete user
 * Endpoint: DELETE /api/user/{id}
 */
export async function deleteUser(
  userId: string,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/User/${userId}`, {
    method: "DELETE",
    accessToken,
  })
}

/**
 * Assign role to user
 * Endpoint: POST /api/role/users/{userId}/assign/{roleId}
 */
export async function assignRoleToUser(
  userId: string,
  roleId: string,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Role/users/${userId}/assign/${roleId}`, {
    method: "POST",
    accessToken,
  })
}

/**
 * Remove role from user
 * Endpoint: DELETE /api/role/users/{userId}/roles/{roleId}
 */
export async function removeRoleFromUser(
  userId: string,
  roleId: string,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Role/users/${userId}/roles/${roleId}`, {
    method: "DELETE",
    accessToken,
  })
}
