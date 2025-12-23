import * as React from "react"
import { useNavigate } from "react-router-dom"

import { Button } from "@/components/ui/button"
import * as api from "@/auth/api"
import { detectIdentifierType, validateIdentifierValue } from "@/auth/validators"

function inputClassName(hasError: boolean) {
  return [
    "h-10 w-full rounded-md border bg-zinc-950 px-3 text-sm text-zinc-100",
    "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1",
    hasError ? "border-red-500/70 focus-visible:ring-red-500/60" : "border-zinc-700 focus-visible:ring-zinc-500",
  ].join(" ")
}

export function ForgotPasswordPage() {
  const navigate = useNavigate()

  const [value, setValue] = React.useState("")
  const [submitting, setSubmitting] = React.useState(false)
  const [touched, setTouched] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)
  const [success, setSuccess] = React.useState(false)

  const identifierType = React.useMemo(() => detectIdentifierType(value), [value])
  const identifyError = React.useMemo(() => validateIdentifierValue(identifierType, value), [identifierType, value])

  const canSubmit = !identifyError

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setTouched(true)
    setError(null)

    if (!canSubmit) return

    setSubmitting(true)
    try {
      await api.forgotPassword({ value: value.trim(), type: identifierType })
      setSuccess(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Request failed")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-6">
      <div className="w-full max-w-md rounded-xl border border-zinc-800 bg-zinc-900 overflow-hidden">
        <div className="p-6 border-b border-zinc-800">
          <h1 className="text-xl font-semibold text-zinc-100">ControlHub</h1>
          <p className="text-sm text-zinc-400 mt-1">Forgot your password</p>
        </div>

        <div className="p-6">
          {error ? (
            <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">{error}</div>
          ) : null}

          {success ? (
            <div className="mb-4 rounded-md border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200">
              If the account exists, we have sent a reset token. Please check your email/phone.
            </div>
          ) : null}

          <form onSubmit={onSubmit} className="space-y-4">
            <div>
              <label className="block text-sm text-zinc-300 mb-1">Identify</label>
              <input
                value={value}
                onChange={(e) => setValue(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Email / Phone / Username"
                className={inputClassName(Boolean(touched && identifyError))}
              />
              {touched && identifyError ? (
                <p className="mt-1 text-xs text-red-300">{identifyError}</p>
              ) : (
                <p className="mt-1 text-xs text-zinc-500">
                  Detected type: {identifierType === 0 ? "Email" : identifierType === 1 ? "Phone" : "Username"}
                </p>
              )}
            </div>

            <Button
              type="submit"
              disabled={!canSubmit || submitting}
              className="w-full bg-white text-black hover:bg-zinc-200 disabled:opacity-50"
            >
              {submitting ? "Sending..." : "Send reset token"}
            </Button>

            <div className="flex items-center justify-between">
              <Button
                type="button"
                variant="link"
                className="px-0 text-zinc-200"
                onClick={() => navigate("/login")}
              >
                Back to login
              </Button>
              <Button
                type="button"
                variant="link"
                className="px-0 text-zinc-200"
                onClick={() => navigate("/reset-password")}
              >
                I already have a token
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
