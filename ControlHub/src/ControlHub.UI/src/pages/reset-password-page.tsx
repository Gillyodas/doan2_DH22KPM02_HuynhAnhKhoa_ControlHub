import * as React from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import * as api from "@/auth/api"
import { Eye, EyeOff } from "lucide-react"
import { toast } from "sonner"

function inputClassName(hasError: boolean) {
  return [
    "h-10 w-full rounded-md border bg-zinc-950 px-3 text-sm text-zinc-100",
    "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1",
    hasError ? "border-red-500/70 focus-visible:ring-red-500/60" : "border-zinc-700 focus-visible:ring-zinc-500",
  ].join(" ")
}

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const tokenFromUrl = searchParams.get("token") || ""

  const [token, setToken] = React.useState(tokenFromUrl)
  const [password, setPassword] = React.useState("")
  const [confirmPassword, setConfirmPassword] = React.useState("")
  const [showPassword, setShowPassword] = React.useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false)
  const [submitting, setSubmitting] = React.useState(false)
  const [touched, setTouched] = React.useState<{ [k: string]: boolean }>({})
  const [error, setError] = React.useState<string | null>(null)
  const [success, setSuccess] = React.useState(false)

  React.useEffect(() => {
    if (tokenFromUrl) {
      setToken(tokenFromUrl)
    }
  }, [tokenFromUrl])

  const tokenError = React.useMemo(() => {
    if (!token.trim()) return "Token is required"
    return null
  }, [token])

  const passwordError = React.useMemo(() => {
    if (!password.trim()) return "Password is required"
    if (password.length < 8) return "Password must be at least 8 characters"
    if (!/[a-z]/.test(password)) return "Password must include a lowercase letter"
    if (!/[A-Z]/.test(password)) return "Password must include an uppercase letter"
    if (!/[0-9]/.test(password)) return "Password must include a number"
    if (!/[\W_]/.test(password)) return "Password must include a special character"
    return null
  }, [password])

  const confirmError = React.useMemo(() => {
    if (!confirmPassword.trim()) return "Confirm password is required"
    if (confirmPassword !== password) return "Passwords do not match"
    return null
  }, [confirmPassword, password])

  const canSubmit = !tokenError && !passwordError && !confirmError

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setError(null)

    try {
      await api.resetPassword({ token, password })
      toast.success("Password has been reset successfully")
      setSuccess(true)
      setTimeout(() => {
        navigate("/login")
      }, 2000)
    } catch (err) {
      const error = err as Error
      setError(error.message || "Failed to reset password. Please try again.")
      toast.error(error.message || "Failed to reset password")
    } finally {
      setSubmitting(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center p-6">
        <div className="w-full max-w-md rounded-xl border border-zinc-800 bg-zinc-900 p-6 text-center">
          <h1 className="text-2xl font-bold text-zinc-100">Password Reset Successful</h1>
          <p className="text-zinc-400 mt-2">Your password has been reset successfully.</p>
          <Button onClick={() => navigate('/login')} className="mt-4 w-full">
            Back to Login
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-6">
      <div className="w-full max-w-md rounded-xl border border-zinc-800 bg-zinc-900 overflow-hidden">
        <div className="p-6">
          <div className="text-center mb-6">
            <h1 className="text-2xl font-bold text-zinc-100">Reset Password</h1>
            <p className="text-zinc-400">Enter your new password below</p>
          </div>

          {error && (
            <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            {!tokenFromUrl && (
              <div className="space-y-2">
                <label htmlFor="token" className="text-sm font-medium text-zinc-300">
                  Reset Token
                </label>
                <input
                  id="token"
                  type="text"
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                  onBlur={() => setTouched(t => ({ ...t, token: true }))}
                  className={inputClassName(!!tokenError && (touched.token || submitting))}
                  placeholder="Enter reset token"
                />
                {tokenError && (touched.token || submitting) && (
                  <p className="text-sm text-red-500 mt-1">{tokenError}</p>
                )}
              </div>
            )}

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium text-zinc-300">
                New Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  onBlur={() => setTouched(t => ({ ...t, password: true }))}
                  className={[
                    inputClassName(!!passwordError && (touched.password || submitting)),
                    "pr-10"
                  ].join(" ")}
                  placeholder="Enter new password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200"
                >
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
              {passwordError && (touched.password || submitting) && (
                <p className="text-sm text-red-500 mt-1">{passwordError}</p>
              )}
            </div>

            <div className="space-y-2">
              <label htmlFor="confirmPassword" className="text-sm font-medium text-zinc-300">
                Confirm Password
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? "text" : "password"}
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  onBlur={() => setTouched(t => ({ ...t, confirmPassword: true }))}
                  className={[
                    inputClassName(!!confirmError && (touched.confirmPassword || submitting)),
                    "pr-10"
                  ].join(" ")}
                  placeholder="Confirm new password"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200"
                >
                  {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
              {confirmError && (touched.confirmPassword || submitting) && (
                <p className="text-sm text-red-500 mt-1">{confirmError}</p>
              )}
            </div>

            <Button
              type="submit"
              className="w-full mt-4"
              disabled={!canSubmit || submitting}
            >
              {submitting ? "Resetting Password..." : "Reset Password"}
            </Button>

            <div className="flex justify-between pt-2">
              <Button
                type="button"
                variant="link"
                className="text-zinc-400 hover:text-zinc-200 px-0"
                onClick={() => navigate('/login')}
              >
                Back to Login
              </Button>
              <Button
                type="button"
                variant="link"
                className="text-zinc-400 hover:text-zinc-200 px-0"
                onClick={() => navigate('/forgot-password')}
              >
                Request New Token
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
