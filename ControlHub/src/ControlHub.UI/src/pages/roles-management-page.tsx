import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Pagination } from "@/components/ui/pagination"
import { LoadingState } from "@/components/ui/loading-state"
import { CreateRolesDialog } from "@/components/roles/create-roles-dialog"
import { RolesTable } from "@/components/roles/roles-table"
import { AssignPermissionsDialog } from "@/components/roles/assign-permissions-dialog"
import { rolesApi, type Role, type PagedResult } from "@/services/api"
import { useAuth } from "@/auth/use-auth"
import { Plus, Search } from "lucide-react"

export function RolesManagementPage() {
  const { auth } = useAuth()
  const [roles, setRoles] = useState<PagedResult<Role>>({
    items: [],
    pageIndex: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const [searchTerm, setSearchTerm] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [selectedRole, setSelectedRole] = useState<Role | null>(null)
  const [permissionsDialogOpen, setPermissionsDialogOpen] = useState(false)

  useEffect(() => {
    loadRoles()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roles.pageIndex, searchTerm])

  const loadRoles = async () => {
    if (!auth?.accessToken) return

    setIsLoading(true)
    try {
      const result = await rolesApi.getRoles(
        {
          pageIndex: roles.pageIndex,
          pageSize: roles.pageSize,
          searchTerm: searchTerm || undefined,
        },
        auth.accessToken
      )
      setRoles(result)
    } catch (error) {
      console.error(error)
      // toast.error(error instanceof Error ? error.message : "Failed to load roles")
    } finally {
      setIsLoading(false)
    }
  }

  const handleSearch = (value: string) => {
    setSearchTerm(value)
    setRoles((prev) => ({ ...prev, pageIndex: 1 }))
  }

  const handlePageChange = (newPage: number) => {
    setRoles((prev) => ({ ...prev, pageIndex: newPage }))
  }

  const handleManagePermissions = (role: Role) => {
    setSelectedRole(role)
    setPermissionsDialogOpen(true)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Roles</h1>
        <p className="text-muted-foreground">Manage system roles and their permissions</p>
      </div>

      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 flex-1 max-w-md">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search roles..."
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-10"
            />
          </div>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Create Role
        </Button>
      </div>

      {isLoading ? (
        <LoadingState message="Loading roles..." />
      ) : (
        <div className="space-y-4">
          <RolesTable roles={roles.items} onManagePermissions={handleManagePermissions} />
          <Pagination
            currentPage={roles.pageIndex}
            totalPages={roles.totalPages}
            totalCount={roles.totalCount}
            onPageChange={handlePageChange}
            hasPreviousPage={roles.hasPreviousPage}
            hasNextPage={roles.hasNextPage}
          />
        </div>
      )}

      {auth && (
        <>
          <CreateRolesDialog
            accessToken={auth.accessToken}
            open={createDialogOpen}
            onOpenChange={setCreateDialogOpen}
            onSuccess={loadRoles}
          />
          <AssignPermissionsDialog
            role={selectedRole}
            accessToken={auth.accessToken}
            open={permissionsDialogOpen}
            onOpenChange={setPermissionsDialogOpen}
            onSuccess={loadRoles}
          />
        </>
      )}
    </div>
  )
}
