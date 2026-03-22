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
import { useTranslation } from "react-i18next"
import { Link } from "react-router-dom"

export function Header() {
  const { t } = useTranslation()
  const { auth, signOut } = useAuth()

  const username = auth?.username ?? "Unknown"
  const initials = username
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join("")

  return (
    <header className="flex items-center justify-between h-16 px-8 bg-sidebar/40 backdrop-blur-md border-b border-sidebar-border sticky top-0 z-40">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-xs font-bold tracking-widest uppercase">
        <span className="text-muted-foreground/60 hover:text-sidebar-primary transition-colors cursor-pointer">{t('navigation.protocol')}</span>
        <ChevronRight className="w-3 h-3 text-muted-foreground/30" />
        <span className="text-foreground italic">{t('navigation.nexusTerminal')}</span>
      </div>

      {/* Right Section */}
      <div className="flex items-center gap-4">
        {/* Notification Bell */}
        <Button variant="ghost" size="icon" className="relative text-muted-foreground hover:text-foreground hover:bg-sidebar-accent/50 rounded-xl transition-all">
          <Bell className="w-5 h-5" />
          <span className="absolute top-2 right-2 w-2 h-2 bg-sidebar-primary rounded-full shadow-[0_0_8px_var(--sidebar-primary)]" />
        </Button>

        {/* User Profile Dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="flex items-center gap-3 px-3 h-10 hover:bg-sidebar-accent/50 rounded-xl border border-transparent hover:border-sidebar-border/50 transition-all">
              <div className="relative">
                <Avatar className="w-8 h-8 border border-sidebar-border">
                  <AvatarImage src="/placeholder.svg?height=32&width=32" alt="User" />
                  <AvatarFallback className="bg-sidebar-accent text-sidebar-primary font-bold text-xs">{initials || "U"}</AvatarFallback>
                </Avatar>
                <div className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 bg-emerald-500 rounded-full border-2 border-sidebar" />
              </div>
              <div className="hidden md:block text-left">
                <p className="text-xs font-black tracking-tight text-foreground">{username}</p>
                <p className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-tighter">{t('navigation.verifiedOperator')}</p>
              </div>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-64 bg-sidebar/95 backdrop-blur-xl border-sidebar-border rounded-2xl p-2 shadow-2xl">
            <DropdownMenuLabel className="px-3 py-2 text-xs font-black text-muted-foreground uppercase tracking-widest">{t('navigation.accountMatrix')}</DropdownMenuLabel>
            <DropdownMenuSeparator className="bg-sidebar-border/50 mx-2" />
            <DropdownMenuItem asChild>
              <Link to="/profile" className="flex gap-3 px-3 py-2.5 text-sm font-semibold text-foreground focus:bg-sidebar-accent rounded-xl cursor-pointer">
                {t('navigation.userProfile')}
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link to="/settings" className="flex gap-3 px-3 py-2.5 text-sm font-semibold text-foreground focus:bg-sidebar-accent rounded-xl cursor-pointer">
                {t('navigation.heuristicSettings')}
              </Link>
            </DropdownMenuItem>
            <DropdownMenuSeparator className="bg-sidebar-border/50 mx-2" />
            <DropdownMenuItem
              className="flex gap-3 px-3 py-2.5 text-sm font-bold text-destructive focus:bg-destructive/10 rounded-xl cursor-pointer"
              onSelect={() => signOut()}
            >
              {t('navigation.terminateSession')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
