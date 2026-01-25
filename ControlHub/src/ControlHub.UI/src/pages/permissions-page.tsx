import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Pagination } from "@/components/ui/pagination"
import { LoadingState } from "@/components/ui/loading-state"
import { CreatePermissionsDialog } from "@/components/permissions/create-permissions-dialog"
import { PermissionsTable } from "@/components/permissions/permissions-table"
import { permissionsApi, type Permission, type PagedResult } from "@/services/api"
import { useAuth } from "@/auth/use-auth"
import { Plus, Search } from "lucide-react"

export function PermissionsPage() {
  const { auth } = useAuth()
  const [permissions, setPermissions] = useState<PagedResult<Permission>>({
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

  useEffect(() => {
    loadPermissions()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [permissions.pageIndex, searchTerm])

  const loadPermissions = async () => {
    if (!auth?.accessToken) return

    setIsLoading(true)
    try {
      const result = await permissionsApi.getPermissions(
        {
          pageIndex: permissions.pageIndex,
          pageSize: permissions.pageSize,
          searchTerm: searchTerm || undefined,
        },
        auth.accessToken
      )
      setPermissions(result)
    } catch (error) {
      console.error(error)
      // toast.error(error instanceof Error ? error.message : "Failed to load permissions")
    } finally {
      setIsLoading(false)
    }
  }

  const handleSearch = (value: string) => {
    setSearchTerm(value)
    setPermissions((prev) => ({ ...prev, pageIndex: 1 }))
  }

  const handlePageChange = (newPage: number) => {
    setPermissions((prev) => ({ ...prev, pageIndex: newPage }))
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Permissions</h1>
        <p className="text-muted-foreground">Manage system permissions</p>
      </div>

      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 flex-1 max-w-md">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search permissions..."
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-10"
            />
          </div>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Create Permission
        </Button>
      </div>

      {isLoading ? (
        <LoadingState message="Loading permissions..." />
      ) : (
        <div className="space-y-4">
          <PermissionsTable permissions={permissions.items} />
          <Pagination
            currentPage={permissions.pageIndex}
            totalPages={permissions.totalPages}
            totalCount={permissions.totalCount}
            onPageChange={handlePageChange}
            hasPreviousPage={permissions.hasPreviousPage}
            hasNextPage={permissions.hasNextPage}
          />
        </div>
      )}

      {auth && (
        <CreatePermissionsDialog
          accessToken={auth.accessToken}
          open={createDialogOpen}
          onOpenChange={setCreateDialogOpen}
          onSuccess={loadPermissions}
        />
      )}
    </div>
  )
}
