import { fetchVoid } from "./client"
import type {
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from "./types"

export async function changePassword(
  userId: string,
  req: ChangePasswordRequest,
  accessToken: string
): Promise<void> {
  return fetchVoid(`/api/Account/users/${userId}/password`, {
    method: "PATCH",
    body: req,
    accessToken,
  })
}

export async function forgotPassword(req: ForgotPasswordRequest): Promise<void> {
  return fetchVoid("/api/Account/auth/forgot-password", {
    method: "POST",
    body: req,
  })
}

export async function resetPassword(req: ResetPasswordRequest): Promise<void> {
  return fetchVoid("/api/Account/auth/reset-password", {
    method: "POST",
    body: req,
  })
}
