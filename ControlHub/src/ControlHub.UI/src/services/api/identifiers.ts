import { fetchJson } from "@/lib/http"

export const ValidationRuleType = {
  Required: 1,
  MinLength: 2,
  MaxLength: 3,
  Pattern: 4,
  Custom: 5,
  Range: 6,
  Email: 7,
  Phone: 8
} as const

export type ValidationRuleType = typeof ValidationRuleType[keyof typeof ValidationRuleType]

export interface ValidationRuleDto {
  type: ValidationRuleType
  parameters: Record<string, unknown>
  errorMessage?: string | null
  order: number
}

export interface IdentifierConfigDto {
  id: string
  name: string
  description: string
  isActive: boolean
  rules: ValidationRuleDto[]
}

export interface IdentifierConfigDtoFromBackend {
  id: string
  name: string
  description: string
  isActive: boolean
  rules: ValidationRuleDto[]
}

export interface CreateIdentifierConfigCommand {
  name: string
  description: string
  rules: ValidationRuleDto[]
}

export async function getIdentifierConfigs(accessToken: string): Promise<IdentifierConfigDto[]> {
  return fetchJson<IdentifierConfigDto[]>("/api/Identifier", {
    method: "GET",
    accessToken,
  })
}

export async function createIdentifierConfig(
  data: CreateIdentifierConfigCommand,
  accessToken: string
): Promise<{ id: string }> {
  return fetchJson<{ id: string }>("/api/Identifier", {
    method: "POST",
    body: data,
    accessToken,
  })
}

export async function getActiveIdentifierConfigs(includeDeactivated = false): Promise<IdentifierConfigDto[]> {
  const url = includeDeactivated ? "/api/Identifier/active?includeDeactivated=true" : "/api/Identifier/active"
  return fetchJson<IdentifierConfigDto[]>(url, {
    method: "GET",
  })
}

export async function toggleIdentifierActive(
  id: string,
  isActive: boolean,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Identifier/${id}/toggle-active`, {
    method: "PATCH",
    body: { isActive },
    accessToken,
  })
}

export async function updateIdentifierConfig(
  id: string,
  data: CreateIdentifierConfigCommand,
  accessToken: string
): Promise<void> {
  return fetchJson<void>(`/api/Identifier/${id}`, {
    method: "PUT",
    body: { id, ...data },
    accessToken,
  })
}
