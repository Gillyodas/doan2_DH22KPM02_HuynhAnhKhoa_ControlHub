import { cn } from "@/lib/utils"
import type { ToastState } from "./types"

type ToastViewProps = {
  toast: ToastState | null
  onClose: () => void
}

export function ToastView({ toast, onClose }: ToastViewProps) {
  if (!toast) return null

  const variantClass =
    toast.variant === "success"
      ? "border-emerald-700/60 bg-emerald-950/60"
      : toast.variant === "error"
        ? "border-red-700/60 bg-red-950/60"
        : "border-zinc-700/60 bg-zinc-950/60"

  return (
    <div className="fixed bottom-4 right-4 z-50 w-[360px] max-w-[calc(100vw-2rem)]">
      <div
        className={cn(
          "rounded-lg border p-4 shadow-lg backdrop-blur",
          "text-zinc-100",
          variantClass,
        )}
        role="status"
        aria-live="polite"
      >
        <div className="flex items-start gap-3">
          <div className="min-w-0 flex-1">
            <div className="text-sm font-semibold">{toast.title}</div>
            {toast.description ? <div className="mt-1 text-xs text-zinc-300">{toast.description}</div> : null}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="shrink-0 rounded-md px-2 py-1 text-xs text-zinc-300 hover:bg-white/5 hover:text-zinc-100"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  )
}
