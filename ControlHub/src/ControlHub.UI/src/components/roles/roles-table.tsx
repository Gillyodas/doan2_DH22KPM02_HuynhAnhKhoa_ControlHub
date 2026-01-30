import React from "react"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { Role } from "@/services/api"
import { MoreHorizontal, Shield, Pencil, Trash2 } from "lucide-react"

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  })
}

interface RolesTableProps {
  roles: Role[]
  onManagePermissions: (role: Role) => void
  onEdit?: (role: Role) => void
  onDelete?: (role: Role) => void
}

/**
 * RolesTable - Displays a list of roles with actions
 * 
 * Features:
 * - Role name, description, permissions preview
 * - Actions: Manage Permissions, Edit, Delete
 * - Responsive column hiding
 */
export const RolesTable = React.memo(function RolesTable({
  roles,
  onManagePermissions,
  onEdit,
  onDelete,
}: RolesTableProps) {
  if (roles.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center rounded-lg border">
        <p className="text-muted-foreground">No roles found</p>
      </div>
    )
  }

  return (
    <div className="rounded-lg border overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/50">
            <TableHead>Name</TableHead>
            <TableHead className="hidden md:table-cell">Description</TableHead>
            <TableHead className="hidden lg:table-cell">Permissions</TableHead>
            <TableHead className="hidden sm:table-cell">Created</TableHead>
            <TableHead className="w-[80px] text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.id} className="group hover:bg-muted/30 transition-colors">
              <TableCell className="font-medium">
                <Badge variant="secondary">{role.name}</Badge>
              </TableCell>
              <TableCell className="hidden md:table-cell text-muted-foreground">
                {role.description || "-"}
              </TableCell>
              <TableCell className="hidden lg:table-cell">
                {role.permissions && role.permissions.length > 0 ? (
                  <div className="flex flex-wrap gap-1">
                    {role.permissions.slice(0, 3).map((permission) => (
                      <Badge key={permission.id} variant="outline" className="text-xs">
                        {permission.name}
                      </Badge>
                    ))}
                    {role.permissions.length > 3 && (
                      <Badge variant="outline" className="text-xs">
                        +{role.permissions.length - 3} more
                      </Badge>
                    )}
                  </div>
                ) : (
                  <span className="text-muted-foreground text-sm">No permissions</span>
                )}
              </TableCell>
              <TableCell className="hidden sm:table-cell text-muted-foreground text-sm">
                {formatDate(role.createdAt)}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      className="h-8 w-8 p-0 opacity-0 group-hover:opacity-100 focus:opacity-100 transition-opacity"
                      aria-label={`Actions for role ${role.name}`}
                    >
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => onManagePermissions(role)}>
                      <Shield className="mr-2 h-4 w-4" />
                      Manage Permissions
                    </DropdownMenuItem>
                    {onEdit && (
                      <DropdownMenuItem onClick={() => onEdit(role)}>
                        <Pencil className="mr-2 h-4 w-4" />
                        Edit
                      </DropdownMenuItem>
                    )}
                    {onDelete && (
                      <DropdownMenuItem
                        onClick={() => onDelete(role)}
                        className="text-destructive focus:text-destructive"
                      >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Delete
                      </DropdownMenuItem>
                    )}
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

