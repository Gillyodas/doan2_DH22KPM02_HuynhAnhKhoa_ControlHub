import React from "react"
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { MoreHorizontal, Pencil, Trash2, Shield } from "lucide-react"
import type { User } from "@/services/api/types"

interface UserTableProps {
    users: User[]
    onEdit: (user: User) => void
    onDelete: (user: User) => void
    onAssignRole: (user: User) => void
    /** Current user ID to prevent self-deletion */
    currentUserId?: string
}

/**
 * UserTable - Displays a paginated list of users with actions
 * 
 * Accessibility:
 * - Uses semantic table structure
 * - Action buttons have aria-labels
 * - Keyboard navigable dropdown menu
 */
export const UserTable = React.memo(function UserTable({
    users,
    onEdit,
    onDelete,
    onAssignRole,
    currentUserId,
}: UserTableProps) {
    if (users.length === 0) {
        return (
            <div className="flex flex-col items-center justify-center py-12 text-center">
                <p className="text-muted-foreground">No users found</p>
            </div>
        )
    }

    return (
        <div className="rounded-lg border overflow-hidden">
            <Table>
                <TableHeader>
                    <TableRow className="bg-muted/50">
                        <TableHead className="w-[200px]">Username</TableHead>
                        <TableHead className="hidden md:table-cell">Email</TableHead>
                        <TableHead className="hidden lg:table-cell">Roles</TableHead>
                        <TableHead className="hidden sm:table-cell">Status</TableHead>
                        <TableHead className="w-[80px] text-right">Actions</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {users.map((user) => (
                        <TableRow key={user.id} className="group hover:bg-muted/30 transition-colors">
                            <TableCell className="font-medium">
                                <div className="flex flex-col">
                                    <span>{user.username}</span>
                                    {/* Show email on mobile below username */}
                                    <span className="text-xs text-muted-foreground md:hidden">
                                        {user.email || "No email"}
                                    </span>
                                </div>
                            </TableCell>
                            <TableCell className="hidden md:table-cell text-muted-foreground">
                                {user.email || "-"}
                            </TableCell>
                            <TableCell className="hidden lg:table-cell">
                                <div className="flex flex-wrap gap-1">
                                    {user.roles && user.roles.length > 0 ? (
                                        user.roles.slice(0, 3).map((role) => (
                                            <Badge
                                                key={role.id}
                                                variant="secondary"
                                                className="text-xs"
                                            >
                                                {role.name}
                                            </Badge>
                                        ))
                                    ) : (
                                        <span className="text-muted-foreground text-sm">No roles</span>
                                    )}
                                    {user.roles && user.roles.length > 3 && (
                                        <Badge variant="outline" className="text-xs">
                                            +{user.roles.length - 3}
                                        </Badge>
                                    )}
                                </div>
                            </TableCell>
                            <TableCell className="hidden sm:table-cell">
                                <Badge
                                    variant={user.isActive ? "default" : "destructive"}
                                    className={user.isActive ? "bg-green-600" : ""}
                                >
                                    {user.isActive ? "Active" : "Inactive"}
                                </Badge>
                            </TableCell>
                            <TableCell className="text-right">
                                <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                        <Button
                                            variant="ghost"
                                            className="h-8 w-8 p-0 opacity-0 group-hover:opacity-100 focus:opacity-100 transition-opacity"
                                            aria-label={`Actions for user ${user.username}`}
                                        >
                                            <MoreHorizontal className="h-4 w-4" />
                                        </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end">
                                        <DropdownMenuItem onClick={() => onEdit(user)}>
                                            <Pencil className="mr-2 h-4 w-4" />
                                            Edit
                                        </DropdownMenuItem>
                                        <DropdownMenuItem onClick={() => onAssignRole(user)}>
                                            <Shield className="mr-2 h-4 w-4" />
                                            Assign Role
                                        </DropdownMenuItem>
                                        <DropdownMenuItem
                                            onClick={() => onDelete(user)}
                                            disabled={user.id === currentUserId}
                                            className="text-destructive focus:text-destructive"
                                        >
                                            <Trash2 className="mr-2 h-4 w-4" />
                                            Delete
                                        </DropdownMenuItem>
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    )
})
