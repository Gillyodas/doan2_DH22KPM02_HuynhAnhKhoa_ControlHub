# ControlHub Frontend API Documentation

This document outlines all the frontend components and pages created to interact with the ControlHub backend APIs.

## API Service Layer

All API services are located in `src/services/api/` and include:

### Authentication APIs (`auth.ts`)
- `signIn(req)` - POST `/api/Auth/signin`
- `registerUser(req)` - POST `/api/Auth/register`
- `registerAdmin(req, accessToken)` - POST `/api/Auth/register-admin`
- `registerSuperAdmin(req)` - POST `/api/Auth/register-superadmin`
- `refreshAccessToken(req)` - POST `/api/Auth/refresh`
- `signOut(req, accessToken)` - POST `/api/Auth/signout`

### Account Management APIs (`account.ts`)
- `changePassword(userId, req, accessToken)` - POST `/api/Account/change-password/{id}`
- `forgotPassword(req)` - POST `/api/Account/forgot-password`
- `resetPassword(req)` - POST `/api/Account/reset-password`

### Permissions APIs (`permissions.ts`)
- `createPermissions(req, accessToken)` - POST `/api/Permission/permissions`
- `getPermissions(params, accessToken)` - GET `/api/Permission`

### Roles APIs (`roles.ts`)
- `createRoles(req, accessToken)` - POST `/api/Role/roles`
- `addPermissionsForRole(req, accessToken)` - POST `/api/Role/update`
- `getRoles(params, accessToken)` - GET `/api/Role`

### Users APIs (`users.ts`)
- `updateUsername(userId, req, accessToken)` - PATCH `/api/User/username/{id}`

## Components

### Account Management
**`src/components/account/change-password-dialog.tsx`**
- Dialog component for changing user password
- Validates new password confirmation
- Requires current password for security

### Permissions Management
**`src/components/permissions/create-permissions-dialog.tsx`**
- Create multiple permissions at once
- Dynamic form with add/remove capability

**`src/components/permissions/permissions-table.tsx`**
- Display permissions in a table format
- Shows name, description, created/updated dates

### Roles Management
**`src/components/roles/create-roles-dialog.tsx`**
- Create multiple roles at once
- Handles partial success scenarios

**`src/components/roles/roles-table.tsx`**
- Display roles with their assigned permissions
- Action button to manage permissions

**`src/components/roles/assign-permissions-dialog.tsx`**
- Assign/unassign permissions to a role
- Checkbox interface for permission selection
- Fetches all available permissions

### User Management
**`src/components/users/update-username-dialog.tsx`**
- Update user's username
- Simple form with validation

## Pages

### Permissions Page (`src/pages/permissions-page.tsx`)
**Route:** `/permissions`

Features:
- List all permissions with pagination
- Search permissions by name
- Create new permissions
- View permission details

### Roles Management Page (`src/pages/roles-management-page.tsx`)
**Route:** `/roles`

Features:
- List all roles with pagination
- Search roles by name
- Create new roles
- Manage permissions for each role
- View assigned permissions

### Users Page (`src/pages/users-page.tsx`)
**Route:** `/users`

Features:
- View user profile information
- Update username
- Display account ID

### Settings Page (`src/pages/settings-page.tsx`)
**Route:** `/settings`

Features:
- View and update username
- Change password
- Sign out

## Navigation

The sidebar has been updated to include:
- Dashboard
- User Management (`/users`)
- Roles (`/roles`)
- Permissions (`/permissions`) - **NEW**
- Identify
- API
- Settings

## TypeScript Types

All API types are defined in `src/services/api/types.ts`:

### Core Types
- `AuthData` - Authentication response data
- `PagedResult<T>` - Generic pagination wrapper
- `Permission` - Permission entity
- `Role` - Role entity with optional permissions

### Request/Response Types
- `SignInRequest`, `RegisterRequest`, `RefreshTokenRequest`
- `ChangePasswordRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`
- `CreatePermissionsRequest`, `CreateRolesRequest`
- `AddPermissionsForRoleRequest`, `AddPermissionsForRoleResponse`
- `UpdateUsernameRequest`, `UpdateUsernameResponse`

## Usage Examples

### Creating Permissions
```typescript
import { permissionsApi } from '@/services/api'

await permissionsApi.createPermissions(
  { permissions: ['user.view', 'user.edit'] },
  accessToken
)
```

### Assigning Permissions to Role
```typescript
import { rolesApi } from '@/services/api'

await rolesApi.addPermissionsForRole(
  {
    roleId: 'role-id',
    permissionIds: ['perm-id-1', 'perm-id-2']
  },
  accessToken
)
```

### Changing Password
```typescript
import { accountApi } from '@/services/api'

await accountApi.changePassword(
  userId,
  { curPass: 'old', newPass: 'new' },
  accessToken
)
```

## Features Implemented

✅ **Authentication**
- Sign in
- Register (User, Admin, SuperAdmin)
- Sign out
- Token refresh
- Password reset flow

✅ **Account Management**
- Change password (authenticated users only)
- Forgot password (public)
- Reset password (public)

✅ **Permissions Management**
- Create multiple permissions
- List permissions with pagination
- Search permissions

✅ **Roles Management**
- Create multiple roles
- List roles with pagination
- Search roles
- Assign/remove permissions to roles
- View role permissions

✅ **User Management**
- Update username
- View profile information

## Security Features

- **Authorization Required**: All protected routes require authentication
- **Token-based Auth**: Access tokens sent in Authorization header
- **Resource-based Authorization**: Change password restricted to same user
- **Error Handling**: Comprehensive error messages from API
- **Type Safety**: Full TypeScript support for all API calls

## React Hooks

### `useUserManagement`
Located in `src/hooks/use-user-management.ts`.
Encapsulates all user CRUD logic, pagination, and search state.

**Returns:**
- `users`: List of users
- `pagination`: Pagination metadata
- `isLoading`: Loading state
- `searchTerm`: Current search term (auto-debounced)
- `actions`: `updateUser`, `deleteUser`, `assignRole`, `removeRole`

### `useProfile`
Located in `src/hooks/use-profile.ts`.
Handles fetching and updating the current user's profile.

**Returns:**
- `profile`: Current user profile data
- `updateProfile(data)`: Function to update profile
- `isUpdating`: Loading state for updates

## Next Steps

Potential enhancements:
1. Add user listing and management (create, update, delete users)
2. Implement role assignment to users
3. Add permission/role deletion functionality
4. Implement audit logs viewing
5. Add bulk operations for permissions and roles
6. Implement real-time token refresh mechanism
