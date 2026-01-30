import { useState, useEffect, useCallback } from "react"
import { useAuth } from "@/auth/use-auth"
import { getMyProfile, updateMyProfile as updateProfileApi } from "@/services/api/profile"
import { Profile, UpdateProfileRequest } from "@/services/api/types"
import { toast } from "sonner"

interface UseProfileReturn {
    profile: Profile | null
    isLoading: boolean
    isUpdating: boolean
    error: string | null
    refetch: () => Promise<void>
    updateProfile: (data: UpdateProfileRequest) => Promise<boolean>
}

export function useProfile(): UseProfileReturn {
    const { accessToken } = useAuth()
    const [profile, setProfile] = useState<Profile | null>(null)
    const [isLoading, setIsLoading] = useState(false)
    const [isUpdating, setIsUpdating] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const fetchProfile = useCallback(async () => {
        if (!accessToken) return

        setIsLoading(true)
        setError(null)
        try {
            const data = await getMyProfile(accessToken)
            setProfile(data)
        } catch (err: any) {
            console.error("Failed to fetch profile:", err)
            setError(err.message || "Failed to load profile")
            // Don't toast on initial load failure to avoid spamming if auth is resolving
        } finally {
            setIsLoading(false)
        }
    }, [accessToken])

    useEffect(() => {
        if (accessToken) {
            fetchProfile()
        }
    }, [fetchProfile, accessToken])

    const updateProfile = async (data: UpdateProfileRequest): Promise<boolean> => {
        if (!accessToken) return false

        setIsUpdating(true)
        try {
            const updatedProfile = await updateProfileApi(data, accessToken)
            setProfile(updatedProfile)
            toast.success("Profile updated successfully")
            return true
        } catch (err: any) {
            console.error("Failed to update profile:", err)
            toast.error(err.message || "Failed to update profile")
            return false
        } finally {
            setIsUpdating(false)
        }
    }

    return {
        profile,
        isLoading,
        isUpdating,
        error,
        refetch: fetchProfile,
        updateProfile,
    }
}
