import { useEffect, useState } from "react"
import { ProfileForm } from "@/components/account/profile-form"
import { LoadingState } from "@/components/ui/loading-state"
import { profileApi } from "@/services/api"
import { useAuth } from "@/auth/use-auth"
import { toast } from "sonner"
import type { Profile } from "@/services/api/types"

/**
 * ProfilePage - View and edit current user's profile
 * 
 * Features:
 * - View profile information
 * - Edit display name and email
 * - Avatar display with fallback
 */
export function ProfilePage() {
    const { auth } = useAuth()
    const [profile, setProfile] = useState<Profile | null>(null)
    const [isLoading, setIsLoading] = useState(true)

    useEffect(() => {
        loadProfile()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    const loadProfile = async () => {
        if (!auth?.accessToken) return

        setIsLoading(true)
        try {
            const result = await profileApi.getMyProfile(auth.accessToken)
            setProfile(result)
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Failed to load profile")
        } finally {
            setIsLoading(false)
        }
    }

    const handleProfileUpdate = (updatedProfile: Profile) => {
        setProfile(updatedProfile)
    }

    if (isLoading) {
        return <LoadingState message="Loading profile..." />
    }

    if (!profile || !auth) {
        return (
            <div className="flex flex-col items-center justify-center py-12 text-center">
                <p className="text-muted-foreground">Failed to load profile</p>
            </div>
        )
    }

    return (
        <div className="max-w-2xl">
            <div className="mb-6">
                <h1 className="text-3xl font-bold">My Profile</h1>
                <p className="text-muted-foreground">
                    View and update your personal information
                </p>
            </div>

            <ProfileForm
                profile={profile}
                accessToken={auth.accessToken}
                onUpdate={handleProfileUpdate}
            />
        </div>
    )
}
