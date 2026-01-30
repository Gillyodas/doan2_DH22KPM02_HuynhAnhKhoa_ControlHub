import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { profileApi } from "@/services/api"
import { toast } from "sonner"
import { Pencil, Save, X } from "lucide-react"
import type { Profile } from "@/services/api/types"

interface ProfileFormProps {
    profile: Profile
    accessToken: string
    onUpdate: (updatedProfile: Profile) => void
}

/**
 * ProfileForm - Form for viewing and editing user profile
 * 
 * Features:
 * - View mode by default
 * - Edit mode toggle
 * - Form validation
 * - Avatar display with fallback
 * 
 * Accessibility:
 * - Form labels linked to inputs
 * - Clear edit/save/cancel actions
 */
export function ProfileForm({
    profile,
    accessToken,
    onUpdate,
}: ProfileFormProps) {
    const [isEditing, setIsEditing] = useState(false)
    const [isLoading, setIsLoading] = useState(false)
    const [displayName, setDisplayName] = useState(profile.displayName || "")
    const [email, setEmail] = useState(profile.email || "")

    const handleCancel = () => {
        setDisplayName(profile.displayName || "")
        setEmail(profile.email || "")
        setIsEditing(false)
    }

    const handleSave = async () => {
        setIsLoading(true)
        try {
            const updatedProfile = await profileApi.updateMyProfile(
                {
                    displayName: displayName.trim() || undefined,
                    email: email.trim() || undefined,
                },
                accessToken
            )
            toast.success("Profile updated successfully")
            onUpdate(updatedProfile)
            setIsEditing(false)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to update profile")
        } finally {
            setIsLoading(false)
        }
    }

    // Generate initials for avatar fallback
    const getInitials = () => {
        if (profile.displayName) {
            return profile.displayName
                .split(" ")
                .map((n) => n[0])
                .join("")
                .toUpperCase()
                .slice(0, 2)
        }
        return profile.username.slice(0, 2).toUpperCase()
    }

    return (
        <div className="space-y-6">
            {/* Avatar Section */}
            <div className="flex items-center gap-4">
                <Avatar className="h-20 w-20">
                    <AvatarImage src={profile.avatarUrl} alt={profile.username} />
                    <AvatarFallback className="text-lg bg-primary/10 text-primary">
                        {getInitials()}
                    </AvatarFallback>
                </Avatar>
                <div>
                    <h2 className="text-xl font-semibold">
                        {profile.displayName || profile.username}
                    </h2>
                    <p className="text-muted-foreground">@{profile.username}</p>
                </div>
            </div>

            {/* Form Section */}
            <div className="rounded-lg border p-6 space-y-4">
                <div className="flex items-center justify-between">
                    <h3 className="font-semibold">Profile Information</h3>
                    {!isEditing ? (
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setIsEditing(true)}
                        >
                            <Pencil className="h-4 w-4 mr-2" />
                            Edit
                        </Button>
                    ) : (
                        <div className="flex gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleCancel}
                                disabled={isLoading}
                            >
                                <X className="h-4 w-4 mr-2" />
                                Cancel
                            </Button>
                            <Button
                                size="sm"
                                onClick={handleSave}
                                disabled={isLoading}
                            >
                                <Save className="h-4 w-4 mr-2" />
                                {isLoading ? "Saving..." : "Save"}
                            </Button>
                        </div>
                    )}
                </div>

                <div className="grid gap-4">
                    {/* Username (readonly) */}
                    <div className="grid gap-2">
                        <Label htmlFor="username">Username</Label>
                        <Input
                            id="username"
                            value={profile.username}
                            disabled
                            className="bg-muted"
                        />
                        <p className="text-xs text-muted-foreground">
                            Username cannot be changed from this page
                        </p>
                    </div>

                    {/* Display Name */}
                    <div className="grid gap-2">
                        <Label htmlFor="displayName">Display Name</Label>
                        {isEditing ? (
                            <Input
                                id="displayName"
                                placeholder="Your display name"
                                value={displayName}
                                onChange={(e) => setDisplayName(e.target.value)}
                                disabled={isLoading}
                            />
                        ) : (
                            <p className="py-2 px-3 rounded-md border bg-muted/30">
                                {profile.displayName || <span className="text-muted-foreground">Not set</span>}
                            </p>
                        )}
                    </div>

                    {/* Email */}
                    <div className="grid gap-2">
                        <Label htmlFor="email">Email</Label>
                        {isEditing ? (
                            <Input
                                id="email"
                                type="email"
                                placeholder="your.email@example.com"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                disabled={isLoading}
                            />
                        ) : (
                            <p className="py-2 px-3 rounded-md border bg-muted/30">
                                {profile.email || <span className="text-muted-foreground">Not set</span>}
                            </p>
                        )}
                    </div>

                    {/* Account ID (readonly) */}
                    <div className="grid gap-2">
                        <Label htmlFor="accountId">Account ID</Label>
                        <Input
                            id="accountId"
                            value={profile.accountId}
                            disabled
                            className="bg-muted font-mono text-sm"
                        />
                    </div>
                </div>
            </div>
        </div>
    )
}
