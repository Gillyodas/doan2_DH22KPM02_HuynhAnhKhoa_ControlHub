import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { cn } from "@/lib/utils"
import type { Permission, PermissionDraft } from "./types"
import { X } from "lucide-react"

type PermissionsTableCardProps = {
  permissions: Permission[]

  searchTerm: string
  onSearchTermChange: (value: string) => void
  pageIndex: number
  onPageIndexChange: (value: number) => void
  pageSize: number
  onPageSizeChange: (value: number) => void
  totalCount: number
  totalPages: number
  loading: boolean

  permissionDrafts: PermissionDraft[]

  onStartAdd: () => void
  onConfirmAdd: () => void
  onUpdate: () => void
  canConfirm: boolean
  canUpdate: boolean
  saving: boolean

  onDraftChange: (index: number, patch: Partial<PermissionDraft>) => void
  onRemoveDraft: (index: number) => void
}

export function PermissionsTableCard({
  permissions,
  searchTerm,
  onSearchTermChange,
  pageIndex,
  onPageIndexChange,
  pageSize,
  onPageSizeChange,
  totalCount,
  totalPages,
  loading,
  permissionDrafts,
  onStartAdd,
  onConfirmAdd,
  onUpdate,
  canConfirm,
  canUpdate,
  saving,
  onDraftChange,
  onRemoveDraft,
}: PermissionsTableCardProps) {
  return (
    <div className="bg-zinc-900 rounded-lg border border-zinc-800 overflow-hidden">
      <div className="p-4 border-b border-zinc-800 flex flex-col gap-3">
        <div className="flex items-center gap-3">
          <h2 className="text-lg font-semibold text-zinc-100">Permissions List</h2>
          <div className="ml-auto text-xs text-zinc-500">
            {loading ? "Loading..." : `${totalCount} items`}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <input
            value={searchTerm}
            onChange={(e) => onSearchTermChange(e.target.value)}
            placeholder="Search permissions..."
            className={cn(
              "h-8 w-56 rounded-md border border-zinc-700 bg-zinc-950 px-2 text-sm text-zinc-100",
              "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-zinc-500",
            )}
          />

          <select
            value={String(pageSize)}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className={cn(
              "h-8 rounded-md border border-zinc-700 bg-zinc-950 px-2 text-sm text-zinc-100",
              "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-zinc-500",
            )}
          >
            {[10, 20, 50, 100].map((n) => (
              <option key={n} value={String(n)}>
                {n} / page
              </option>
            ))}
          </select>

          <Button
            type="button"
            variant="secondary"
            className="bg-zinc-800 text-zinc-100 hover:bg-zinc-700"
            onClick={() => onPageIndexChange(Math.max(1, pageIndex - 1))}
            disabled={loading || pageIndex <= 1}
          >
            Prev
          </Button>
          <div className="text-sm text-zinc-300">
            Page {pageIndex} / {Math.max(1, totalPages)}
          </div>
          <Button
            type="button"
            variant="secondary"
            className="bg-zinc-800 text-zinc-100 hover:bg-zinc-700"
            onClick={() => onPageIndexChange(Math.min(Math.max(1, totalPages), pageIndex + 1))}
            disabled={loading || pageIndex >= Math.max(1, totalPages)}
          >
            Next
          </Button>
        </div>
      </div>

      <div className="overflow-auto scrollbar-none max-h-[calc(100vh-260px)]">
        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-zinc-900">
              <TableHead className="text-zinc-300 w-12">STT</TableHead>
              <TableHead className="text-zinc-300 w-20">ID</TableHead>
              <TableHead className="text-zinc-300 w-32">Permission</TableHead>
              <TableHead className="text-zinc-300 w-40">Description</TableHead>
            </TableRow>
          </TableHeader>

          <TableBody>
            {permissions.map((permission, index) => (
              <TableRow
                key={permission.id}
                className="border-zinc-800 hover:bg-zinc-800/50"
                draggable
                onDragStart={(e) => {
                  e.dataTransfer.setData("text/plain", permission.id)
                  e.dataTransfer.effectAllowed = "copy"
                }}
                title="Drag to a role to add"
              >
                <TableCell className="text-zinc-400">{(pageIndex - 1) * pageSize + index + 1}</TableCell>
                <TableCell className="text-zinc-300 font-mono text-xs">{permission.id}</TableCell>
                <TableCell className="text-zinc-100 font-medium text-sm">{permission.code}</TableCell>
                <TableCell className="text-zinc-400 text-xs max-w-[160px] truncate" title={permission.description}>
                  {permission.description}
                </TableCell>
              </TableRow>
            ))}

            {permissionDrafts.map((draft, draftIndex) => (
              <TableRow key={`draft-${draftIndex}`} className="border-zinc-800 bg-zinc-950/30">
                <TableCell className="text-zinc-400">-</TableCell>
                <TableCell className="text-zinc-500 font-mono text-xs">
                  (new)
                  <button
                    type="button"
                    onClick={() => onRemoveDraft(draftIndex)}
                    className="ml-2 inline-flex items-center justify-center rounded p-0.5 hover:bg-white/10"
                    aria-label="Remove draft"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </TableCell>
                <TableCell>
                  <input
                    value={draft.code}
                    onChange={(e) => onDraftChange(draftIndex, { code: e.target.value })}
                    placeholder="permission.code"
                    className={cn(
                      "h-8 w-full rounded-md border border-zinc-700 bg-zinc-950 px-2 text-sm text-zinc-100",
                      "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-zinc-500",
                    )}
                  />
                </TableCell>
                <TableCell>
                  <input
                    value={draft.description}
                    onChange={(e) => onDraftChange(draftIndex, { description: e.target.value })}
                    placeholder="Description"
                    className={cn(
                      "h-8 w-full rounded-md border border-zinc-700 bg-zinc-950 px-2 text-sm text-zinc-100",
                      "placeholder:text-zinc-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-zinc-500",
                    )}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <div className="p-4 border-t border-zinc-800 flex items-center gap-2">
        <Button onClick={onStartAdd} variant="secondary" className="bg-zinc-800 text-zinc-100 hover:bg-zinc-700">
          Add
        </Button>
        <Button
          onClick={onConfirmAdd}
          disabled={!canConfirm}
          variant="secondary"
          className="bg-zinc-800 text-zinc-100 hover:bg-zinc-700 disabled:opacity-50"
        >
          Confirm
        </Button>
        <Button
          onClick={onUpdate}
          disabled={!canUpdate || saving}
          className="bg-white text-black hover:bg-zinc-200 disabled:opacity-50"
        >
          {saving ? "Updating..." : "Update"}
        </Button>
      </div>
    </div>
  )
}
