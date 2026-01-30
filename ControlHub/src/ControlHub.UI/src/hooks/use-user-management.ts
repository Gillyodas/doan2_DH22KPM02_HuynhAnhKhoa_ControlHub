import { useState, useEffect, useCallback } from "react"
import { useAuth } from "@/auth/use-auth"
import {
    getUsers,
    updateUser as updateUserApi,
    deleteUser as deleteUserApi,
    assignRoleToUser as assignRoleApi,
    removeRoleFromUser as removeRoleApi,
} from "@/services/api/users"
import { useDebounce } from "./use-debounce"
import { User, PagedResult, UpdateUserRequest, GetUsersParams } from "@/services/api/types"
import { toast } from "sonner"

interface UseUserManagementReturn {
    users: User[]
    pagination: Omit<PagedResult<User>, "items"> | null
    isLoading: boolean
    error: string | null
    searchTerm: string
    setSearchTerm: (term: string) => void
    pageIndex: number
    setPageIndex: (index: number) => void
    pageSize: number
    setPageSize: (size: number) => void
    refetch: () => Promise<void>
    updateUser: (id: string, data: UpdateUserRequest) => Promise<boolean>
    deleteUser: (id: string) => Promise<boolean>
    assignRole: (userId: string, roleId: string) => Promise<boolean>
    removeRole: (userId: string, roleId: string) => Promise<boolean>
}

export function useUserManagement(initialPageSize = 10): UseUserManagementReturn {
    const { accessToken } = useAuth()
    const [users, setUsers] = useState<User[]>([])
    const [pagination, setPagination] = useState<Omit<PagedResult<User>, "items"> | null>(null)
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const [searchTerm, setSearchTerm] = useState("")
    const [pageIndex, setPageIndex] = useState(1)
    const [pageSize, setPageSize] = useState(initialPageSize)

    const debouncedSearchTerm = useDebounce(searchTerm, 500)

    const fetchUsers = useCallback(async () => {
        if (!accessToken) return

        setIsLoading(true)
        setError(null)
        try {
            const params: GetUsersParams = {
                pageIndex,
                pageSize,
                searchTerm: debouncedSearchTerm,
            }
            const result = await getUsers(params, accessToken)
            setUsers(result.items)
            const { items, ...paginationData } = result
            setPagination(paginationData)
        } catch (err: any) {
            console.error("Failed to fetch users:", err)
            setError(err.message || "Failed to load users")
            toast.error("Failed to load users")
        } finally {
            setIsLoading(false)
        }
    }, [accessToken, pageIndex, pageSize, debouncedSearchTerm])

    // Reset page index when search term changes
    useEffect(() => {
        setPageIndex(1)
    }, [debouncedSearchTerm])

    useEffect(() => {
        fetchUsers()
    }, [fetchUsers])

    const updateUser = async (id: string, data: UpdateUserRequest): Promise<boolean> => {
        if (!accessToken) return false
        try {
            await updateUserApi(id, data, accessToken)
            toast.success("User updated successfully")
            await fetchUsers()
            return true
        } catch (err: any) {
            console.error("Failed to update user:", err)
            toast.error(err.message || "Failed to update user")
            return false
        }
    }

    const deleteUser = async (id: string): Promise<boolean> => {
        if (!accessToken) return false
        try {
            await deleteUserApi(id, accessToken)
            toast.success("User deleted successfully")
            // If deleting the last item on a page, go back one page (if possible)
            if (users.length === 1 && pageIndex > 1) {
                setPageIndex(prev => prev - 1)
            } else {
                await fetchUsers()
            }
            return true
        } catch (err: any) {
            console.error("Failed to delete user:", err)
            toast.error(err.message || "Failed to delete user")
            return false
        }
    }

    const assignRole = async (userId: string, roleId: string): Promise<boolean> => {
        if (!accessToken) return false
        try {
            await assignRoleApi(userId, roleId, accessToken)
            toast.success("Role assigned successfully")
            await fetchUsers()
            return true
        } catch (err: any) {
            console.error("Failed to assign role:", err)
            toast.error(err.message || "Failed to assign role")
            return false
        }
    }

    const removeRole = async (userId: string, roleId: string): Promise<boolean> => {
        if (!accessToken) return false
        try {
            await removeRoleApi(userId, roleId, accessToken)
            toast.success("Role removed successfully")
            await fetchUsers()
            return true
        } catch (err: any) {
            console.error("Failed to remove role:", err)
            toast.error(err.message || "Failed to remove role")
            return false
        }
    }

    return {
        users,
        pagination,
        isLoading,
        error,
        searchTerm,
        setSearchTerm,
        pageIndex,
        setPageIndex,
        pageSize,
        setPageSize,
        refetch: fetchUsers,
        updateUser,
        deleteUser,
        assignRole,
        removeRole,
    }
}
