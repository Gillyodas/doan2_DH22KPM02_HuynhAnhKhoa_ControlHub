"use client"

import { LayoutDashboard, Users, ShieldCheck, Settings, ChevronLeft, ChevronRight, Code2 } from "lucide-react"
import { NavLink } from "react-router-dom"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

const menuItems = [
  { icon: LayoutDashboard, label: "Dashboard", href: "/" },
  { icon: Users, label: "User Management", href: "/users" },
  { icon: ShieldCheck, label: "Roles & Permissions", href: "/roles" },
  { icon: Code2, label: "API", href: "/apis" },
  { icon: Settings, label: "Settings", href: "/settings" },
]

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  return (
    <aside
      className={cn(
        "relative flex flex-col bg-black text-zinc-100 transition-all duration-300 border-r border-zinc-800",
        collapsed ? "w-16" : "w-64",
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between h-16 px-4 border-b border-zinc-800">
        {!collapsed && <h1 className="text-xl font-semibold text-white">ControlHub</h1>}
        {collapsed && (
          <div className="w-full flex justify-center">
            <span className="text-xl font-bold text-white">C</span>
          </div>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-4">
        {menuItems.map((item) => (
          <NavLink
            key={item.label}
            to={item.href}
            className={({ isActive }: { isActive: boolean }) =>
              cn(
                "flex items-center gap-3 px-4 py-3 mx-2 rounded-lg transition-colors hover:bg-zinc-900",
                isActive ? "bg-zinc-900 text-white" : "text-zinc-300 hover:text-white",
              )
            }
            end={item.href === "/"}
          >
            <item.icon className="w-5 h-5 shrink-0" />
            {!collapsed && <span className="text-sm font-medium">{item.label}</span>}
          </NavLink>
        ))}
      </nav>

      {/* Toggle Button */}
      <div className="p-4 border-t border-zinc-800">
        <Button
          variant="ghost"
          size="icon"
          onClick={onToggle}
          className="w-full text-zinc-400 hover:text-white hover:bg-zinc-900"
        >
          {collapsed ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
        </Button>
      </div>
    </aside>
  )
}
