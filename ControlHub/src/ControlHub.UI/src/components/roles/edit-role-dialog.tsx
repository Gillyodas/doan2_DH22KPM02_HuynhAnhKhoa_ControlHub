import { useState, useEffect } from "react"
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
import { Textarea } from "@/components/ui/textarea"
import { rolesApi } from "@/services/api"
import { toast } from "sonner"
import type { Role } from "@/services/api/types"

interface EditRoleDialogProps {
    role: Role | null
    accessToken: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onSuccess: () => void
}

/**
 * EditRoleDialog - Modal for editing role details
 * 
 * Features:
 * - Edit role name and description
 * - Form validation (name required)
 * - Loading state during submission
 */
export function EditRoleDialog({
    role,
    accessToken,
    open,
    onOpenChange,
    onSuccess,
}: EditRoleDialogProps) {
    const [name, setName] = useState("")
    const [description, setDescription] = useState("")
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState("")

    // Sync state when role changes
    useEffect(() => {
        if (open && role) {
            setName(role.name)
            setDescription(role.description || "")
            setError("")
        }
    }, [open, role])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (!role) return

        // Validation
        if (!name.trim()) {
            setError("Role name is required")
            return
        }

        if (name.trim().length < 2) {
            setError("Role name must be at least 2 characters")
            return
        }

        setIsLoading(true)
        setError("")

        try {
            await rolesApi.updateRole(
                role.id,
                {
                    name: name.trim(),
                    description: description.trim() || undefined
                },
                accessToken
            )
            toast.success("Role updated successfully")
            onSuccess()
            onOpenChange(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to update role")
        } finally {
            setIsLoading(false)
        }
    }

    if (!role) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Edit Role</DialogTitle>
                        <DialogDescription>
                            Update role information and permissions scope.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                        <div className="grid gap-2">
                            <Label htmlFor="role-name">
                                Role Name <span className="text-destructive">*</span>
                            </Label>
                            <Input
                                id="role-name"
                                placeholder="e.g., Administrator"
                                value={name}
                                onChange={(e) => {
                                    setName(e.target.value)
                                    setError("")
                                }}
                                disabled={isLoading}
                                aria-invalid={!!error}
                                aria-describedby={error ? "name-error" : undefined}
                            />
                            {error && (
                                <p id="name-error" className="text-sm text-destructive">
                                    {error}
                                </p>
                            )}
                        </div>

                        <div className="grid gap-2">
                            <Label htmlFor="role-description">Description</Label>
                            <Textarea
                                id="role-description"
                                placeholder="Brief description of this role's responsibilities..."
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                disabled={isLoading}
                                rows={3}
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
