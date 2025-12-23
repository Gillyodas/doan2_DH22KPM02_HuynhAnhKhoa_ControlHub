"use client"

import { Outlet } from "react-router-dom"

import { Header } from "@/components/dashboard/header"
import { Sidebar } from "@/components/dashboard/sidebar"
import { cn } from "@/lib/utils"
import { useState } from "react"

export function MainLayout() {
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div className="flex h-screen overflow-hidden bg-black">
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed(!collapsed)} />
      <div className="flex flex-col flex-1 overflow-hidden">
        <Header />
        <main className={cn("flex-1 overflow-auto p-6 bg-zinc-950")}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
