import { useEffect, useState } from "react"
import { useAuth } from "@/auth/use-auth"
import { getAdminAccounts, registerAdmin } from "@/services/api/account"
import type { AccountDto } from "@/services/api/types"
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Loader2, Plus, ShieldAlert, CheckCircle2, Eye, EyeOff } from "lucide-react"

export function AdminAccountsPage() {
    const { auth } = useAuth()
    const accessToken = auth?.accessToken

    const [admins, setAdmins] = useState<AccountDto[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    // Form State
    const [email, setEmail] = useState("")
    const [password, setPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [creating, setCreating] = useState(false)
    const [createError, setCreateError] = useState<string | null>(null)
    const [createSuccess, setCreateSuccess] = useState<string | null>(null)
    const [showPassword, setShowPassword] = useState(false)
    const [showConfirmPassword, setShowConfirmPassword] = useState(false)

    async function loadAdmins() {
        if (!accessToken) return
        try {
            setLoading(true)
            const data = await getAdminAccounts(accessToken)
            setAdmins(data)
        } catch (err) {
            setError(err instanceof Error ? err.message : "Failed to load admin accounts")
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadAdmins()
    }, [accessToken])

    async function handleCreateAdmin(e: React.FormEvent) {
        e.preventDefault()
        if (!accessToken) return

        setCreateError(null)
        setCreateSuccess(null)

        // Basic Validation
        if (!email.trim() || !password || !confirmPassword) {
            setCreateError("All fields are required.")
            return
        }
        if (password !== confirmPassword) {
            setCreateError("Passwords do not match.")
            return
        }
        if (password.length < 8) {
            setCreateError("Password must be at least 8 characters.")
            return
        }

        try {
            setCreating(true)
            await registerAdmin({
                value: email,
                password: password,
                type: 0 // Email
            }, accessToken)

            setCreateSuccess("Admin account created successfully!")
            setEmail("")
            setPassword("")
            setConfirmPassword("")
            loadAdmins() // Refresh list
        } catch (err) {
            setCreateError(err instanceof Error ? err.message : "Failed to create admin")
        } finally {
            setCreating(false)
        }
    }

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-3xl font-bold tracking-tight bg-gradient-to-r from-white to-gray-400 bg-clip-text text-transparent">
                    Admin Accounts
                </h2>
                <p className="text-muted-foreground mt-2">
                    Manage administrative accounts for the system.
                </p>
            </div>

            {/* Create Admin Form Section */}
            <Card className="border-sidebar-border bg-sidebar/50 backdrop-blur-sm">
                <CardHeader>
                    <CardTitle>Create New Admin</CardTitle>
                    <CardDescription>Add a new administrator to the system.</CardDescription>
                </CardHeader>
                <form onSubmit={handleCreateAdmin}>
                    <CardContent className="space-y-4">
                        {createError && (
                            <div className="text-sm text-red-400 bg-red-500/10 border border-red-500/20 p-3 rounded-md flex items-center gap-2">
                                <ShieldAlert className="h-4 w-4" />
                                {createError}
                            </div>
                        )}
                        {createSuccess && (
                            <div className="text-sm text-green-400 bg-green-500/10 border border-green-500/20 p-3 rounded-md flex items-center gap-2">
                                <CheckCircle2 className="h-4 w-4" />
                                {createSuccess}
                            </div>
                        )}

                        <div className="grid gap-4 md:grid-cols-3">
                            <div className="space-y-2">
                                <Label htmlFor="email">Email / Username</Label>
                                <Input
                                    id="email"
                                    placeholder="admin@example.com"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    className="bg-zinc-950/50"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="password">Password</Label>
                                <div className="relative">
                                    <Input
                                        id="password"
                                        type={showPassword ? "text" : "password"}
                                        placeholder="••••••••"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        className="bg-zinc-950/50 pr-10"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowPassword(!showPassword)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200 transition-colors"
                                    >
                                        {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                    </button>
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="confirmPassword">Confirm Password</Label>
                                <div className="relative">
                                    <Input
                                        id="confirmPassword"
                                        type={showConfirmPassword ? "text" : "password"}
                                        placeholder="••••••••"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        className="bg-zinc-950/50 pr-10"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-200 transition-colors"
                                    >
                                        {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                    <CardFooter>
                        <Button type="submit" disabled={creating} className="w-full md:w-auto">
                            {creating ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Creating...
                                </>
                            ) : (
                                <>
                                    <Plus className="mr-2 h-4 w-4" />
                                    Create Account
                                </>
                            )}
                        </Button>
                    </CardFooter>
                </form>
            </Card>

            {error && (
                <div className="rounded-lg border border-red-500/20 bg-red-500/10 p-4 text-red-400 flex items-center gap-3">
                    <ShieldAlert className="h-5 w-5" />
                    {error}
                </div>
            )}

            {loading ? (
                <div className="flex justify-center p-12">
                    <Loader2 className="h-8 w-8 animate-spin text-primary" />
                </div>
            ) : (
                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {admins.map((admin) => (
                        <Card key={admin.id} className="bg-sidebar/50 border-sidebar-border backdrop-blur-sm">
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-lg font-medium">
                                    {admin.username}
                                </CardTitle>
                                <Badge variant={admin.isActive ? "default" : "destructive"}>
                                    {admin.isActive ? "Active" : "Inactive"}
                                </Badge>
                            </CardHeader>
                            <CardContent>
                                <div className="text-sm text-muted-foreground">
                                    Role: <span className="text-foreground font-medium">{admin.roleName}</span>
                                </div>
                                <div className="text-xs text-zinc-500 mt-4 font-mono">
                                    ID: {admin.id}
                                </div>
                            </CardContent>
                        </Card>
                    ))}

                    {admins.length === 0 && !error && (
                        <div className="col-span-full text-center p-12 text-muted-foreground border border-dashed border-zinc-800 rounded-lg">
                            No admin accounts found.
                        </div>
                    )}
                </div>
            )}
        </div>
    )
}
