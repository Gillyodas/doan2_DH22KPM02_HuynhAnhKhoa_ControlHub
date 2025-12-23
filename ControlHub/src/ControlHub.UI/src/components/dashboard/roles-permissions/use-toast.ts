import * as React from "react"
import type { ToastState } from "./types"

type NotifyInput = Omit<ToastState, "id">

export function useToast() {
  const [toast, setToast] = React.useState<ToastState | null>(null)
  const timerRef = React.useRef<number | null>(null)

  const showToast = React.useCallback((next: NotifyInput) => {
    if (timerRef.current) {
      window.clearTimeout(timerRef.current)
      timerRef.current = null
    }

    const id = String(Date.now())
    setToast({ ...next, id })

    timerRef.current = window.setTimeout(() => {
      setToast(null)
      timerRef.current = null
    }, 2500)
  }, [])

  const closeToast = React.useCallback(() => {
    if (timerRef.current) {
      window.clearTimeout(timerRef.current)
      timerRef.current = null
    }
    setToast(null)
  }, [])

  React.useEffect(() => {
    return () => {
      if (timerRef.current) window.clearTimeout(timerRef.current)
    }
  }, [])

  return { toast, showToast, closeToast }
}
