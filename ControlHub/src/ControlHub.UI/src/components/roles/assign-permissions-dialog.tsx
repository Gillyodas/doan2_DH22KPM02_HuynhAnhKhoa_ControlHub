import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { rolesApi, permissionsApi, type Role, type Permission } from "@/services/api"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

interface AssignPermissionsDialogProps {
  role: Role | null
  accessToken: string
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function AssignPermissionsDialog({ role, accessToken, open, onOpenChange, onSuccess }: AssignPermissionsDialogProps) {
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(new Set())
  const [isLoading, setIsLoading] = useState(false)
  const [isFetching, setIsFetching] = useState(false)

  useEffect(() => {
    if (open && role) {
      loadPermissions()
      const existingPermissionIds = new Set(role.permissions?.map((p) => p.id) || [])
      setSelectedPermissions(existingPermissionIds)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, role])

  const loadPermissions = async () => {
    setIsFetching(true)
    try {
      const result = await permissionsApi.getPermissions(
        { pageIndex: 1, pageSize: 100 },
        accessToken
      )
      setPermissions(result.items)
    } catch {
      toast.error("Failed to load permissions")
    } finally {
      setIsFetching(false)
    }
  }

  const togglePermission = (permissionId: string) => {
    const newSelected = new Set(selectedPermissions)
    if (newSelected.has(permissionId)) {
      newSelected.delete(permissionId)
    } else {
      newSelected.add(permissionId)
    }
    setSelectedPermissions(newSelected)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!role) return

    setIsLoading(true)
    try {
      await rolesApi.addPermissionsForRole(
        role.id,
        Array.from(selectedPermissions),
        accessToken
      )

      toast.success("Permissions updated successfully")

      onSuccess()
      onOpenChange(false)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Failed to assign permissions")
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[625px]">
        <DialogHeader>
          <DialogTitle>Manage Permissions for {role?.name}</DialogTitle>
          <DialogDescription>Select permissions to assign to this role</DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="py-4">
            {isFetching ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin" />
              </div>
            ) : (
              <div className="max-h-[400px] overflow-y-auto space-y-2">
                {permissions.length === 0 ? (
                  <p className="text-center text-muted-foreground py-4">No permissions available</p>
                ) : (
                  permissions.map((permission) => (
                    <div
                      key={permission.id}
                      className="flex items-center space-x-2 p-3 rounded-lg border hover:bg-accent cursor-pointer"
                      onClick={() => togglePermission(permission.id)}
                    >
                      <input
                        type="checkbox"
                        checked={selectedPermissions.has(permission.id)}
                        onChange={() => togglePermission(permission.id)}
                        className="h-4 w-4 cursor-pointer"
                      />
                      <div className="flex-1">
                        <Label className="font-medium cursor-pointer">{permission.name}</Label>
                        {permission.description && (
                          <p className="text-sm text-muted-foreground">{permission.description}</p>
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isLoading || isFetching}>
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                "Save Permissions"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
