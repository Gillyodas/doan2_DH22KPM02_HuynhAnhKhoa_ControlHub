import { AuthProvider } from "@/auth/context"
import { RequireAuth } from "@/auth/require-auth"
import { MainLayout } from "@/components/layout/main-layout"
import { DashboardPage } from "@/pages/dashboard-page"
import { ApisPage } from "@/pages/apis-page"
import { ForgotPasswordPage } from "@/pages/forgot-password-page"
import { LoginPage } from "@/pages/login-page"
import { ResetPasswordPage } from "@/pages/reset-password-page"
import { RolesPage } from "@/pages/roles-page"
import { SettingsPage } from "@/pages/settings-page"
import { UsersPage } from "@/pages/users-page"
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />

          <Route element={<RequireAuth />}>
            <Route element={<MainLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="roles" element={<RolesPage />} />
              <Route path="apis" element={<ApisPage />} />
              <Route path="settings" element={<SettingsPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
