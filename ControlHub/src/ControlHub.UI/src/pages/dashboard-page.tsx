import { ShieldCheck, Fingerprint, Code2, Users } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useTranslation } from "react-i18next"
import { useRealtimeStats } from "@/hooks/use-realtime-stats"
import { LoginAttemptsChart } from "@/components/dashboard/login-attempts-chart"
import { cn } from "@/lib/utils" // Ensure this exists, or copy from original file. Original had local function.

export function DashboardPage() {
  const { t } = useTranslation()
  const { activeUsers, totalRoles, totalIdentifiers, loginAttempts, connectionStatus } = useRealtimeStats()

  const stats = [
    { title: t('dashboard.totalRoles'), value: String(totalRoles), icon: ShieldCheck, color: "text-blue-500", bg: "bg-blue-500/10", live: true },
    { title: t('dashboard.identifiers'), value: String(totalIdentifiers), icon: Fingerprint, color: "text-purple-500", bg: "bg-purple-500/10", live: true },
    { title: t('dashboard.apiEndpoints'), value: "45", icon: Code2, color: "text-emerald-500", bg: "bg-emerald-500/10", live: false },
    { title: t('dashboard.activeUsers'), value: String(activeUsers), icon: Users, color: "text-amber-500", bg: "bg-amber-500/10", live: true },
  ]

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-4xl font-extrabold tracking-tight bg-gradient-to-r from-white to-zinc-500 bg-clip-text text-transparent">
          {t('dashboard.overviewTitle')}
        </h1>
        <p className="text-muted-foreground mt-2 text-lg">
          {t('dashboard.welcomeMessage')}
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.title} className="relative overflow-hidden border-sidebar-border bg-sidebar/50 backdrop-blur-sm hover:border-sidebar-primary/50 transition-all duration-300 group">
            <div className={cn("absolute top-0 right-0 w-24 h-24 -mr-8 -mt-8 rounded-full blur-3xl opacity-20", stat.bg)} />
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
                {stat.title}
              </CardTitle>
              <div className={cn("p-2 rounded-lg transition-transform group-hover:scale-110", stat.bg, stat.color)}>
                <stat.icon className="h-5 w-5" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground mt-1 font-medium">
                {stat.live ? (
                  <span className="flex items-center gap-1 text-emerald-500">
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                    </span>
                    Live
                  </span>
                ) : (
                  t('dashboard.fromLastMonth')
                )}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-7">
        <div className="col-span-4 rounded-xl border border-sidebar-border bg-sidebar/50 overflow-hidden">
          <LoginAttemptsChart attempts={loginAttempts} connectionStatus={connectionStatus} />
        </div>

        <Card className="col-span-3 border-sidebar-border bg-sidebar/50">
          <CardHeader>
            <CardTitle>{t('dashboard.systemHealth')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                { name: t('dashboard.apiService'), status: t('dashboard.healthy'), color: "bg-emerald-500" },
                { name: t('dashboard.database'), status: t('dashboard.connected'), color: "bg-emerald-500" },
                { name: t('dashboard.authProvider'), status: t('dashboard.activeStatus'), color: "bg-blue-500" },
                { name: "Real-time Hub", status: connectionStatus === 'connected' ? "Active" : "Connecting...", color: connectionStatus === 'connected' ? "bg-emerald-500" : "bg-yellow-500" },
              ].map((item) => (
                <div key={item.name} className="flex items-center justify-between">
                  <span className="text-sm font-medium">{item.name}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-muted-foreground">{item.status}</span>
                    <div className={cn("h-2 w-2 rounded-full", item.color)} />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}


