import type { IdentifierType } from "./types"

export const EMAIL_REGEX = /^(\w+(?:[.+-]\w+)*)@(\w+(?:[.-]\w+)*\.[a-z]{2,})$/i
export const PHONE_REGEX = /^\+?[1-9]\d{7,14}$/

export function validateIdentifierValue(type: IdentifierType, value: string) {
  const trimmed = value.trim()
  if (!trimmed) return "Identify is required"

  if (type === 0) {
    if (!EMAIL_REGEX.test(trimmed)) return "Email is invalid"
  }

  if (type === 1) {
    const normalized = trimmed.replace(/\s+/g, "").replace(/-/g, "")
    if (!PHONE_REGEX.test(normalized)) return "Phone number is invalid"
  }

  return null
}

export function detectIdentifierType(value: string): IdentifierType {
  const trimmed = value.trim()
  if (EMAIL_REGEX.test(trimmed)) return 0

  const normalized = trimmed.replace(/\s+/g, "").replace(/-/g, "")
  if (PHONE_REGEX.test(normalized)) return 1

  return 2
}
