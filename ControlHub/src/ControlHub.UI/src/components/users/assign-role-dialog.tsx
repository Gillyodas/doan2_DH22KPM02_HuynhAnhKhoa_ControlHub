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
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { rolesApi, usersApi } from "@/services/api"
import { toast } from "sonner"
import { Search, Check, Loader2 } from "lucide-react"
import type { User, Role, PagedResult } from "@/services/api/types"

interface AssignRoleDialogProps {
    user: User | null
    accessToken: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onSuccess: () => void
}

/**
 * AssignRoleDialog - Modal for assigning/removing roles from a user
 * 
 * Features:
 * - List available roles
 * - Show currently assigned roles (checked)
 * - Search/filter roles
 * - Toggle role assignment
 * 
 * Accessibility:
 * - Keyboard navigable list
 * - Clear visual indicators for assigned roles
 */
export function AssignRoleDialog({
    user,
    accessToken,
    open,
    onOpenChange,
    onSuccess,
}: AssignRoleDialogProps) {
    const [allRoles, setAllRoles] = useState<Role[]>([])
    const [isLoadingRoles, setIsLoadingRoles] = useState(false)
    const [searchTerm, setSearchTerm] = useState("")
    const [pendingChanges, setPendingChanges] = useState<Map<string, boolean>>(new Map())
    const [isSaving, setIsSaving] = useState(false)

    // Load all roles when dialog opens
    useEffect(() => {
        if (open && accessToken) {
            loadRoles()
        }
    }, [open, accessToken])

    // Reset state when user changes
    useEffect(() => {
        if (open && user) {
            setPendingChanges(new Map())
            setSearchTerm("")
        }
    }, [open, user])

    const loadRoles = async () => {
        setIsLoadingRoles(true)
        try {
            const result: PagedResult<Role> = await rolesApi.getRoles(
                { pageSize: 100 }, // Get all roles 
                accessToken
            )
            setAllRoles(result.items)
        } catch (error) {
            toast.error("Failed to load roles")
        } finally {
            setIsLoadingRoles(false)
        }
    }

    const isRoleAssigned = (roleId: string): boolean => {
        // Check pending changes first
        if (pendingChanges.has(roleId)) {
            return pendingChanges.get(roleId)!
        }
        // Then check current user roles
        return user?.roles?.some(r => r.id === roleId) || false
    }

    const toggleRole = (roleId: string) => {
        const currentlyAssigned = user?.roles?.some(r => r.id === roleId) || false
        const pendingState = pendingChanges.get(roleId)

        // If we have a pending change that matches the original state, remove it
        if (pendingState !== undefined && pendingState === currentlyAssigned) {
            const newChanges = new Map(pendingChanges)
            newChanges.delete(roleId)
            setPendingChanges(newChanges)
        } else {
            // Toggle the state
            const newChanges = new Map(pendingChanges)
            newChanges.set(roleId, !isRoleAssigned(roleId))
            setPendingChanges(newChanges)
        }
    }

    const handleSave = async () => {
        if (!user || pendingChanges.size === 0) {
            onOpenChange(false)
            return
        }

        setIsSaving(true)
        try {
            const promises: Promise<void>[] = []

            pendingChanges.forEach((shouldAssign, roleId) => {
                if (shouldAssign) {
                    promises.push(usersApi.assignRoleToUser(user.id, roleId, accessToken))
                } else {
                    promises.push(usersApi.removeRoleFromUser(user.id, roleId, accessToken))
                }
            })

            await Promise.all(promises)
            toast.success("Roles updated successfully")
            onSuccess()
            onOpenChange(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to update roles")
        } finally {
            setIsSaving(false)
        }
    }

    const filteredRoles = allRoles.filter(role =>
        role.name.toLowerCase().includes(searchTerm.toLowerCase())
    )

    if (!user) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Assign Roles</DialogTitle>
                    <DialogDescription>
                        Manage roles for user <span className="font-semibold">{user.username}</span>
                    </DialogDescription>
                </DialogHeader>

                <div className="space-y-4 py-4">
                    {/* Search Input */}
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <Input
                            placeholder="Search roles..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="pl-10"
                        />
                    </div>

                    {/* Roles List */}
                    <div className="max-h-[300px] overflow-y-auto rounded-lg border">
                        {isLoadingRoles ? (
                            <div className="flex items-center justify-center py-8">
                                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                            </div>
                        ) : filteredRoles.length === 0 ? (
                            <p className="py-8 text-center text-muted-foreground">
                                No roles found
                            </p>
                        ) : (
                            <div className="divide-y">
                                {filteredRoles.map((role) => {
                                    const assigned = isRoleAssigned(role.id)
                                    const hasPendingChange = pendingChanges.has(role.id)

                                    return (
                                        <button
                                            key={role.id}
                                            type="button"
                                            onClick={() => toggleRole(role.id)}
                                            className={`flex w-full items-center justify-between px-4 py-3 text-left transition-colors hover:bg-muted/50 ${assigned ? "bg-primary/5" : ""
                                                }`}
                                        >
                                            <div className="flex items-center gap-3">
                                                <div
                                                    className={`flex h-5 w-5 items-center justify-center rounded border transition-colors ${assigned
                                                        ? "border-primary bg-primary text-primary-foreground"
                                                        : "border-muted-foreground"
                                                        }`}
                                                >
                                                    {assigned && <Check className="h-3 w-3" />}
                                                </div>
                                                <div>
                                                    <p className="font-medium">{role.name}</p>
                                                    {role.description && (
                                                        <p className="text-xs text-muted-foreground">
                                                            {role.description}
                                                        </p>
                                                    )}
                                                </div>
                                            </div>
                                            {hasPendingChange && (
                                                <Badge variant="outline" className="text-xs">
                                                    {assigned ? "Adding" : "Removing"}
                                                </Badge>
                                            )}
                                        </button>
                                    )
                                })}
                            </div>
                        )}
                    </div>

                    {/* Pending Changes Summary */}
                    {pendingChanges.size > 0 && (
                        <p className="text-sm text-muted-foreground">
                            {pendingChanges.size} pending change{pendingChanges.size > 1 ? "s" : ""}
                        </p>
                    )}
                </div>

                <DialogFooter>
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() => onOpenChange(false)}
                        disabled={isSaving}
                    >
                        Cancel
                    </Button>
                    <Button
                        type="button"
                        onClick={handleSave}
                        disabled={isSaving || pendingChanges.size === 0}
                    >
                        {isSaving ? "Saving..." : "Save Changes"}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}
