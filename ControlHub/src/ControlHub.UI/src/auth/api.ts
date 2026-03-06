import type {
  AuthData,
  ForgotPasswordRequest,
  RegisterRequest,
  RegisterRole,
  RegisterSuperAdminRequest,
  ResetPasswordRequest,
  SignInRequest,
} from "./types"

const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? ""

type ProblemDetails = {
  title?: string
  status?: number
  detail?: string
  extensions?: {
    code?: string
  }
}

async function readErrorMessage(res: Response) {
  try {
    const json = (await res.json()) as ProblemDetails
    return json?.detail || json?.title || `Request failed (${res.status})`
  } catch {
    return `Request failed (${res.status})`
  }
}

async function postJson<T>(path: string, body: unknown, accessToken?: string) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : null),
    },
    body: JSON.stringify(body),
  })

  if (!res.ok) {
    throw new Error(await readErrorMessage(res))
  }

  return (await res.json()) as T
}

async function postJsonVoid(path: string, body: unknown, accessToken?: string) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : null),
    },
    body: JSON.stringify(body),
  })

  if (!res.ok) {
    throw new Error(await readErrorMessage(res))
  }
}

export async function signIn(req: SignInRequest): Promise<AuthData> {
  const data = await postJson<AuthData>("/api/Auth/auth/signin", {
    value: req.value,
    password: req.password,
    type: req.type,
    identifierConfigId: req.identifierConfigId,
  })

  return data
}

export async function register(role: RegisterRole, req: RegisterRequest, options?: { masterKey?: string; accessToken?: string }) {
  if (role === "User") {
    return postJson<{ accountId: string | number; message: string }>("/api/Auth/users/register", {
      ...req,
      identifierConfigId: req.identifierConfigId,
    })
  }

  if (role === "Admin") {
    return postJson<{ accountId: string | number; message: string }>(
      "/api/Auth/admins/register",
      {
        ...req,
        identifierConfigId: req.identifierConfigId,
      },
      options?.accessToken,
    )
  }

  const superReq: RegisterSuperAdminRequest = {
    ...req,
    masterKey: options?.masterKey ?? "",
  }

  return postJson<{ accountId: string | number; message: string }>("/api/Auth/superadmins/register", superReq)
}

export async function forgotPassword(req: ForgotPasswordRequest) {
  await postJsonVoid("/api/Account/auth/forgot-password", req)
}

export async function resetPassword(req: ResetPasswordRequest) {
  await postJsonVoid("/api/Account/auth/reset-password", req)
}
