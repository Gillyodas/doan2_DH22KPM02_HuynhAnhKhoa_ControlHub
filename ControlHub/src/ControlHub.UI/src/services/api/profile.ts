import { fetchJson } from "./client"
import type { Profile, UpdateProfileRequest } from "./types"

/**
 * Get the currently authenticated user's profile
 * Endpoint: GET /api/profile/me
 */
export async function getMyProfile(accessToken: string): Promise<Profile> {
    return fetchJson<Profile>("/api/Profile/me", {
        method: "GET",
        accessToken,
    })
}

/**
 * Update the currently authenticated user's profile
 * Endpoint: PUT /api/profile/me
 */
export async function updateMyProfile(
    data: UpdateProfileRequest,
    accessToken: string
): Promise<Profile> {
    return fetchJson<Profile>("/api/Profile/me", {
        method: "PUT",
        body: data,
        accessToken,
    })
}
