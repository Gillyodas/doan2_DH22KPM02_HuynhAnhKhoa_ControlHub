import { useState } from "react"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { rolesApi } from "@/services/api"
import { toast } from "sonner"
import { AlertTriangle } from "lucide-react"
import type { Role } from "@/services/api/types"

interface DeleteRoleDialogProps {
    role: Role | null
    accessToken: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onSuccess: () => void
}

/**
 * DeleteRoleDialog - Confirmation modal for deleting a role
 * 
 * Features:
 * - Shows role name being deleted
 * - Warning about users losing this role
 * - Loading state during deletion
 */
export function DeleteRoleDialog({
    role,
    accessToken,
    open,
    onOpenChange,
    onSuccess,
}: DeleteRoleDialogProps) {
    const [isLoading, setIsLoading] = useState(false)

    const handleDelete = async () => {
        if (!role) return

        setIsLoading(true)
        try {
            await rolesApi.deleteRole(role.id, accessToken)
            toast.success(`Role "${role.name}" deleted successfully`)
            onSuccess()
            onOpenChange(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to delete role")
        } finally {
            setIsLoading(false)
        }
    }

    if (!role) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-destructive/10">
                            <AlertTriangle className="h-5 w-5 text-destructive" />
                        </div>
                        <div>
                            <DialogTitle>Delete Role</DialogTitle>
                            <DialogDescription>
                                This action cannot be undone.
                            </DialogDescription>
                        </div>
                    </div>
                </DialogHeader>

                <div className="py-4 space-y-3">
                    <p className="text-sm text-muted-foreground">
                        Are you sure you want to delete the role{" "}
                        <span className="font-semibold text-foreground">"{role.name}"</span>?
                    </p>
                    <div className="rounded-lg bg-destructive/10 p-3">
                        <p className="text-sm text-destructive">
                            ⚠️ All users with this role will lose associated permissions.
                        </p>
                    </div>
                </div>

                <DialogFooter>
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() => onOpenChange(false)}
                        disabled={isLoading}
                    >
                        Cancel
                    </Button>
                    <Button
                        type="button"
                        variant="destructive"
                        onClick={handleDelete}
                        disabled={isLoading}
                    >
                        {isLoading ? "Deleting..." : "Delete Role"}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}
