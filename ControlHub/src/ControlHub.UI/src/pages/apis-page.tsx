export function ApisPage() {
  return (
    <div className="text-zinc-100">
      <h1 className="text-xl font-semibold">APIs</h1>
      <div className="mt-4 grid gap-3">
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-4">
          <div className="text-sm font-medium">Role</div>
          <div className="mt-1 text-xs text-zinc-400">POST /api/Role/roles</div>
          <div className="mt-1 text-xs text-zinc-400">POST /api/Role/update</div>
        </div>
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-4">
          <div className="text-sm font-medium">Permission</div>
          <div className="mt-1 text-xs text-zinc-400">POST /api/Permission/permissions</div>
        </div>
      </div>
    </div>
  )
}
