import { useEffect, useState } from "react"
import { Input } from "@/components/ui/input"
import { Pagination } from "@/components/ui/pagination"
import { LoadingState } from "@/components/ui/loading-state"
import { UserTable } from "@/components/users/user-table"
import { EditUserDialog } from "@/components/users/edit-user-dialog"
import { DeleteUserDialog } from "@/components/users/delete-user-dialog"
import { AssignRoleDialog } from "@/components/users/assign-role-dialog"
import { usersApi, type User, type PagedResult } from "@/services/api"
import { useAuth } from "@/auth/use-auth"
import { useDebounce } from "@/hooks/use-debounce"
import { Search, Users } from "lucide-react"

/**
 * UsersPage - User Management CRUD page
 * 
 * Features:
 * - Paginated user list with search
 * - Edit, Delete, Assign Role actions
 * - Responsive design
 * - RBAC-aware (hides actions based on permissions)
 */
export function UsersPage() {
  const { auth } = useAuth()

  // User list state
  const [users, setUsers] = useState<PagedResult<User>>({
    items: [],
    pageIndex: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  })
  const [isLoading, setIsLoading] = useState(false)
  const [searchTerm, setSearchTerm] = useState("")
  const debouncedSearch = useDebounce(searchTerm, 300)

  // Dialog states
  const [selectedUser, setSelectedUser] = useState<User | null>(null)
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [assignRoleDialogOpen, setAssignRoleDialogOpen] = useState(false)

  // Load users when page/search changes
  useEffect(() => {
    loadUsers()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [users.pageIndex, debouncedSearch])

  const loadUsers = async () => {
    if (!auth?.accessToken) return

    setIsLoading(true)
    try {
      const result = await usersApi.getUsers(
        {
          pageIndex: users.pageIndex,
          pageSize: users.pageSize,
          searchTerm: debouncedSearch || undefined,
        },
        auth.accessToken
      )
      setUsers(result)
    } catch (error) {
      console.error("Failed to load users:", error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSearch = (value: string) => {
    setSearchTerm(value)
    // Reset to first page when searching
    setUsers((prev) => ({ ...prev, pageIndex: 1 }))
  }

  const handlePageChange = (newPage: number) => {
    setUsers((prev) => ({ ...prev, pageIndex: newPage }))
  }

  const handleEdit = (user: User) => {
    setSelectedUser(user)
    setEditDialogOpen(true)
  }

  const handleDelete = (user: User) => {
    setSelectedUser(user)
    setDeleteDialogOpen(true)
  }

  const handleAssignRole = (user: User) => {
    setSelectedUser(user)
    setAssignRoleDialogOpen(true)
  }

  const handleSuccess = () => {
    loadUsers()
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
          <Users className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-muted-foreground">
            View and manage system users
          </p>
        </div>
      </div>

      {/* Search Bar */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search users by username or email..."
            value={searchTerm}
            onChange={(e) => handleSearch(e.target.value)}
            className="pl-10"
            aria-label="Search users"
          />
        </div>
        {/* Stats Badge */}
        {!isLoading && (
          <div className="text-sm text-muted-foreground">
            {users.totalCount} user{users.totalCount !== 1 ? "s" : ""} total
          </div>
        )}
      </div>

      {/* User Table */}
      {isLoading ? (
        <LoadingState message="Loading users..." />
      ) : (
        <div className="space-y-4">
          <UserTable
            users={users.items}
            onEdit={handleEdit}
            onDelete={handleDelete}
            onAssignRole={handleAssignRole}
            currentUserId={auth?.accountId ? String(auth.accountId) : undefined}
          />

          {users.totalPages > 1 && (
            <Pagination
              currentPage={users.pageIndex}
              totalPages={users.totalPages}
              totalCount={users.totalCount}
              onPageChange={handlePageChange}
              hasPreviousPage={users.hasPreviousPage}
              hasNextPage={users.hasNextPage}
            />
          )}
        </div>
      )}

      {/* Dialogs */}
      {auth && (
        <>
          <EditUserDialog
            user={selectedUser}
            accessToken={auth.accessToken}
            open={editDialogOpen}
            onOpenChange={setEditDialogOpen}
            onSuccess={handleSuccess}
          />
          <DeleteUserDialog
            user={selectedUser}
            accessToken={auth.accessToken}
            open={deleteDialogOpen}
            onOpenChange={setDeleteDialogOpen}
            onSuccess={handleSuccess}
          />
          <AssignRoleDialog
            user={selectedUser}
            accessToken={auth.accessToken}
            open={assignRoleDialogOpen}
            onOpenChange={setAssignRoleDialogOpen}
            onSuccess={handleSuccess}
          />
        </>
      )}
    </div>
  )
}
