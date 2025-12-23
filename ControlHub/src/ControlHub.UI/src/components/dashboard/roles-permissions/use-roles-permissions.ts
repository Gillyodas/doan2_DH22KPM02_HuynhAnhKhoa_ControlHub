import * as React from "react"
import { initialPermissions, initialRoles } from "./data"
import type { Permission, PermissionDraft, Role, RoleDraft } from "./types"

type NotifyFn = (input: { title: string; description?: string; variant?: "success" | "error" | "info" }) => void

type UseRolesPermissionsOptions = {
  notify: NotifyFn
  accessToken?: string
}

export function useRolesPermissions({ notify, accessToken }: UseRolesPermissionsOptions) {
  const [roles, setRoles] = React.useState<Role[]>(() => initialRoles)
  const [permissions, setPermissions] = React.useState<Permission[]>(() => initialPermissions)

  const [rolesSearchTerm, setRolesSearchTerm] = React.useState("")
  const [rolesPageIndex, setRolesPageIndex] = React.useState(1)
  const [rolesPageSize, setRolesPageSize] = React.useState(10)
  const [rolesTotalCount, setRolesTotalCount] = React.useState(0)
  const [rolesTotalPages, setRolesTotalPages] = React.useState(1)
  const [loadingRoles, setLoadingRoles] = React.useState(false)
  const [rolesReloadToken, bumpRolesReloadToken] = React.useReducer((x: number) => x + 1, 0)

  const [permissionsSearchTerm, setPermissionsSearchTerm] = React.useState("")
  const [permissionsPageIndex, setPermissionsPageIndex] = React.useState(1)
  const [permissionsPageSize, setPermissionsPageSize] = React.useState(10)
  const [permissionsTotalCount, setPermissionsTotalCount] = React.useState(0)
  const [permissionsTotalPages, setPermissionsTotalPages] = React.useState(1)
  const [loadingPermissions, setLoadingPermissions] = React.useState(false)
  const [permissionsReloadToken, bumpPermissionsReloadToken] = React.useReducer((x: number) => x + 1, 0)

  const [roleDrafts, setRoleDrafts] = React.useState<RoleDraft[]>([])
  const [permissionDrafts, setPermissionDrafts] = React.useState<PermissionDraft[]>([])

  const [rolesDirty, setRolesDirty] = React.useState(false)
  const [permissionsDirty, setPermissionsDirty] = React.useState(false)

  const [savingRoles, setSavingRoles] = React.useState(false)
  const [savingPermissions, setSavingPermissions] = React.useState(false)

  const rolePermissionsSnapshotRef = React.useRef<Map<string, string[]>>(new Map())

  const [debouncedRolesSearchTerm, setDebouncedRolesSearchTerm] = React.useState(rolesSearchTerm)
  const [debouncedPermissionsSearchTerm, setDebouncedPermissionsSearchTerm] = React.useState(permissionsSearchTerm)

  React.useEffect(() => {
    const t = window.setTimeout(() => setDebouncedRolesSearchTerm(rolesSearchTerm), 300)
    return () => window.clearTimeout(t)
  }, [rolesSearchTerm])

  React.useEffect(() => {
    const t = window.setTimeout(() => setDebouncedPermissionsSearchTerm(permissionsSearchTerm), 300)
    return () => window.clearTimeout(t)
  }, [permissionsSearchTerm])

  React.useEffect(() => {
    if (!accessToken) return

    let cancelled = false
    setLoadingRoles(true)

    ;(async () => {
      try {
        const mod = await import("@/rbac/api")
        const res = await mod.getRoles(accessToken, {
          pageIndex: rolesPageIndex,
          pageSize: rolesPageSize,
          searchTerm: debouncedRolesSearchTerm.trim() ? debouncedRolesSearchTerm.trim() : undefined,
        })

        if (cancelled) return

        const nextRoles: Role[] = (res.items ?? []).map((r) => ({
          id: r.id,
          name: r.name,
          description: r.description,
          permissionIds: (r.permissions ?? []).map((p) => p.id),
        }))

        setRoles(nextRoles)
        setRolesTotalCount(res.totalCount ?? 0)
        setRolesTotalPages(res.totalPages ?? 1)
        setLoadingRoles(false)

        const map = new Map<string, string[]>()
        for (const r of nextRoles) {
          map.set(r.id, [...r.permissionIds])
        }
        rolePermissionsSnapshotRef.current = map
      } catch (e) {
        if (cancelled) return
        setLoadingRoles(false)
        notify({
          title: "Load roles failed",
          description: e instanceof Error ? e.message : String(e),
          variant: "error",
        })
      }
    })()

    return () => {
      cancelled = true
    }
  }, [
    accessToken,
    debouncedRolesSearchTerm,
    notify,
    rolesPageIndex,
    rolesPageSize,
    rolesReloadToken,
  ])

  React.useEffect(() => {
    if (!accessToken) return

    let cancelled = false
    setLoadingPermissions(true)

    ;(async () => {
      try {
        const mod = await import("@/rbac/api")
        const res = await mod.getPermissions(accessToken, {
          pageIndex: permissionsPageIndex,
          pageSize: permissionsPageSize,
          searchTerm: debouncedPermissionsSearchTerm.trim() ? debouncedPermissionsSearchTerm.trim() : undefined,
        })

        if (cancelled) return

        const nextPermissions: Permission[] = (res.items ?? []).map((p) => ({
          id: p.id,
          code: p.code,
          description: p.description,
        }))

        setPermissions(nextPermissions)
        setPermissionsTotalCount(res.totalCount ?? 0)
        setPermissionsTotalPages(res.totalPages ?? 1)
        setLoadingPermissions(false)
      } catch (e) {
        if (cancelled) return
        setLoadingPermissions(false)
        notify({
          title: "Load permissions failed",
          description: e instanceof Error ? e.message : String(e),
          variant: "error",
        })
      }
    })()

    return () => {
      cancelled = true
    }
  }, [
    accessToken,
    debouncedPermissionsSearchTerm,
    notify,
    permissionsPageIndex,
    permissionsPageSize,
    permissionsReloadToken,
  ])

  React.useEffect(() => {
    if (rolePermissionsSnapshotRef.current.size) return

    const map = new Map<string, string[]>()
    for (const r of roles) {
      map.set(r.id, [...r.permissionIds])
    }
    rolePermissionsSnapshotRef.current = map
  }, [roles])

  React.useEffect(() => {
    setPermissionsDirty(permissionDrafts.length > 0)
  }, [permissionDrafts.length])

  const startAddRole = React.useCallback(() => {
    setRoleDrafts((prev) => [...prev, { name: "", description: "", permissionIds: [] }])
  }, [])

  const startAddPermission = React.useCallback(() => {
    setPermissionDrafts((prev) => [...prev, { code: "", description: "" }])
  }, [])

  const updateRoleDraft = React.useCallback((index: number, patch: Partial<RoleDraft>) => {
    setRoleDrafts((prev) => prev.map((d, i) => (i === index ? { ...d, ...patch } : d)))
  }, [])

  const updatePermissionDraft = React.useCallback((index: number, patch: Partial<PermissionDraft>) => {
    setPermissionDrafts((prev) => prev.map((d, i) => (i === index ? { ...d, ...patch } : d)))
  }, [])

  const removeRoleDraft = React.useCallback((index: number) => {
    setRoleDrafts((prev) => prev.filter((_, i) => i !== index))
  }, [])

  const removePermissionDraft = React.useCallback((index: number) => {
    setPermissionDrafts((prev) => prev.filter((_, i) => i !== index))
  }, [])

  const addPermissionToRole = React.useCallback((roleId: string, permissionId: string) => {
    setRoles((prev) => {
      const next = prev.map((r) =>
        r.id === roleId
          ? r.permissionIds.includes(permissionId)
            ? r
            : { ...r, permissionIds: [...r.permissionIds, permissionId] }
          : r,
      )
      return next
    })
    setRolesDirty(true)
  }, [])

  const removePermissionFromRole = React.useCallback((roleId: string, permissionId: string) => {
    setRoles((prev) => prev.map((r) => (r.id === roleId ? { ...r, permissionIds: r.permissionIds.filter((p) => p !== permissionId) } : r)))
    setRolesDirty(true)
  }, [])

  const addPermissionToRoleDraft = React.useCallback((draftIndex: number, permissionId: string) => {
    setRoleDrafts((prev) =>
      prev.map((d, i) => {
        if (i !== draftIndex) return d
        if (d.permissionIds.includes(permissionId)) return d
        return { ...d, permissionIds: [...d.permissionIds, permissionId] }
      }),
    )
  }, [])

  const removePermissionFromRoleDraft = React.useCallback((draftIndex: number, permissionId: string) => {
    setRoleDrafts((prev) =>
      prev.map((d, i) => (i === draftIndex ? { ...d, permissionIds: d.permissionIds.filter((p) => p !== permissionId) } : d)),
    )
  }, [])

  const confirmAddRoles = React.useCallback(async () => {
    if (!roleDrafts.length) return

    const normalized = roleDrafts.map((d) => ({
      name: d.name.trim(),
      description: d.description.trim(),
      permissionIds: d.permissionIds,
    }))

    const missing = normalized.find((d) => !d.name || !d.description || !d.permissionIds.length)
    if (missing) {
      notify({ title: "Invalid role", description: "Name, description and permissions are required.", variant: "error" })
      return
    }

    if (!accessToken) {
      notify({ title: "Not authenticated", description: "Please login again.", variant: "error" })
      return
    }

    setSavingRoles(true)
    try {
      const mod = await import("@/rbac/api")
      const res = await mod.createRoles(normalized, accessToken)
      notify({
        title: res.message || "Roles created",
        description: `Success: ${res.successCount}. Failed: ${res.failureCount}.`,
        variant: res.failureCount ? "info" : "success",
      })
      setRoleDrafts([])
      bumpRolesReloadToken()
    } catch (e) {
      notify({ title: "Create roles failed", description: e instanceof Error ? e.message : String(e), variant: "error" })
    } finally {
      setSavingRoles(false)
    }
  }, [accessToken, notify, roleDrafts])

  const confirmAddPermissions = React.useCallback(async () => {
    if (!permissionDrafts.length) return

    const normalized = permissionDrafts.map((d) => ({
      code: d.code.trim(),
      description: d.description.trim(),
    }))

    const missing = normalized.find((d) => !d.code)
    if (missing) {
      notify({ title: "Permission code is required", variant: "error" })
      return
    }

    if (!accessToken) {
      notify({ title: "Not authenticated", description: "Please login again.", variant: "error" })
      return
    }

    setSavingPermissions(true)
    try {
      const mod = await import("@/rbac/api")
      await mod.createPermissions(normalized, accessToken)
      notify({ title: "Permissions created", description: `Created: ${normalized.length}`, variant: "success" })
      setPermissionDrafts([])
      setPermissionsDirty(false)
      bumpPermissionsReloadToken()
    } catch (e) {
      notify({ title: "Create permissions failed", description: e instanceof Error ? e.message : String(e), variant: "error" })
    } finally {
      setSavingPermissions(false)
    }
  }, [accessToken, notify, permissionDrafts])

  const updateRoles = React.useCallback(async () => {
    setSavingRoles(true)
    try {
      if (!accessToken) {
        throw new Error("Not authenticated")
      }

      const snapshot = rolePermissionsSnapshotRef.current
      const updates = roles
        .map((r) => {
          const prev = new Set(snapshot.get(r.id) ?? [])
          const curr = new Set(r.permissionIds)
          const added = r.permissionIds.filter((p) => !prev.has(p))
          const removed = [...prev].filter((p) => !curr.has(p))
          return { roleId: r.id, added, removed }
        })
        .filter((u) => u.added.length || u.removed.length)

      if (!updates.length) {
        setRolesDirty(false)
        notify({ title: "No changes", variant: "info" })
        return
      }

      const hasRemovals = updates.some((u) => u.removed.length)
      if (hasRemovals) {
        notify({ title: "Remove is not supported", description: "Backend currently only supports adding permissions.", variant: "info" })
      }

      const mod = await import("@/rbac/api")
      const toApply = updates.filter((u) => u.added.length)

      await Promise.all(toApply.map((u) => mod.addPermissionsForRole(u.roleId, u.added, accessToken)))

      for (const u of toApply) {
        const prev = snapshot.get(u.roleId) ?? []
        snapshot.set(u.roleId, [...new Set([...prev, ...u.added])])
      }

      setRolesDirty(false)
      notify({ title: "Roles updated", description: "Permissions added successfully.", variant: "success" })
    } catch (e) {
      notify({ title: "Update failed", description: e instanceof Error ? e.message : "API call failed.", variant: "error" })
    } finally {
      setSavingRoles(false)
    }
  }, [accessToken, notify, roles])

  const updatePermissions = React.useCallback(async () => {
    await confirmAddPermissions()
  }, [confirmAddPermissions])

  const canConfirmRole = Boolean(
    roleDrafts.length && roleDrafts.every((d) => d.name.trim() && d.description.trim() && d.permissionIds.length),
  )
  const canConfirmPermission = Boolean(permissionDrafts.length && permissionDrafts.every((d) => d.code.trim()))

  return {
    roles,
    permissions,

    rolesSearchTerm,
    setRolesSearchTerm: (v: string) => {
      if (rolesDirty) {
        notify({ title: "Unsaved changes", description: "Please Update roles before searching/navigating.", variant: "info" })
        return
      }
      setRolesSearchTerm(v)
      setRolesPageIndex(1)
    },
    rolesPageIndex,
    rolesPageSize,
    rolesTotalCount,
    rolesTotalPages,
    loadingRoles,
    setRolesPageIndex: (v: number) => {
      if (rolesDirty) {
        notify({ title: "Unsaved changes", description: "Please Update roles before changing page.", variant: "info" })
        return
      }
      setRolesPageIndex(v)
    },
    setRolesPageSize: (v: number) => {
      if (rolesDirty) {
        notify({ title: "Unsaved changes", description: "Please Update roles before changing page size.", variant: "info" })
        return
      }
      setRolesPageSize(v)
      setRolesPageIndex(1)
    },

    permissionsSearchTerm,
    setPermissionsSearchTerm: (v: string) => {
      if (permissionsDirty) {
        notify({ title: "Unsaved changes", description: "Please Update permissions before searching/navigating.", variant: "info" })
        return
      }
      setPermissionsSearchTerm(v)
      setPermissionsPageIndex(1)
    },
    permissionsPageIndex,
    permissionsPageSize,
    permissionsTotalCount,
    permissionsTotalPages,
    loadingPermissions,
    setPermissionsPageIndex: (v: number) => {
      if (permissionsDirty) {
        notify({ title: "Unsaved changes", description: "Please Update permissions before changing page.", variant: "info" })
        return
      }
      setPermissionsPageIndex(v)
    },
    setPermissionsPageSize: (v: number) => {
      if (permissionsDirty) {
        notify({ title: "Unsaved changes", description: "Please Update permissions before changing page size.", variant: "info" })
        return
      }
      setPermissionsPageSize(v)
      setPermissionsPageIndex(1)
    },

    roleDrafts,
    permissionDrafts,

    rolesDirty,
    permissionsDirty,

    savingRoles,
    savingPermissions,

    startAddRole,
    startAddPermission,

    updateRoleDraft,
    updatePermissionDraft,

    removeRoleDraft,
    removePermissionDraft,

    addPermissionToRole,
    removePermissionFromRole,

    addPermissionToRoleDraft,
    removePermissionFromRoleDraft,

    confirmAddRoles,
    confirmAddPermissions,

    updateRoles,
    updatePermissions,

    canConfirmRole,
    canConfirmPermission,
  }
}
