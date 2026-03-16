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

function toCamelCase(str: string): string {
  return str.charAt(0).toLowerCase() + str.slice(1)
}

function deepCamelCase(obj: unknown): unknown {
  if (Array.isArray(obj)) return obj.map(deepCamelCase)
  if (obj !== null && typeof obj === "object") {
    return Object.fromEntries(
      Object.entries(obj as Record<string, unknown>).map(([k, v]) => [
        toCamelCase(k),
        deepCamelCase(v),
      ])
    )
  }
  return obj
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

  return deepCamelCase(await res.json()) as T
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
  return postJson<AuthData>("/api/auth/signin", {
    value: req.value,
    password: req.password,
    type: req.type,
    identifierConfigId: req.identifierConfigId,
  })
}

export async function register(role: RegisterRole, req: RegisterRequest, options?: { masterKey?: string; accessToken?: string }) {
  if (role === "User") {
    return postJson<{ accountId: string | number; message: string }>("/api/auth/users/register", {
      ...req,
      identifierConfigId: req.identifierConfigId,
    })
  }

  if (role === "Admin") {
    return postJson<{ accountId: string | number; message: string }>(
      "/api/auth/admins/register",
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

  return postJson<{ accountId: string | number; message: string }>("/api/auth/superadmins/register", superReq)
}

export async function forgotPassword(req: ForgotPasswordRequest) {
  await postJsonVoid("/api/account/auth/forgot-password", req)
}

export async function resetPassword(req: ResetPasswordRequest) {
  await postJsonVoid("/api/account/auth/reset-password", req)
}
