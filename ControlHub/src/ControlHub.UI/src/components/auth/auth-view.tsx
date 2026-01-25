import * as React from "react"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/auth/use-auth"
import type { IdentifierType, RegisterRole } from "@/auth/types"
import { detectIdentifierType, validateIdentifierValue } from "@/auth/validators"
import { Eye, EyeOff } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { getActiveIdentifierConfigs, type IdentifierConfigDto } from "@/services/api/identifiers"
import { cn } from "@/lib/utils"
import { useTranslation } from "react-i18next"

type Mode = "signin" | "register"

// Map from API response to IdentifierType values
function mapIdentifierType(name: string): IdentifierType {
  const normalized = name.toLowerCase()
  switch (normalized) {
    case 'email': return 0
    case 'phone': return 1
    case 'username': return 2
    case 'employeeid': return 2 // Map EmployeeID to Username type for now
    case 'age': return 99 // Custom type for Age
    default: return 99 // Default to Custom for any new/unknown identifier types
  }
}


// const ROLE_OPTIONS: RegisterRole[] = ["User", "Admin", "SupperAdmin"]

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
  const { t } = useTranslation()

  const [mode, setMode] = React.useState<Mode>("signin")
  const [submitting, setSubmitting] = React.useState(false)
  const [activeIdentifiers, setActiveIdentifiers] = React.useState<IdentifierConfigDto[]>([])

  const [signinValue, setSigninValue] = React.useState("")
  const [signinPassword, setSigninPassword] = React.useState("")
  const [showSigninPassword, setShowSigninPassword] = React.useState(false)

  const [signinIdentifyType, setSigninIdentifyType] = React.useState<IdentifierType>(2) // Default to username for signin
  const [signinConfigId, setSigninConfigId] = React.useState<string | undefined>(undefined)

  const [regType, setRegType] = React.useState<IdentifierType>(0)
  const [regConfigId, setRegConfigId] = React.useState<string | undefined>(undefined)
  const [regValue, setRegValue] = React.useState("")
  const [regPassword, setRegPassword] = React.useState("")
  const [regConfirmPassword, setRegConfirmPassword] = React.useState("")
  const [showRegPassword, setShowRegPassword] = React.useState(false)
  const [showRegConfirmPassword, setShowRegConfirmPassword] = React.useState(false)
  // const [regRole, setRegRole] = React.useState<RegisterRole>("SupperAdmin") 
  const regRole: RegisterRole = "SupperAdmin"
  const [regMasterKey, setRegMasterKey] = React.useState("")

  const [error, setError] = React.useState<string | null>(null)

  const [touched, setTouched] = React.useState<{ [k: string]: boolean }>({})

  React.useEffect(() => {
    async function loadActiveIdentifiers() {
      try {
        const configs = await getActiveIdentifierConfigs()
        setActiveIdentifiers(configs)

        // Initialize default reg types
        const emailConfig = configs.find(c => c.name.toLowerCase() === 'email')
        if (emailConfig) {
          setRegType(0)
          setRegConfigId(emailConfig.id)
        } else if (configs.length > 0) {
          setRegType(mapIdentifierType(configs[0].name))
          setRegConfigId(configs[0].id)
        }
      } catch (err) {
        console.error("Failed to load active identifiers:", err)
      }
    }
    loadActiveIdentifiers()
  }, [])

  const regIdentifyError = React.useMemo(() => {
    return validateIdentifierValue(regType, regValue)
  }, [regType, regValue])

  const regPasswordError = React.useMemo(() => {
    if (!regPassword.trim()) return t('auth.validation.passwordRequired')
    if (regPassword.length < 8) return t('auth.validation.passwordMinLength')
    if (!/[a-z]/.test(regPassword)) return t('auth.validation.passwordLowercase')
    if (!/[A-Z]/.test(regPassword)) return t('auth.validation.passwordUppercase')
    if (!/[0-9]/.test(regPassword)) return t('auth.validation.passwordNumber')
    if (!/[\W_]/.test(regPassword)) return t('auth.validation.passwordSpecial')
    return null
  }, [regPassword, t])

  const regConfirmError = React.useMemo(() => {
    if (!regConfirmPassword.trim()) return t('auth.validation.confirmPasswordRequired')
    if (regConfirmPassword !== regPassword) return t('auth.validation.confirmPasswordMatch')
    return null
  }, [regConfirmPassword, regPassword, t])

  const regMasterKeyError = React.useMemo(() => {
    // Only SupperAdmin registration is allowed now
    if (!regMasterKey.trim()) return t('auth.validation.masterKeyRequired')
    return null
  }, [regMasterKey, t])

  const canRegister = !regIdentifyError && !regPasswordError && !regConfirmError && !regMasterKeyError

  const detectedSigninType = React.useMemo(() => detectIdentifierType(signinValue), [signinValue])

  // Sync detected type to state for signin if we don't have a manual config selection
  React.useEffect(() => {
    if (!signinConfigId) {
      setSigninIdentifyType(detectedSigninType)
    }
  }, [detectedSigninType, signinConfigId])
  const signinIdentifyError = React.useMemo(() => {
    if (!signinValue.trim()) return t('auth.validation.identifyRequired')
    return null
  }, [signinValue, t])
  const signinPasswordError = React.useMemo(() => {
    if (!signinPassword.trim()) return t('auth.validation.passwordRequired')
    return null
  }, [signinPassword, t])
  const canSignIn = !signinIdentifyError && !signinPasswordError

  async function onSubmitSignIn(e: React.FormEvent) {
    e.preventDefault()
    setTouched((p) => ({ ...p, signinValue: true, signinPassword: true }))
    setError(null)

    if (!canSignIn) return

    setSubmitting(true)
    try {
      await signIn({
        value: signinValue.trim(),
        password: signinPassword,
        type: signinIdentifyType,
        identifierConfigId: signinConfigId
      })
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
          identifierConfigId: regConfigId
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
    <div className="min-h-screen bg-background flex items-center justify-center p-6 relative overflow-hidden">
      {/* Decorative gradients */}
      <div className="absolute top-0 left-0 w-full h-full pointer-events-none">
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-primary/20 rounded-full blur-[120px]" />
        <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-purple-500/10 rounded-full blur-[120px]" />
      </div>

      <div className="w-full max-w-md rounded-2xl border border-sidebar-border bg-sidebar/80 backdrop-blur-xl shadow-2xl relative z-10 overflow-hidden">
        <div className="p-8 border-b border-sidebar-border text-center">
          <h1 className="text-3xl font-extrabold bg-[var(--vibrant-gradient)] bg-clip-text text-transparent">
            ControlHub
          </h1>
          <p className="text-muted-foreground mt-2 font-medium">
            {mode === "signin" ? t('auth.signInTitle') : t('auth.registerTitle')}
          </p>
        </div>

        <div className="p-8">
          <div className="grid grid-cols-2 gap-3 mb-8 p-1 bg-background rounded-xl border border-sidebar-border">
            <Button
              type="button"
              variant={mode === "signin" ? "vibrant" : "ghost"}
              className={cn(
                "rounded-lg font-semibold",
                mode !== "signin" && "text-muted-foreground hover:text-white"
              )}
              onClick={() => {
                setMode("signin")
                setError(null)
              }}
            >
              {t('auth.login')}
            </Button>
            <Button
              type="button"
              variant={mode === "register" ? "vibrant" : "ghost"}
              className={cn(
                "rounded-lg font-semibold",
                mode !== "register" && "text-muted-foreground hover:text-white"
              )}
              onClick={() => {
                setMode("register")
                setError(null)
              }}
            >
              {t('auth.register')}
            </Button>
          </div>

          {error ? (
            <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">{error}</div>
          ) : null}

          {mode === "signin" ? (
            <form onSubmit={onSubmitSignIn} className="space-y-4">
              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.identify')}</label>
                <input
                  value={signinValue}
                  onChange={(e) => setSigninValue(e.target.value)}
                  onBlur={() => setTouched((p) => ({ ...p, signinValue: true }))}
                  placeholder={t('auth.identifyPlaceholder')}
                  className={inputClassName(Boolean(touched.signinValue && signinIdentifyError))}
                />
                {touched.signinValue && signinIdentifyError ? (
                  <p className="mt-1 text-xs text-red-300">{signinIdentifyError}</p>
                ) : (
                  <p className="mt-1 text-xs text-zinc-500">
                    {t('auth.detectedType')}: {signinIdentifyType === 0 ? t('auth.email') : signinIdentifyType === 1 ? t('auth.phone') : signinIdentifyType === 2 ? t('auth.username') : `${t('auth.custom')} (${signinIdentifyType})`}
                  </p>
                )}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.password')}</label>
                <div className="relative">
                  <input
                    value={signinPassword}
                    onChange={(e) => setSigninPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, signinPassword: true }))}
                    type={showSigninPassword ? "text" : "password"}
                    placeholder={t('auth.passwordPlaceholder')}
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
                variant="vibrant"
                disabled={!canSignIn || submitting}
                className="w-full text-white font-bold py-6 rounded-xl shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all disabled:opacity-50"
              >
                {submitting ? t('auth.signingIn') : t('auth.signIn')}
              </Button>

              <div className="flex items-center justify-between">
                <Button
                  type="button"
                  variant="link"
                  className="px-0 text-zinc-200"
                  onClick={() => navigate("/forgot-password")}
                >
                  {t('auth.forgotPassword')}
                </Button>
              </div>
            </form>
          ) : (
            <form onSubmit={onSubmitRegister} className="space-y-4">
              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.identifyType')}</label>
                <select
                  value={regConfigId}
                  onChange={(e) => {
                    const configId = e.target.value
                    const config = activeIdentifiers.find(c => c.id === configId)
                    if (config) {
                      setRegConfigId(configId)
                      setRegType(mapIdentifierType(config.name))
                    }
                  }}
                  onBlur={() => setTouched((p) => ({ ...p, regType: true }))}
                  className={inputClassName(false)}
                >
                  {activeIdentifiers.map((config) => (
                    <option key={config.id} value={config.id} className="bg-zinc-950">
                      {config.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.identify')}</label>
                <input
                  value={regValue}
                  onChange={(e) => setRegValue(e.target.value)}
                  onBlur={() => setTouched((p) => ({ ...p, regValue: true }))}
                  placeholder={regType === 0 ? "name@example.com" : regType === 1 ? "+84123456789" : "username"}
                  className={inputClassName(Boolean(touched.regValue && regIdentifyError))}
                />
                {touched.regValue && regIdentifyError ? <p className="mt-1 text-xs text-red-300">{regIdentifyError}</p> : null}
              </div>

              {/* Role selection removed - Defaults to SupperAdmin */}

              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.masterKey')}</label>
                <input
                  value={regMasterKey}
                  onChange={(e) => setRegMasterKey(e.target.value)}
                  onBlur={() => setTouched((p) => ({ ...p, regMasterKey: true }))}
                  placeholder={t('auth.masterKeyPlaceholder')}
                  className={inputClassName(Boolean(touched.regMasterKey && regMasterKeyError))}
                />
                {touched.regMasterKey && regMasterKeyError ? <p className="mt-1 text-xs text-red-300">{regMasterKeyError}</p> : null}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.password')}</label>
                <div className="relative">
                  <input
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, regPassword: true }))}
                    type={showRegPassword ? "text" : "password"}
                    placeholder={t('auth.password')}
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
                  <p className="mt-1 text-xs text-zinc-500">{t('auth.hints.passwordRequirements')}</p>
                ) : null}
              </div>

              <div>
                <label className="block text-sm text-zinc-300 mb-1">{t('auth.confirmPassword')}</label>
                <div className="relative">
                  <input
                    value={regConfirmPassword}
                    onChange={(e) => setRegConfirmPassword(e.target.value)}
                    onBlur={() => setTouched((p) => ({ ...p, regConfirmPassword: true }))}
                    type={showRegConfirmPassword ? "text" : "password"}
                    placeholder={t('auth.confirmPassword')}
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
                variant="vibrant"
                disabled={!canRegister || submitting}
                className="w-full text-white font-bold py-6 rounded-xl shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all disabled:opacity-50"
              >
                {submitting ? t('auth.creating') : t('auth.createAccount')}
              </Button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
