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
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { usersApi } from "@/services/api"
import { toast } from "sonner"
import type { User } from "@/services/api/types"

interface EditUserDialogProps {
    user: User | null
    accessToken: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onSuccess: () => void
}

/**
 * EditUserDialog - Modal for editing user details
 * 
 * Features:
 * - Edit email (username is readonly for identity)
 * - Form validation
 * - Loading state during submission
 * 
 * Accessibility:
 * - Uses Dialog with proper aria attributes
 * - Focus trap inside modal
 * - Form labels linked to inputs
 */
export function EditUserDialog({
    user,
    accessToken,
    open,
    onOpenChange,
    onSuccess,
}: EditUserDialogProps) {
    const [email, setEmail] = useState(user?.email || "")
    const [isLoading, setIsLoading] = useState(false)

    // Sync state when user changes
    const handleOpenChange = (isOpen: boolean) => {
        if (isOpen && user) {
            setEmail(user.email || "")
        }
        onOpenChange(isOpen)
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (!user) return

        setIsLoading(true)
        try {
            await usersApi.updateUser(
                user.id,
                { email: email || undefined },
                accessToken
            )
            toast.success("User updated successfully")
            onSuccess()
            onOpenChange(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to update user")
        } finally {
            setIsLoading(false)
        }
    }

    if (!user) return null

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Edit User</DialogTitle>
                        <DialogDescription>
                            Update user information. Username cannot be changed.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                        <div className="grid gap-2">
                            <Label htmlFor="username">Username</Label>
                            <Input
                                id="username"
                                value={user.username}
                                disabled
                                className="bg-muted"
                                aria-describedby="username-hint"
                            />
                            <p id="username-hint" className="text-xs text-muted-foreground">
                                Username cannot be modified
                            </p>
                        </div>

                        <div className="grid gap-2">
                            <Label htmlFor="email">Email</Label>
                            <Input
                                id="email"
                                type="email"
                                placeholder="user@example.com"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                disabled={isLoading}
                            />
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
                        <Button type="submit" disabled={isLoading}>
                            {isLoading ? "Saving..." : "Save Changes"}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
