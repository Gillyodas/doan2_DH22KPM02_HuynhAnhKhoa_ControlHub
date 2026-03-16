import { useState, useMemo } from "react"
import { ChevronDown, ChevronRight, Lock, Globe, Search, Copy, Check, Terminal } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { useTranslation } from "react-i18next"

interface ApiEndpoint {
  path: string
  method: string
  descriptionKey: string
  controller: string
  requiresAuth: boolean
}

interface ApiGroup {
  nameKey: string
  endpoints: ApiEndpoint[]
}

const API_GROUPS: ApiGroup[] = [
  {
    nameKey: "identityAuth",
    endpoints: [
      { path: "/api/auth/signin", method: "POST", descriptionKey: "login", controller: "AuthController", requiresAuth: false },
      { path: "/api/auth/users/register", method: "POST", descriptionKey: "register", controller: "AuthController", requiresAuth: false },
      { path: "/api/auth/admins/register", method: "POST", descriptionKey: "registerAdmin", controller: "AuthController", requiresAuth: true },
      { path: "/api/auth/refresh", method: "POST", descriptionKey: "refreshToken", controller: "AuthController", requiresAuth: false },
      { path: "/api/account/auth/forgot-password", method: "POST", descriptionKey: "forgotPassword", controller: "AccountController", requiresAuth: false },
      { path: "/api/account/auth/reset-password", method: "POST", descriptionKey: "resetPassword", controller: "AccountController", requiresAuth: false },
      { path: "/api/account/users/{id}/password", method: "PATCH", descriptionKey: "changePassword", controller: "AccountController", requiresAuth: true },
    ]
  },
  {
    nameKey: "userHeuristics",
    endpoints: [
      { path: "/api/user/users/{id}/username", method: "PATCH", descriptionKey: "updateUsername", controller: "UserController", requiresAuth: true },
    ]
  },
  {
    nameKey: "roleProtocols",
    endpoints: [
      { path: "/api/role", method: "GET", descriptionKey: "getRoles", controller: "RoleController", requiresAuth: true },
      { path: "/api/role/{id}", method: "GET", descriptionKey: "getRole", controller: "RoleController", requiresAuth: true },
      { path: "/api/role", method: "POST", descriptionKey: "createRole", controller: "RoleController", requiresAuth: true },
      { path: "/api/role/{id}", method: "PUT", descriptionKey: "updateRole", controller: "RoleController", requiresAuth: true },
      { path: "/api/role/{id}", method: "DELETE", descriptionKey: "deleteRole", controller: "RoleController", requiresAuth: true },
    ]
  },
  {
    nameKey: "permissionsHub",
    endpoints: [
      { path: "/api/permission", method: "GET", descriptionKey: "getPermissions", controller: "PermissionController", requiresAuth: true },
      { path: "/api/permission/{id}", method: "GET", descriptionKey: "getPermission", controller: "PermissionController", requiresAuth: true },
      { path: "/api/permission", method: "POST", descriptionKey: "createPermission", controller: "PermissionController", requiresAuth: true },
    ]
  },
  {
    nameKey: "identifierGates",
    endpoints: [
      { path: "/api/identifier", method: "GET", descriptionKey: "getIdentifiers", controller: "IdentifierController", requiresAuth: true },
      { path: "/api/identifier", method: "POST", descriptionKey: "createIdentifier", controller: "IdentifierController", requiresAuth: true },
    ]
  },
]

export default function ApiExplorerPage() {
  const { t } = useTranslation()
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set([API_GROUPS[0].nameKey]))
  const [searchQuery, setSearchQuery] = useState("")
  const [copiedPath, setCopiedPath] = useState<string | null>(null)

  const toggleGroup = (groupKey: string) => {
    const newExpanded = new Set(expandedGroups)
    if (newExpanded.has(groupKey)) {
      newExpanded.delete(groupKey)
    } else {
      newExpanded.add(groupKey)
    }
    setExpandedGroups(newExpanded)
  }

  const handleCopy = (path: string) => {
    navigator.clipboard.writeText(`https://localhost:7110${path}`)
    setCopiedPath(path)
    setTimeout(() => setCopiedPath(null), 2000)
  }

  const getMethodStyles = (method: string) => {
    switch (method) {
      case "GET": return "bg-blue-500/10 text-blue-400 border-blue-500/20"
      case "POST": return "bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
      case "PUT": return "bg-amber-500/10 text-amber-400 border-amber-500/20"
      case "PATCH": return "bg-purple-500/10 text-purple-400 border-purple-500/20"
      case "DELETE": return "bg-rose-500/10 text-rose-400 border-rose-500/20"
      default: return "bg-sidebar-accent text-muted-foreground border-sidebar-border"
    }
  }

  const totalProtocols = useMemo(() => API_GROUPS.reduce((sum, g) => sum + g.endpoints.length, 0), [])

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-end gap-6">
        <div>
          <h1 className="text-4xl font-extrabold tracking-tight bg-[var(--vibrant-gradient)] bg-clip-text text-transparent italic">
            {t('apiExplorer.title')}
          </h1>
          <p className="text-muted-foreground mt-2 text-lg">
            {t('apiExplorer.description')}
          </p>
        </div>
        <div className="relative group w-full md:w-96">
          <div className="absolute inset-y-0 left-4 flex items-center pointer-events-none text-muted-foreground/50 group-focus-within:text-sidebar-primary transition-colors">
            <Search className="w-4 h-4" />
          </div>
          <input
            type="text"
            placeholder={t('apiExplorer.searchPlaceholder')}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-11 pr-4 py-3 bg-sidebar/40 backdrop-blur-md border border-sidebar-border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-sidebar-primary/50 transition-all font-medium"
          />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6">
        {API_GROUPS.map((group) => {
          const filteredEndpoints = group.endpoints.filter(e => {
            const desc = t(`apiExplorer.endpoints.${e.descriptionKey}`)
            return e.path.toLowerCase().includes(searchQuery.toLowerCase()) ||
              desc.toLowerCase().includes(searchQuery.toLowerCase())
          })

          if (searchQuery && filteredEndpoints.length === 0) return null

          return (
            <div key={group.nameKey} className="group/group overflow-hidden rounded-2xl border border-sidebar-border/40 bg-sidebar/10 backdrop-blur-sm transition-all hover:border-sidebar-border/80 shadow-sm hover:shadow-2xl hover:shadow-sidebar-primary/5">
              <button
                onClick={() => toggleGroup(group.nameKey)}
                className="w-full flex items-center justify-between p-5 bg-sidebar-accent/5 hover:bg-sidebar-accent/20 transition-all text-left"
              >
                <div className="flex items-center gap-4">
                  <div className={cn(
                    "p-2 rounded-xl transition-all duration-300",
                    expandedGroups.has(group.nameKey) ? "bg-sidebar-primary/20 text-sidebar-primary shadow-[0_0_15px_rgba(var(--sidebar-primary),0.2)]" : "bg-sidebar-accent/50 text-muted-foreground"
                  )}>
                    <Terminal className="w-5 h-5" />
                  </div>
                  <div>
                    <h2 className="text-lg font-black tracking-tight text-foreground">{t(`apiExplorer.groups.${group.nameKey}`)}</h2>
                    <p className="text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest mt-0.5">
                      {t('apiExplorer.activeNodes', { count: filteredEndpoints.length })}
                    </p>
                  </div>
                </div>
                {expandedGroups.has(group.nameKey) ? (
                  <ChevronDown className="w-5 h-5 text-muted-foreground/50 group-hover:text-foreground transition-colors" />
                ) : (
                  <ChevronRight className="w-5 h-5 text-muted-foreground/50 group-hover:text-foreground transition-colors" />
                )}
              </button>

              <div className={cn(
                "transition-all duration-500 ease-in-out",
                expandedGroups.has(group.nameKey) ? "max-h-[2000px] opacity-100" : "max-h-0 opacity-0 overflow-hidden"
              )}>
                <div className="p-4 space-y-3 bg-gradient-to-b from-sidebar-accent/5 to-transparent">
                  {filteredEndpoints.map((endpoint, index) => (
                    <div
                      key={index}
                      className="group/endpoint relative p-4 bg-sidebar/40 backdrop-blur-md rounded-xl border border-sidebar-border shadow-sm hover:border-sidebar-primary/50 transition-all duration-300 hover:translate-x-1"
                    >
                      <div className="flex flex-col md:flex-row md:items-center gap-4">
                        <Badge variant="outline" className={cn(
                          "w-20 justify-center h-8 font-black uppercase text-[10px] tracking-tighter transition-all group-hover/endpoint:scale-105",
                          getMethodStyles(endpoint.method)
                        )}>
                          {endpoint.method}
                        </Badge>
                        <div className="flex-1 min-w-0">
                          <div className="flex flex-wrap items-center gap-2 mb-1">
                            <code className="text-sm font-black font-mono text-sidebar-primary break-all selection:bg-sidebar-primary/20 selection:text-white">
                              {endpoint.path}
                            </code>
                            {endpoint.requiresAuth && (
                              <Badge variant="warning" className="h-5 px-1.5 font-black uppercase text-[8px] bg-amber-500/10 text-amber-400 border-amber-500/20">
                                <Lock className="w-2.5 h-2.5 mr-1" /> {t('apiExplorer.authGate')}
                              </Badge>
                            )}
                          </div>
                          <p className="text-muted-foreground text-sm leading-relaxed max-w-2xl font-medium italic opacity-80">
                            {t(`apiExplorer.endpoints.${endpoint.descriptionKey}`)}
                          </p>
                        </div>
                        <div className="flex items-center gap-2 self-end md:self-center">
                          <Button
                            variant="secondary"
                            size="sm"
                            className="h-8 px-3 rounded-lg font-bold text-[10px] border border-sidebar-border bg-sidebar-accent/20 hover:bg-sidebar-accent/50"
                            onClick={() => handleCopy(endpoint.path)}
                          >
                            {copiedPath === endpoint.path ? (
                              <><Check className="w-3.5 h-3.5 mr-2 text-emerald-400" /> {t('apiExplorer.copied')}</>
                            ) : (
                              <><Copy className="w-3.5 h-3.5 mr-2" /> {t('apiExplorer.replicate')}</>
                            )}
                          </Button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )
        })}
      </div>

      <div className="p-8 bg-sidebar/30 backdrop-blur-md border border-sidebar-border rounded-3xl relative overflow-hidden group">
        <div className="absolute top-0 right-0 p-4 opacity-5 group-hover:opacity-10 transition-opacity">
          <Globe className="w-32 h-32" />
        </div>
        <h3 className="text-lg font-black tracking-tight text-foreground flex items-center gap-2 mb-6">
          <Terminal className="w-5 h-5 text-sidebar-primary" />
          {t('apiExplorer.networkBriefing')}
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
          <div className="space-y-1">
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">{t('apiExplorer.targetHost')}</span>
            <p className="font-mono text-sm text-sidebar-primary font-bold">https://localhost:7110</p>
          </div>
          <div className="space-y-1">
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">{t('apiExplorer.authProtocol')}</span>
            <p className="font-bold text-sm text-foreground">{t('apiExplorer.jwtBearerSequence')}</p>
          </div>
          <div className="space-y-1">
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">{t('apiExplorer.dataFormat')}</span>
            <p className="font-bold text-sm text-foreground">{t('apiExplorer.standardizedJson')}</p>
          </div>
          <div className="space-y-1">
            <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground/60">{t('apiExplorer.nexusDensity')}</span>
            <p className="font-bold text-sm text-foreground">
              {t('apiExplorer.protocols', { count: totalProtocols })}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
