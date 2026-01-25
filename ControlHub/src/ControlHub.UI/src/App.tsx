import { AuthProvider } from "@/auth/context"
import { RequireAuth } from "@/auth/require-auth"
import "sonner/dist/styles.css"
import { Toaster } from "@/components/ui/sonner"
import { MainLayout } from "@/components/layout/main-layout"
import { DashboardPage } from "@/pages/dashboard-page"
import ApiExplorerPage from "@/pages/api-explorer-page"
import { ForgotPasswordPage } from "@/pages/forgot-password-page"
import { LoginPage } from "@/pages/login-page"
import { ResetPasswordPage } from "@/pages/reset-password-page"
import { RolesPage } from "@/pages/roles-page"
import { SettingsPage } from "@/pages/settings-page"
import { UsersPage } from "@/pages/users-page"
import IdentifiersPage from "@/pages/identifiers-page"
import { AdminAccountsPage } from "@/pages/admin-accounts-page"
import AiAuditPage from "@/pages/AiAuditPage"
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"

export default function App() {
  return (
    <BrowserRouter basename="/control-hub">
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<MainLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="admin-accounts" element={<AdminAccountsPage />} />
              <Route path="roles" element={<RolesPage />} />
              <Route path="identifiers" element={<IdentifiersPage />} />
              <Route path="apis" element={<ApiExplorerPage />} />
              <Route path="ai-audit" element={<AiAuditPage />} />
              <Route path="settings" element={<SettingsPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Route>
        </Routes>
        <Toaster />
      </AuthProvider>
    </BrowserRouter>
  )
}
