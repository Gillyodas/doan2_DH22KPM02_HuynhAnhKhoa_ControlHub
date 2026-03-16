import { fetchJson, fetchVoid } from "./client"
import type {
  AuthData,
  SignInRequest,
  RegisterRequest,
  RegisterSuperAdminRequest,
  RefreshTokenRequest,
  RefreshTokenResponse,
  SignOutRequest,
} from "./types"

export async function signIn(req: SignInRequest): Promise<AuthData> {
  return fetchJson<AuthData>("/api/auth/signin", {
    method: "POST",
    body: req,
  })
}

export async function registerUser(req: RegisterRequest): Promise<{ accountId: string; message: string }> {
  return fetchJson<{ accountId: string; message: string }>("/api/auth/users/register", {
    method: "POST",
    body: req,
  })
}

export async function registerAdmin(req: RegisterRequest, accessToken: string): Promise<{ accountId: string; message: string }> {
  return fetchJson<{ accountId: string; message: string }>("/api/auth/admins/register", {
    method: "POST",
    body: req,
    accessToken,
  })
}

export async function registerSuperAdmin(req: RegisterSuperAdminRequest): Promise<{ accountId: string; message: string }> {
  return fetchJson<{ accountId: string; message: string }>("/api/auth/superadmins/register", {
    method: "POST",
    body: req,
  })
}

export async function refreshAccessToken(req: RefreshTokenRequest): Promise<RefreshTokenResponse> {
  return fetchJson<RefreshTokenResponse>("/api/auth/refresh", {
    method: "POST",
    body: req,
  })
}

export async function signOut(req: SignOutRequest, accessToken: string): Promise<void> {
  return fetchVoid("/api/auth/signout", {
    method: "POST",
    body: req,
    accessToken,
  })
}
