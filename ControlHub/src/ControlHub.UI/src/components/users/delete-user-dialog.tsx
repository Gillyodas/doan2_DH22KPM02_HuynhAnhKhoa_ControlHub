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
import { usersApi } from "@/services/api"
import { toast } from "sonner"
import { AlertTriangle } from "lucide-react"
import type { User } from "@/services/api/types"

interface DeleteUserDialogProps {
    user: User | null
    accessToken: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onSuccess: () => void
}

/**
 * DeleteUserDialog - Confirmation modal for deleting a user
 * 
 * Features:
 * - Shows username being deleted
 * - Warning about permanent action
 * - Cancel/Confirm buttons
 * - Loading state during deletion
 * 
 * Accessibility:
 * - Uses destructive button variant
 * - Clear warning iconography
 */
export function DeleteUserDialog({
    user,
    accessToken,
    open,
    onOpenChange,
    onSuccess,
}: DeleteUserDialogProps) {
    const [isLoading, setIsLoading] = useState(false)

    const handleDelete = async () => {
        if (!user) return

        setIsLoading(true)
        try {
            await usersApi.deleteUser(user.id, accessToken)
            toast.success(`User "${user.username}" deleted successfully`)
            onSuccess()
            onOpenChange(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to delete user")
        } finally {
            setIsLoading(false)
        }
    }

    if (!user) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-destructive/10">
                            <AlertTriangle className="h-5 w-5 text-destructive" />
                        </div>
                        <div>
                            <DialogTitle>Delete User</DialogTitle>
                            <DialogDescription>
                                This action cannot be undone.
                            </DialogDescription>
                        </div>
                    </div>
                </DialogHeader>

                <div className="py-4">
                    <p className="text-sm text-muted-foreground">
                        Are you sure you want to delete the user{" "}
                        <span className="font-semibold text-foreground">"{user.username}"</span>?
                        All associated data will be permanently removed.
                    </p>
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
                        {isLoading ? "Deleting..." : "Delete User"}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}
