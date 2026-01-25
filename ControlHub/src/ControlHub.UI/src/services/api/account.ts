import { fetchVoid, fetchJson } from "./client"
import type {
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  AccountDto,
  RegisterRequest,
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

export async function getAdminAccounts(accessToken: string): Promise<AccountDto[]> {
  return fetchJson<AccountDto[]>("/api/Account/admins", {
    method: "GET",
    accessToken,
  })
}

export async function registerAdmin(
  req: RegisterRequest,
  accessToken: string
): Promise<void> {
  return fetchVoid("/api/Auth/admins/register", {
    method: "POST",
    body: req,
    accessToken,
  })
}
