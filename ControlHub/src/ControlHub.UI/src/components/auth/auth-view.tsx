import * as React from "react"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/auth/use-auth"
import type { IdentifierType, RegisterRole } from "@/auth/types"
import { detectIdentifierType, validateIdentifierValue } from "@/auth/validators"
import { Eye, EyeOff } from "lucide-react"
import { useNavigate } from "react-router-dom"

type Mode = "signin" | "register"

const IDENTIFIER_OPTIONS: Array<{ label: string; value: IdentifierType }> = [
  { label: "Email", value: 0 },
  { label: "Phone number", value: 1 },
  { label: "Username", value: 2 },
]

const ROLE_OPTIONS: RegisterRole[] = ["User", "Admin", "SupperAdmin"]

function inputClassName(hasError: boolean) {
  return [
    "h-10 w-full rounded-md border bg-zinc-950 px-3 text-sm text-zinc-100",
    "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1",
    hasError ? "border-red-500/70 focus-visible:ring-red-500/60" : "border-zinc-700 focus-visible:ring-zinc-500",
  ].join(" ")
}

export function AuthView() {
  const { signIn, register } = useAuth()
  const navigate = useNavigate()

  const [mode, setMode] = React.useState<Mode>("signin")
  const [submitting, setSubmitting] = React.useState(false)

  const [signinValue, setSigninValue] = React.useState("")
  const [signinPassword, setSigninPassword] = React.useState("")
  const [showSigninPassword, setShowSigninPassword] = React.useState(false)

  const [regType, setRegType] = React.useState<IdentifierType>(0)
  const [regValue, setRegValue] = React.useState("")
  const [regPassword, setRegPassword] = React.useState("")
  const [regConfirmPassword, setRegConfirmPassword] = React.useState("")
  const [showRegPassword, setShowRegPassword] = React.useState(false)
  const [showRegConfirmPassword, setShowRegConfirmPassword] = React.useState(false)
  const [regRole, setRegRole] = React.useState<RegisterRole>("User")
  const [regMasterKey, setRegMasterKey] = React.useState("")

  const [error, setError] = React.useState<string | null>(null)

  const [touched, setTouched] = React.useState<{ [k: string]: boolean }>({})

  const regIdentifyError = React.useMemo(() => {
    return validateIdentifierValue(regType, regValue)
  }, [regType, regValue])

  const regPasswordError = React.useMemo(() => {
    if (!regPassword.trim()) return "Password is required"
    if (regPassword.length < 8) return "Password must be at least 8 characters"
    if (!/[a-z]/.test(regPassword)) return "Password must include a lowercase letter"
    if (!/[A-Z]/.test(regPassword)) return "Password must include an uppercase letter"
    if (!/[0-9]/.test(regPassword)) return "Password must include a number"
    if (!/[\W_]/.test(regPassword)) return "Password must include a special character"
    return null
  }, [regPassword])

  const regConfirmError = React.useMemo(() => {
    if (!regConfirmPassword.trim()) return "Confirm password is required"
    if (regConfirmPassword !== regPassword) return "Confirm password does not match"
    return null
  }, [regConfirmPassword, regPassword])

  const regMasterKeyError = React.useMemo(() => {
    if (regRole !== "SupperAdmin") return null
    if (!regMasterKey.trim()) return "MasterKey is required"
    return null
  }, [regMasterKey, regRole])

  const canRegister = !regIdentifyError && !regPasswordError && !regConfirmError && !regMasterKeyError

  const signinIdentifyType = React.useMemo(() => detectIdentifierType(signinValue), [signinValue])
  const signinIdentifyError = React.useMemo(() => {
    if (!signinValue.trim()) return "Identify is required"
    return null
  }, [signinValue])
  const signinPasswordError = React.useMemo(() => {
    if (!signinPassword.trim()) return "Password is required"
    return null
  }, [signinPassword])
  const canSignIn = !signinIdentifyError && !signinPasswordError

  async function onSubmitSignIn(e: React.FormEvent) {
    e.preventDefault()
    setTouched((p) => ({ ...p, signinValue: true, signinPassword: true }))
    setError(null)

    if (!canSignIn) return

    setSubmitting(true)
    try {
      await signIn({ value: signinValue.trim(), password: signinPassword, type: signinIdentifyType })
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign in failed")
    } finally {
      setSubmitting(false)
    }
  }

  async function onSubmitRegister(e: React.FormEvent) {
    e.preventDefault()
    setTouched((p) => ({
      ...p,
      regType: true,
      regValue: true,
      regPassword: true,
      regConfirmPassword: true,
      regRole: true,
      regMasterKey: true,
    }))
    setError(null)

    if (!canRegister) return

    setSubmitting(true)
    try {
      await register(
        regRole,
        {
          type: regType,
          value: regValue.trim(),
          password: regPassword,
        },
        { masterKey: regRole === "SupperAdmin" ? regMasterKey : undefined },
      )

      setMode("signin")
      setSigninValue(regValue.trim())
      setSigninPassword("")
      setRegPassword("")
      setRegConfirmPassword("")
      setRegMasterKey("")
      setTouched({})
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Register failed")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-6">
      <div className="w-full max-w-md rounded-xl border border-zinc-800 bg-zinc-900 overflow-hidden">
        <div className="p-6 border-b border-zinc-800">
          <h1 className="text-xl font-semibold text-zinc-100">ControlHub</h1>
          <p className="text-sm text-zinc-400 mt-1">{mode === "signin" ? "Sign in to continue" : "Create a new account"}</p>
        </div>

        <div className="p-6">
          <div className="flex gap-2 mb-6">
            <Button
              type="button"
              variant={mode === "signin" ? "default" : "secondary"}
              className={mode === "signin" ? "bg-white text-black hover:bg-zinc-200" : "bg-zinc-800 text-zinc-100 hover:bg-zinc-700"}
              onClick={() => {
                setMode("signin")
                setError(null)
              }}
            >
              Login
            </Button>
            <Button
              type="button"
              variant={mode === "register" ? "default" : "secondary"}
              className={mode === "register" ? "bg-white text-black hover:bg-zinc-200" : "bg-zinc-800 text-zinc-100 hover:bg-zinc-700"}
              onClick={() => {
                setMode("register")
                setError(null)
              }}
            >
              Register
            </Button>
          </div>

          {error ? (
            <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">{error}</div>
          ) : null}

          {mode === "signin" ? (
            <form onSubmit={onSubmitSignIn} className="space-y-4">
              <div>
                <label className="block text-sm text-zinc-300 mb-1">Identify</label>
                <input
                  value={signinValue}
                  onChange={(e) => setSigninValue(e.target.value)}
                  onBlur={() => setTouched((p) => ({ ...p, signinValue: true }))}
                  placeholder="Email / Phone / Username"
                  className={inputClassName(Boolean(touched.signinValue && signinIdentifyError))}
                />
                {touched.signinValue && signinIdentifyError ? (
                  <p className="mt-1 text-xs text-red-300">{signinIdentifyError}</p>
                ) : (
                  <p className="mt-1 text-xs text-zinc-500">Detected type: {signinIdentifyType === 0 ? "Email" : signinIdentifyType === 1 ? "Phone" : "Username"}</p>
                )}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">Password</label>
                <div className="relative">
                  <input
                    value={signinPassword}
                    onChange={(e) => setSigninPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, signinPassword: true }))}
                    type={showSigninPassword ? "text" : "password"}
                    placeholder="Your password"
                    className={[inputClassName(Boolean(touched.signinPassword && signinPasswordError)), "pr-10"].join(" ")}
                  />
                  <button
                    type="button"
                    onClick={() => setShowSigninPassword((v) => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-300 hover:bg-white/10"
                    aria-label={showSigninPassword ? "Hide password" : "Show password"}
                  >
                    {showSigninPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {touched.signinPassword && signinPasswordError ? <p className="mt-1 text-xs text-red-300">{signinPasswordError}</p> : null}
              </div>

              <Button
                type="submit"
                disabled={!canSignIn || submitting}
                className="w-full bg-white text-black hover:bg-zinc-200 disabled:opacity-50"
              >
                {submitting ? "Signing in..." : "Sign in"}
              </Button>

              <div className="flex items-center justify-between">
                <Button
                  type="button"
                  variant="link"
                  className="px-0 text-zinc-200"
                  onClick={() => navigate("/forgot-password")}
                >
                  Forgot password?
                </Button>
              </div>
            </form>
          ) : (
            <form onSubmit={onSubmitRegister} className="space-y-4">
              <div>
                <label className="block text-sm text-zinc-300 mb-1">Identify type</label>
                <select
                  value={regType}
                  onChange={(e) => setRegType(Number(e.target.value) as IdentifierType)}
                  onBlur={() => setTouched((p) => ({ ...p, regType: true }))}
                  className={inputClassName(false)}
                >
                  {IDENTIFIER_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value} className="bg-zinc-950">
                      {opt.label}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">Identify</label>
                <input
                  value={regValue}
                  onChange={(e) => setRegValue(e.target.value)}
                  onBlur={() => setTouched((p) => ({ ...p, regValue: true }))}
                  placeholder={regType === 0 ? "name@example.com" : regType === 1 ? "+84123456789" : "username"}
                  className={inputClassName(Boolean(touched.regValue && regIdentifyError))}
                />
                {touched.regValue && regIdentifyError ? <p className="mt-1 text-xs text-red-300">{regIdentifyError}</p> : null}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">Role</label>
                <select
                  value={regRole}
                  onChange={(e) => setRegRole(e.target.value as RegisterRole)}
                  onBlur={() => setTouched((p) => ({ ...p, regRole: true }))}
                  className={inputClassName(false)}
                >
                  {ROLE_OPTIONS.map((r) => (
                    <option key={r} value={r} className="bg-zinc-950">
                      {r}
                    </option>
                  ))}
                </select>
                {regRole === "Admin" ? (
                  <p className="mt-1 text-xs text-zinc-500">Note: API requires permission policy for Admin registration.</p>
                ) : null}
              </div>

              {regRole === "SupperAdmin" ? (
                <div>
                  <label className="block text-sm text-zinc-300 mb-1">MasterKey</label>
                  <input
                    value={regMasterKey}
                    onChange={(e) => setRegMasterKey(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, regMasterKey: true }))}
                    placeholder="MasterKey"
                    className={inputClassName(Boolean(touched.regMasterKey && regMasterKeyError))}
                  />
                  {touched.regMasterKey && regMasterKeyError ? <p className="mt-1 text-xs text-red-300">{regMasterKeyError}</p> : null}
                </div>
              ) : null}

              <div>
                <label className="block text-sm text-zinc-300 mb-1">Password</label>
                <div className="relative">
                  <input
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, regPassword: true }))}
                    type={showRegPassword ? "text" : "password"}
                    placeholder="Password"
                    className={[inputClassName(Boolean(touched.regPassword && regPasswordError)), "pr-10"].join(" ")}
                  />
                  <button
                    type="button"
                    onClick={() => setShowRegPassword((v) => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-300 hover:bg-white/10"
                    aria-label={showRegPassword ? "Hide password" : "Show password"}
                  >
                    {showRegPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {touched.regPassword && regPasswordError ? <p className="mt-1 text-xs text-red-300">{regPasswordError}</p> : null}
                {!touched.regPassword && !regPasswordError ? (
                  <p className="mt-1 text-xs text-zinc-500">At least 8 chars, include lower/upper, number, special.</p>
                ) : null}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">Confirm password</label>
                <div className="relative">
                  <input
                    value={regConfirmPassword}
                    onChange={(e) => setRegConfirmPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, regConfirmPassword: true }))}
                    type={showRegConfirmPassword ? "text" : "password"}
                    placeholder="Confirm password"
                    className={[inputClassName(Boolean(touched.regConfirmPassword && regConfirmError)), "pr-10"].join(" ")}
                  />
                  <button
                    type="button"
                    onClick={() => setShowRegConfirmPassword((v) => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-zinc-300 hover:bg-white/10"
                    aria-label={showRegConfirmPassword ? "Hide password" : "Show password"}
                  >
                    {showRegConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {touched.regConfirmPassword && regConfirmError ? <p className="mt-1 text-xs text-red-300">{regConfirmError}</p> : null}
              </div>

              <Button
                type="submit"
                disabled={!canRegister || submitting}
                className="w-full bg-white text-black hover:bg-zinc-200 disabled:opacity-50"
              >
                {submitting ? "Creating..." : "Create account"}
              </Button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
