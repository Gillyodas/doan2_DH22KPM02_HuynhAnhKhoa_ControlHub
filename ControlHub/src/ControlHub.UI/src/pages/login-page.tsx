import * as React from "react"
import { useLocation, useNavigate } from "react-router-dom"

import { useAuth } from "@/auth/use-auth"
import { AuthView } from "@/components/auth/auth-view"

type LocationState = {
  from?: {
    pathname?: string
  }
}

export function LoginPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const from = ((location.state as LocationState | null)?.from?.pathname ?? "/")

  React.useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true })
    }
  }, [from, isAuthenticated, navigate])

  if (isAuthenticated) return null

  return <AuthView />
}
