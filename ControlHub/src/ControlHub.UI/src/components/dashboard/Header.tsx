"use client"

import { Bell, ChevronRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/auth/use-auth"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

export function Header() {
  const { auth, signOut } = useAuth()

  const username = auth?.username ?? "Unknown"
  const initials = username
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join("")

  return (
    <header className="flex items-center justify-between h-16 px-6 bg-zinc-900 border-b border-zinc-800">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm">
        <span className="text-zinc-400">Home</span>
        <ChevronRight className="w-4 h-4 text-zinc-600" />
        <span className="font-medium text-zinc-100">Dashboard</span>
      </div>

      {/* Right Section */}
      <div className="flex items-center gap-3">
        {/* Notification Bell */}
        <Button variant="ghost" size="icon" className="relative text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800">
          <Bell className="w-5 h-5" />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full" />
        </Button>

        {/* User Profile Dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="flex items-center gap-2 px-2 hover:bg-zinc-800">
              <Avatar className="w-8 h-8">
                <AvatarImage src="/placeholder.svg?height=32&width=32" alt="User" />
                <AvatarFallback className="bg-zinc-700 text-zinc-200">{initials || "U"}</AvatarFallback>
              </Avatar>
              <div className="hidden md:block text-left">
                <p className="text-sm font-medium text-zinc-100">{username}</p>
                <p className="text-xs text-zinc-400">Authenticated</p>
              </div>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56 bg-zinc-900 border-zinc-800">
            <DropdownMenuLabel className="text-zinc-100">My Account</DropdownMenuLabel>
            <DropdownMenuSeparator className="bg-zinc-800" />
            <DropdownMenuItem className="text-zinc-300 focus:bg-zinc-800 focus:text-zinc-100">Profile</DropdownMenuItem>
            <DropdownMenuItem className="text-zinc-300 focus:bg-zinc-800 focus:text-zinc-100">
              Settings
            </DropdownMenuItem>
            <DropdownMenuItem className="text-zinc-300 focus:bg-zinc-800 focus:text-zinc-100">Billing</DropdownMenuItem>
            <DropdownMenuSeparator className="bg-zinc-800" />
            <DropdownMenuItem
              className="text-red-400 focus:bg-zinc-800 focus:text-red-300"
              onSelect={() => signOut()}
            >
              Log out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}