import * as React from "react"
import { Search, Plus, Trash2, X, GripVertical, ChevronLeft, ChevronRight, Loader2, Save, Pencil, Check } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { useTranslation } from "react-i18next"
import type { Permission, Role, RoleDraft } from "./types"

type RolesTableCardProps = {
  roles: Role[]
  permissions: Permission[]
  searchTerm: string
  onSearchTermChange: (v: string) => void
  pageIndex: number
  onPageIndexChange: (v: number) => void
  pageSize: number
  onPageSizeChange: (v: number) => void
  totalCount: number
  totalPages: number
  loading: boolean

  roleDrafts: RoleDraft[]
  onStartAdd: () => void
  onConfirmAdd: () => void
  onUpdate: () => void
  canConfirm: boolean
  canUpdate: boolean
  saving: boolean

  onDraftChange: (index: number, patch: Partial<RoleDraft>) => void
  onRemoveDraft: (index: number) => void
  onRemovePermission: (roleId: string, permissionId: string) => void
  onDropPermissionToRole: (roleId: string, permissionId: string) => void
  onDropPermissionToDraft: (draftIndex: number, permissionId: string) => void
  onRemovePermissionFromDraft: (draftIndex: number, permissionId: string) => void

  onDeleteRole: (id: string) => void
  onEditRole: (id: string, data: { name: string; description: string }) => Promise<void>
}

export function RolesTableCard({
  roles,
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
  roleDrafts,
  onStartAdd,
  onConfirmAdd,
  onUpdate,
  canConfirm,
  canUpdate,
  saving,
  onDraftChange,
  onRemoveDraft,
  onRemovePermission,
  onDropPermissionToRole,
  onDropPermissionToDraft,
  onRemovePermissionFromDraft,
  onDeleteRole,
  onEditRole,
}: RolesTableCardProps) {
  const { t } = useTranslation()
  const permissionMap = React.useMemo(() => new Map(permissions.map((p) => [p.id, p])), [permissions])

  const [dragOverRole, setDragOverRole] = React.useState<string | null>(null)
  const [dragOverDraft, setDragOverDraft] = React.useState<number | null>(null)

  const [editingRoleId, setEditingRoleId] = React.useState<string | null>(null)
  const [editName, setEditName] = React.useState("")
  const [editDescription, setEditDescription] = React.useState("")
  const [isSavingEdit, setIsSavingEdit] = React.useState(false)

  const startEdit = (role: Role) => {
    setEditingRoleId(role.id)
    setEditName(role.name)
    setEditDescription(role.description)
  }

  const cancelEdit = () => {
    setEditingRoleId(null)
    setEditName("")
    setEditDescription("")
  }

  const saveEdit = async () => {
    if (!editingRoleId) return
    setIsSavingEdit(true)
    await onEditRole(editingRoleId, { name: editName, description: editDescription })
    setIsSavingEdit(false)
    setEditingRoleId(null)
  }

  const handleDropToRole = (e: React.DragEvent, roleId: string) => {
    e.preventDefault()
    setDragOverRole(null)
    const permissionId = e.dataTransfer.getData("application/rbac-permission-id")
    if (permissionId) {
      onDropPermissionToRole(roleId, permissionId)
    }
  }

  const handleDropToDraft = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    setDragOverDraft(null)
    const permissionId = e.dataTransfer.getData("application/rbac-permission-id")
    if (permissionId) {
      onDropPermissionToDraft(index, permissionId)
    }
  }

  return (
    <div className="flex flex-col h-full bg-sidebar/10 backdrop-blur-md rounded-2xl border border-sidebar-border overflow-hidden shadow-2xl transition-all hover:border-sidebar-border/80">
      <div className="p-6 border-b border-sidebar-border/40 bg-sidebar-accent/5">
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between flex-wrap gap-3">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-sidebar-primary/10 text-sidebar-primary rounded-xl shadow-inner">
                <GripVertical className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-xl font-black tracking-tight text-foreground uppercase">{t('roles.title')}</h2>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest leading-none">{t('roles.matrix')}</span>
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
                <Plus className="w-3.5 h-3.5 mr-2" /> {t('roles.addRole')}
              </Button>
              {roleDrafts.length > 0 && (
                <Button
                  variant="vibrant"
                  size="sm"
                  onClick={onConfirmAdd}
                  disabled={!canConfirm || saving}
                  className="h-9 px-4 rounded-xl text-xs font-black uppercase tracking-wider shadow-lg shadow-sidebar-primary/20 hover:scale-105 transition-all active:scale-95"
                >
                  {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5 mr-2" />}
                  {t('roles.confirmDrafts')}
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
                  {t('roles.updateAll')}
                </Button>
              )}
            </div>
          </div>

          <div className="relative group/search">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground/40 group-focus-within/search:text-sidebar-primary transition-colors" />
            <input
              type="text"
              placeholder={t('roles.searchPlaceholder')}
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
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30">{t('table.role')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30">{t('table.permissions')}</th>
              <th className="p-4 text-[10px] font-black text-muted-foreground/60 uppercase tracking-widest border-b border-sidebar-border/30 w-12"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-sidebar-border/10">
            {roleDrafts.map((draft, idx) => (
              <tr
                key={`draft-${idx}`}
                onDragOver={(e) => {
                  e.preventDefault()
                  setDragOverDraft(idx)
                }}
                onDragLeave={() => setDragOverDraft(null)}
                onDrop={(e) => handleDropToDraft(e, idx)}
                className={cn(
                  "group/row transition-all duration-300",
                  dragOverDraft === idx ? "bg-sidebar-primary/10" : "bg-sidebar-primary/5 hover:bg-sidebar-primary/10"
                )}
              >
                <td className="p-4">
                  <Badge variant="warning" className="h-5 px-1.5 text-[8px] font-black uppercase tracking-tighter shadow-sm animate-pulse">Draft</Badge>
                </td>
                <td className="p-4">
                  <div className="space-y-2">
                    <input
                      className="w-full bg-transparent border-none p-0 text-sm font-black text-foreground placeholder:text-muted-foreground/20 focus:ring-0"
                      placeholder={t('roles.roleNamePlaceholder')}
                      value={draft.name}
                      onChange={(e) => onDraftChange(idx, { name: e.target.value })}
                    />
                    <input
                      className="w-full bg-transparent border-none p-0 text-[11px] text-muted-foreground italic placeholder:text-muted-foreground/20 focus:ring-0"
                      placeholder={t('roles.roleDescriptionPlaceholder')}
                      value={draft.description}
                      onChange={(e) => onDraftChange(idx, { description: e.target.value })}
                    />
                  </div>
                </td>
                <td className="p-4">
                  <div className="flex flex-wrap gap-1.5 min-h-[40px] p-2 bg-sidebar-accent/10 rounded-xl border border-dashed border-sidebar-border/40 group-hover/row:border-sidebar-primary/30 transition-all">
                    {draft.permissionIds.map((pid) => (
                      <Badge
                        key={pid}
                        variant="secondary"
                        className="h-6 gap-1 pl-2 pr-1 rounded-lg border-sidebar-border/50 bg-sidebar/50 text-[10px] font-bold group/badge border shadow-sm"
                      >
                        {formatPermissionLabel(pid, permissionMap)}
                        <button
                          onClick={() => onRemovePermissionFromDraft(idx, pid)}
                          className="p-0.5 rounded-md hover:bg-destructive/10 hover:text-destructive text-muted-foreground/50 transition-colors"
                          aria-label={t('roles.removePermissionAria', { permission: formatPermissionLabel(pid, permissionMap) })}
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </Badge>
                    ))}
                    {draft.permissionIds.length === 0 && (
                      <div className="w-full h-8 flex items-center justify-center text-[10px] font-black text-muted-foreground/30 uppercase tracking-widest italic group-hover/row:text-sidebar-primary/40 transition-colors">
                        {t('roles.dropPermissionsHere')}
                      </div>
                    )}
                  </div>
                </td>
                <td className="p-4">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onRemoveDraft(idx)}
                    className="h-8 w-8 rounded-lg text-muted-foreground/40 hover:text-destructive hover:bg-destructive/10 transition-all"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
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
              roles.map((role, idx) => (
                <tr
                  key={role.id}
                  onDragOver={(e) => {
                    e.preventDefault()
                    setDragOverRole(role.id)
                  }}
                  onDragLeave={() => setDragOverRole(null)}
                  onDrop={(e) => handleDropToRole(e, role.id)}
                  className={cn(
                    "group/row transition-all duration-300 hover:bg-sidebar-accent/10",
                    dragOverRole === role.id ? "bg-sidebar-primary/10 ring-1 ring-inset ring-sidebar-primary/30" : ""
                  )}
                >
                  <td className="p-4 text-xs font-mono font-bold text-muted-foreground/40">{(pageIndex - 1) * pageSize + idx + 1}</td>
                  <td className="p-4">
                    {editingRoleId === role.id ? (
                      <div className="flex flex-col gap-2">
                        <input
                          className="w-full bg-sidebar-primary/5 border border-sidebar-primary/20 rounded px-2 py-1 text-sm font-black text-foreground placeholder:text-muted-foreground/20 focus:outline-none focus:ring-1 focus:ring-sidebar-primary"
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          autoFocus
                        />
                        <input
                          className="w-full bg-sidebar-primary/5 border border-sidebar-primary/20 rounded px-2 py-1 text-[11px] text-muted-foreground italic placeholder:text-muted-foreground/20 focus:outline-none focus:ring-1 focus:ring-sidebar-primary"
                          value={editDescription}
                          onChange={(e) => setEditDescription(e.target.value)}
                        />
                      </div>
                    ) : (
                      <div className="flex flex-col">
                        <div className="text-sm font-black text-foreground group-hover/row:text-sidebar-primary transition-colors uppercase tracking-tight">{role.name}</div>
                        <div className="text-[11px] text-muted-foreground italic line-clamp-1 mt-0.5 opacity-60 group-hover/row:opacity-100 transition-opacity">{role.description}</div>
                      </div>
                    )}
                  </td>
                  <td className="p-4">
                    <div className="flex flex-wrap gap-1.5 min-h-[40px] p-2 bg-transparent transition-all">
                      {role.permissionIds.map((pid) => (
                        <Badge
                          key={pid}
                          variant="outline"
                          className="h-6 gap-1 pl-2 pr-1 rounded-lg border-sidebar-border/30 bg-sidebar-accent/5 transition-all hover:bg-sidebar-accent/20 text-[10px] font-bold group/badge shadow-sm"
                        >
                          {formatPermissionLabel(pid, permissionMap)}
                          <button
                            onClick={() => onRemovePermission(role.id, pid)}
                            className="p-0.5 rounded-md hover:bg-destructive/10 hover:text-destructive text-muted-foreground/20 group-hover/badge:text-muted-foreground/50 transition-colors"
                            aria-label={t('roles.removePermissionFromRoleAria', { permission: formatPermissionLabel(pid, permissionMap), role: role.name })}
                          >
                            <X className="w-3 h-3" />
                          </button>
                        </Badge>
                      ))}
                      {role.permissionIds.length === 0 && (
                        <div className="w-full text-center text-[10px] font-black text-muted-foreground/20 uppercase tracking-widest italic group-hover/row:text-sidebar-primary/40 transition-colors">
                          {t('roles.dropPermissionsHere')}
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="p-4">
                    {editingRoleId === role.id ? (
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
                          onClick={() => startEdit(role)}
                          className="h-8 w-8 text-muted-foreground/40 hover:text-sidebar-primary hover:bg-sidebar-primary/10"
                        >
                          <Pencil className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => onDeleteRole(role.id)}
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
            {!loading && roles.length === 0 && roleDrafts.length === 0 && (
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
          {t('table.showingXofY', { current: roles.length, total: totalCount })}
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

function formatPermissionLabel(id: string, map: Map<string, Permission>): string {
  const p = map.get(id)
  if (!p) return id
  return p.code
}
