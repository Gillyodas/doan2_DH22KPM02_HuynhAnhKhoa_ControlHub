"use client"

import { PermissionsTableCard } from "@/components/dashboard/roles-permissions/permissions-table-card"
import { RolesTableCard } from "@/components/dashboard/roles-permissions/roles-table-card"
import { ToastView } from "@/components/dashboard/roles-permissions/toast"
import { useRolesPermissions } from "@/components/dashboard/roles-permissions/use-roles-permissions"
import { useToast } from "@/components/dashboard/roles-permissions/use-toast"
import { useAuth } from "@/auth/use-auth"

export function RolesPermissionsContent() {
  const { toast, showToast, closeToast } = useToast()
  const { auth } = useAuth()

  const {
    roles,
    permissions,

    rolesSearchTerm,
    setRolesSearchTerm,
    rolesPageIndex,
    setRolesPageIndex,
    rolesPageSize,
    setRolesPageSize,
    rolesTotalCount,
    rolesTotalPages,
    loadingRoles,

    permissionsSearchTerm,
    setPermissionsSearchTerm,
    permissionsPageIndex,
    setPermissionsPageIndex,
    permissionsPageSize,
    setPermissionsPageSize,
    permissionsTotalCount,
    permissionsTotalPages,
    loadingPermissions,

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
  } = useRolesPermissions({
    notify: showToast,
    accessToken: auth?.accessToken,
  })

  return (
    <>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RolesTableCard
          roles={roles}
          permissions={permissions}
          searchTerm={rolesSearchTerm}
          onSearchTermChange={setRolesSearchTerm}
          pageIndex={rolesPageIndex}
          onPageIndexChange={setRolesPageIndex}
          pageSize={rolesPageSize}
          onPageSizeChange={setRolesPageSize}
          totalCount={rolesTotalCount}
          totalPages={rolesTotalPages}
          loading={loadingRoles}
          roleDrafts={roleDrafts}
          onStartAdd={startAddRole}
          onConfirmAdd={confirmAddRoles}
          onUpdate={updateRoles}
          canConfirm={canConfirmRole}
          canUpdate={rolesDirty}
          saving={savingRoles}
          onDraftChange={updateRoleDraft}
          onRemoveDraft={removeRoleDraft}
          onRemovePermission={removePermissionFromRole}
          onDropPermissionToRole={addPermissionToRole}
          onDropPermissionToDraft={addPermissionToRoleDraft}
          onRemovePermissionFromDraft={removePermissionFromRoleDraft}
        />

        <PermissionsTableCard
          permissions={permissions}
          searchTerm={permissionsSearchTerm}
          onSearchTermChange={setPermissionsSearchTerm}
          pageIndex={permissionsPageIndex}
          onPageIndexChange={setPermissionsPageIndex}
          pageSize={permissionsPageSize}
          onPageSizeChange={setPermissionsPageSize}
          totalCount={permissionsTotalCount}
          totalPages={permissionsTotalPages}
          loading={loadingPermissions}
          permissionDrafts={permissionDrafts}
          onStartAdd={startAddPermission}
          onConfirmAdd={confirmAddPermissions}
          onUpdate={updatePermissions}
          canConfirm={canConfirmPermission}
          canUpdate={permissionsDirty}
          saving={savingPermissions}
          onDraftChange={updatePermissionDraft}
          onRemoveDraft={removePermissionDraft}
        />
      </div>

      <ToastView toast={toast} onClose={closeToast} />
    </>
  )
}
