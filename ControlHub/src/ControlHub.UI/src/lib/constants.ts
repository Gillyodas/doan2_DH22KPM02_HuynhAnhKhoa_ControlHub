export const APP_CONFIG = {
  name: 'ControlHub',
  version: '1.0.0',
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || '',
  baseUrl: import.meta.env.VITE_BASE_URL || '/control-hub',
} as const

export const AUTH_CONFIG = {
  tokenRefreshInterval: 14 * 60 * 1000, // 14 minutes
  tokenExpiryBuffer: 60 * 1000, // 1 minute buffer before expiry
} as const

export const PAGINATION_CONFIG = {
  defaultPageSize: 10,
  pageSizeOptions: [5, 10, 20, 50, 100],
  maxPageSize: 100,
} as const

export const DEBOUNCE_DELAYS = {
  search: 500,
  input: 300,
  resize: 250,
} as const

export const IDENTIFIER_TYPES = {
  USERNAME: 0,
  EMAIL: 1,
  PHONE: 2,
  AUTO: 99,
} as const

export const ROUTES = {
  home: '/',
  login: '/login',
  forgotPassword: '/forgot-password',
  resetPassword: '/reset-password',
  users: '/users',
  roles: '/roles',
  permissions: '/permissions',
  identify: '/identify',
  apis: '/apis',
  settings: '/settings',
} as const

export const API_ENDPOINTS = {
  auth: {
    signIn: '/api/Auth/auth/signin',
    register: '/api/Auth/users/register',
    registerAdmin: '/api/Auth/admins/register',
    registerSuperAdmin: '/api/Auth/superadmins/register',
    refresh: '/api/Auth/auth/refresh',
    signOut: '/api/Auth/auth/signout',
  },
  account: {
    changePassword: (id: string) => `/api/Account/users/${id}/password`,
    forgotPassword: '/api/Account/auth/forgot-password',
    resetPassword: '/api/Account/auth/reset-password',
  },
  permissions: {
    create: '/api/Permission/permissions',
    list: '/api/Permission',
  },
  roles: {
    create: '/api/Role/roles',
    update: '/api/Role/roles/{roleId}/permissions',
    list: '/api/Role',
  },
  users: {
    updateUsername: (id: string) => `/api/User/users/${id}/username`,
  },
} as const

export const TOAST_MESSAGES = {
  success: {
    signIn: 'Successfully signed in',
    signOut: 'Successfully signed out',
    created: (entity: string) => `${entity} created successfully`,
    updated: (entity: string) => `${entity} updated successfully`,
    deleted: (entity: string) => `${entity} deleted successfully`,
  },
  error: {
    signIn: 'Failed to sign in',
    signOut: 'Failed to sign out',
    network: 'Network error. Please check your connection.',
    unauthorized: 'Unauthorized. Please sign in again.',
    forbidden: 'You do not have permission to perform this action.',
    notFound: (entity: string) => `${entity} not found`,
    server: 'Server error. Please try again later.',
    validation: 'Please check your input and try again.',
  },
} as const

export const ERROR_CODES = {
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  SERVER_ERROR: 500,
} as const
