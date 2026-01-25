"use client"

import { LayoutDashboard, ShieldCheck, Settings, ChevronLeft, ChevronRight, Code2, Fingerprint, Sparkles, ShieldAlert } from "lucide-react"
import { NavLink } from "react-router-dom"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { useTranslation } from "react-i18next"

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  const { t } = useTranslation()

  const menuItems = [
    { icon: LayoutDashboard, label: t('navigation.dashboard'), href: "/" },
    { icon: ShieldCheck, label: t('navigation.roles'), href: "/roles" },
    { icon: ShieldAlert, label: "Admins", href: "/admin-accounts" },
    { icon: Fingerprint, label: t('navigation.identifiers'), href: "/identifiers" },
    { icon: Code2, label: t('navigation.apis'), href: "/apis" },
    { icon: Sparkles, label: t('navigation.aiAudit', 'AI Audit'), href: "/ai-audit" },
    { icon: Settings, label: t('navigation.settings'), href: "/settings" },
  ]

  return (
    <aside
      className={cn(
        "relative flex flex-col bg-sidebar text-sidebar-foreground transition-all duration-300 border-r border-sidebar-border",
        collapsed ? "w-16" : "w-64",
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between h-16 px-4 border-b border-sidebar-border">
        {!collapsed && (
          <h1 className="text-xl font-bold bg-[var(--vibrant-gradient)] bg-clip-text text-transparent">
            ControlHub
          </h1>
        )}
        {collapsed && (
          <div className="w-full flex justify-center">
            <span className="text-xl font-bold bg-[var(--vibrant-gradient)] bg-clip-text text-transparent">
              C
            </span>
          </div>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-4 space-y-1">
        {menuItems.map((item) => (
          <NavLink
            key={item.label}
            to={item.href}
            className={({ isActive }: { isActive: boolean }) =>
              cn(
                "group relative flex items-center gap-3 px-4 py-3 mx-2 rounded-lg transition-all duration-200",
                isActive
                  ? "bg-sidebar-accent text-sidebar-primary shadow-[inset_0_0_20px_rgba(0,0,0,0.2)]"
                  : "text-sidebar-foreground/60 hover:text-sidebar-foreground hover:bg-sidebar-accent/50",
              )
            }
            end={item.href === "/"}
          >
            {({ isActive }: { isActive: boolean }) => (
              <>
                <item.icon className={cn(
                  "w-5 h-5 shrink-0 transition-transform group-hover:scale-110",
                  isActive ? "text-sidebar-primary" : "text-sidebar-foreground/40 group-hover:text-sidebar-foreground"
                )} />
                {!collapsed && <span className="text-sm font-semibold">{item.label}</span>}
                {isActive && (
                  <div className="absolute left-0 w-1 h-6 rounded-r-full bg-sidebar-primary shadow-[0_0_10px_var(--sidebar-primary)]" />
                )}
              </>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Toggle Button */}
      <div className="p-4 border-t border-sidebar-border">
        <Button
          variant="ghost"
          size="icon"
          onClick={onToggle}
          className="w-full text-muted-foreground hover:text-foreground hover:bg-sidebar-accent"
        >
          {collapsed ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
        </Button>
      </div>
    </aside>
  )
}
