import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { useTranslation } from "react-i18next"
import type { Permission, PermissionDraft } from "./types"
import * as React from "react"
import { X, Search, Plus, Save, Loader2, Key, ChevronLeft, ChevronRight, Pencil, Trash2, Check } from "lucide-react"

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

  onDeletePermission: (id: string) => void
  onEditPermission: (id: string, data: { code: string; description: string }) => Promise<void>
}

export function PermissionsTableCard({
  permissions,
  searchTerm,
  onSearchTermChange,
  pageIndex,
  onPageIndexChange,
  pageSize,
  onPageSizeChange: _onPageSizeChange,
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
  onDeletePermission,
  onEditPermission,
}: PermissionsTableCardProps) {
  const { t } = useTranslation()

  const [editingPermissionId, setEditingPermissionId] = React.useState<string | null>(null)
  const [editCode, setEditCode] = React.useState("")
  const [editDescription, setEditDescription] = React.useState("")
  const [isSavingEdit, setIsSavingEdit] = React.useState(false)

  const startEdit = (p: Permission) => {
    setEditingPermissionId(p.id)
    setEditCode(p.code)
    setEditDescription(p.description)
  }

  const cancelEdit = () => {
    setEditingPermissionId(null)
    setEditCode("")
    setEditDescription("")
  }

  const saveEdit = async () => {
    if (!editingPermissionId) return
    setIsSavingEdit(true)
    await onEditPermission(editingPermissionId, { code: editCode, description: editDescription })
    setIsSavingEdit(false)
    setEditingPermissionId(null)
  }

  return (
    <div className="flex flex-col h-full bg-sidebar/10 backdrop-blur-md rounded-2xl border border-sidebar-border overflow-hidden shadow-2xl transition-all hover:border-sidebar-border/80">
      <div className="p-6 border-b border-sidebar-border/40 bg-sidebar-accent/5">
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between flex-wrap gap-3">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-sidebar-primary/10 text-sidebar-primary rounded-xl shadow-inner">
                <Key className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-xl font-black tracking-tight text-foreground uppercase bg-[var(--vibrant-gradient)] bg-clip-text text-transparent">{t('permissions.vault')}</h2>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest leading-none">{t('permissions.secrets')}</span>
                  <Badge variant="outline" className="h-4 px-1 text-[8px] font-black border-sidebar-border/50 bg-sidebar-accent/30">{totalCount}</Badge>
                </div>
              </div>
            </div>
            <div className="flex gap-2 flex-wrap justify-end">
              <Button
                variant="outline"
                size="sm"
                onClick={onStartAdd}
                className="h-9 px-4 rounded-xl text-xs font-black uppercase tracking-wider bg-sidebar-accent/20 border-sidebar-border hover:bg-sidebar-accent hover:scale-105 transition-all active:scale-95"
              >
                <Plus className="w-3.5 h-3.5 mr-2" /> {t('permissions.addPermission')}
              </Button>
              {permissionDrafts.length > 0 && (
                <Button
                  variant="vibrant"
                  size="sm"
                  onClick={onConfirmAdd}
                  disabled={!canConfirm || saving}
                  className="h-9 px-4 rounded-xl text-xs font-black uppercase tracking-wider shadow-lg shadow-sidebar-primary/20 hover:scale-105 transition-all active:scale-95"
                >
                  {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5 mr-2" />}
                  {t('permissions.confirmDrafts')}
                </Button>
              )}
              {canUpdate && (
                <Button
                  variant="vibrant"
                  size="sm"
                  onClick={onUpdate}
                  disabled={saving}
                  className="h-9 px-4 rounded-xl text-xs font-black uppercase tracking-wider shadow-lg shadow-emerald-500/20 bg-emerald-600 hover:bg-emerald-500 hover:scale-105 transition-all active:scale-95 border-none"
                >
                  {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5 mr-2" />}
                  {t('permissions.updateAll')}
                </Button>
              )}
            </div>
          </div>

          <div className="relative group/search">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground/40 group-focus-within/search:text-sidebar-primary transition-colors" />
            <input
              type="text"
              placeholder={t('permissions.searchPlaceholder')}
              value={searchTerm}
              onChange={(e) => onSearchTermChange(e.target.value)}
              className="w-full pl-10 pr-4 py-2.5 bg-sidebar-accent/20 border border-sidebar-border/50 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-sidebar-primary/30 transition-all font-medium placeholder:text-muted-foreground/30"
            />
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto custom-scrollbar p-1">
        <table className="w-full text-left border-separate border-spacing-0">
          <thead className="sticky top-0 z-10 bg-sidebar/95 backdrop-blur-md">
            <tr>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30 w-12">{t('table.stt')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30 w-24">{t('table.id')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30">{t('table.permission')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30">{t('table.description')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30 w-12"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-sidebar-border/10">
            {permissionDrafts.map((draft, idx) => (
              <tr key={`draft-${idx}`} className="group/row bg-sidebar-primary/5 hover:bg-sidebar-primary/10 transition-all">
                <td className="p-4">
                  <Badge variant="warning" className="h-5 px-1.5 text-[8px] font-black uppercase tracking-tighter shadow-sm animate-pulse">Draft</Badge>
                </td>
                <td className="p-4 text-muted-foreground font-mono text-[10px]">-</td>
                <td className="p-4">
                  <div className="relative">
                    <input
                      value={draft.code}
                      onChange={(e) => onDraftChange(idx, { code: e.target.value })}
                      placeholder={t('permissions.codePlaceholder')}
                      className="w-full bg-transparent border-none p-0 text-sm font-bold text-foreground placeholder:text-muted-foreground/20 focus:ring-0"
                    />
                    <button
                      onClick={() => onRemoveDraft(idx)}
                      className="absolute -right-2 top-1/2 -translate-y-1/2 p-1 rounded-full text-muted-foreground/40 hover:text-destructive hover:bg-destructive/10 opacity-0 group-hover/row:opacity-100 transition-all"
                      aria-label={t('permissions.removeDraft')}
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </td>
                <td className="p-4">
                  <input
                    value={draft.description}
                    onChange={(e) => onDraftChange(idx, { description: e.target.value })}
                    placeholder={t('permissions.descriptionPlaceholder')}
                    className="w-full bg-transparent border-none p-0 text-[11px] text-muted-foreground italic placeholder:text-muted-foreground/20 focus:ring-0"
                  />
                </td>
              </tr>
            ))}

            {loading ? (
              [...Array(pageSize)].map((_, i) => (
                <tr key={i} className="animate-pulse">
                  <td colSpan={4} className="p-4">
                    <div className="h-10 bg-sidebar-accent/5 rounded-xl border border-sidebar-border/20"></div>
                  </td>
                </tr>
              ))
            ) : (
              permissions.map((permission, idx) => (
                <tr
                  key={permission.id}
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.setData("application/rbac-permission-id", permission.id)
                    e.dataTransfer.effectAllowed = "copy"
                  }}
                  className="group/row transition-all duration-300 hover:bg-sidebar-accent/10 cursor-grab active:cursor-grabbing"
                  title={t('permissions.dragHint')}
                >
                  <td className="p-4 text-xs font-mono font-bold text-muted-foreground/40">{(pageIndex - 1) * pageSize + idx + 1}</td>
                  <td className="p-4 text-[10px] font-mono text-muted-foreground/50">{permission.id.substring(0, 8)}...</td>
                  <td className="p-4">
                    {editingPermissionId === permission.id ? (
                      <input
                        className="w-full bg-sidebar-primary/5 border border-sidebar-primary/20 rounded px-2 py-1 text-xs font-bold font-mono text-emerald-500 focus:outline-none focus:ring-1 focus:ring-sidebar-primary"
                        value={editCode}
                        onChange={(e) => setEditCode(e.target.value)}
                        autoFocus
                      />
                    ) : (
                      <span className="px-2 py-1 rounded-lg bg-emerald-500/10 text-emerald-500 border border-emerald-500/20 text-xs font-bold font-mono">
                        {permission.code}
                      </span>
                    )}
                  </td>
                  <td className="p-4 text-[11px] text-muted-foreground italic opacity-70 group-hover/row:opacity-100 transition-opacity">
                    {editingPermissionId === permission.id ? (
                      <input
                        className="w-full bg-sidebar-primary/5 border border-sidebar-primary/20 rounded px-2 py-1 text-[11px] text-muted-foreground italic placeholder:text-muted-foreground/20 focus:outline-none focus:ring-1 focus:ring-sidebar-primary"
                        value={editDescription}
                        onChange={(e) => setEditDescription(e.target.value)}
                      />
                    ) : (
                      permission.description
                    )}
                  </td>
                  <td className="p-4">
                    {editingPermissionId === permission.id ? (
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={saveEdit}
                          disabled={isSavingEdit}
                          className="h-8 w-8 text-emerald-500 hover:text-emerald-600 hover:bg-emerald-500/10"
                        >
                          {isSavingEdit ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={cancelEdit}
                          disabled={isSavingEdit}
                          className="h-8 w-8 text-muted-foreground hover:text-foreground hover:bg-sidebar-accent/50"
                        >
                          <X className="w-4 h-4" />
                        </Button>
                      </div>
                    ) : (
                      <div className="flex items-center gap-1 opacity-0 group-hover/row:opacity-100 transition-all">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => startEdit(permission)}
                          className="h-8 w-8 text-muted-foreground/40 hover:text-sidebar-primary hover:bg-sidebar-primary/10"
                        >
                          <Pencil className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => onDeletePermission(permission.id)}
                          className="h-8 w-8 text-muted-foreground/40 hover:text-destructive hover:bg-destructive/10"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))
            )}

            {!loading && permissions.length === 0 && permissionDrafts.length === 0 && (
              <tr>
                <td colSpan={4} className="p-20 text-center">
                  <div className="inline-flex p-6 bg-sidebar-accent/10 rounded-3xl border border-sidebar-border/20 mb-4">
                    <Search className="w-10 h-10 text-muted-foreground/20" />
                  </div>
                  <div className="text-lg font-black text-muted-foreground/40 uppercase tracking-widest">{t('table.noResults')}</div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="p-4 border-t border-sidebar-border/40 bg-sidebar-accent/5 flex items-center justify-between">
        <div className="text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest">
          {t('table.showingXofY', { current: permissions.length, total: totalCount })}
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageIndexChange(pageIndex - 1)}
            disabled={pageIndex <= 1 || loading}
            className="h-8 w-8 rounded-lg border-sidebar-border hover:bg-sidebar-accent"
          >
            <ChevronLeft className="w-4 h-4" />
          </Button>
          <div className="flex items-center gap-1">
            {[...Array(totalPages)].map((_, i) => (
              <Button
                key={i}
                variant={pageIndex === i + 1 ? "vibrant" : "ghost"}
                size="sm"
                onClick={() => onPageIndexChange(i + 1)}
                className={cn(
                  "h-8 w-8 rounded-lg text-xs font-black p-0",
                  pageIndex === i + 1 ? "shadow-md shadow-sidebar-primary/20" : "text-muted-foreground/60"
                )}
              >
                {i + 1}
              </Button>
            ))}
          </div>
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageIndexChange(pageIndex + 1)}
            disabled={pageIndex >= totalPages || loading}
            className="h-8 w-8 rounded-lg border-sidebar-border hover:bg-sidebar-accent"
          >
            <ChevronRight className="w-4 h-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}
