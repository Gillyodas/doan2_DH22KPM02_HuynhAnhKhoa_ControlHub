import type { Permission, Role } from "./types"

export const initialPermissions: Permission[] = [
  { id: "11111111-1111-1111-1111-111111111111", code: "dashboard.view", description: "Can view dashboard analytics" },
  { id: "22222222-2222-2222-2222-222222222222", code: "user.manage", description: "Can create, edit, delete users" },
  { id: "33333333-3333-3333-3333-333333333333", code: "role.manage", description: "Can create, edit, delete roles" },
  { id: "44444444-4444-4444-4444-444444444444", code: "report.view", description: "Can view system reports" },
  { id: "55555555-5555-5555-5555-555555555555", code: "settings.edit", description: "Can modify system settings" },
  { id: "66666666-6666-6666-6666-666666666666", code: "data.delete", description: "Can delete critical data" },
]

export const initialRoles: Role[] = [
  {
    id: "9BA459E9-2A98-43C4-8530-392A63C66F1B",
    name: "super_admin",
    permissionIds: initialPermissions.map((p) => p.id),
    description: "System Super Admin",
  },
  {
    id: "0CD24FAC-ABD7-4AD9-A7E4-248058B8D404",
    name: "admin",
    permissionIds: [
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
      "44444444-4444-4444-4444-444444444444",
      "55555555-5555-5555-5555-555555555555",
    ],
    description: "System Admin",
  },
  {
    id: "8CF94B41-5AD8-4893-82B2-B193C91717AF",
    name: "user",
    permissionIds: ["11111111-1111-1111-1111-111111111111"],
    description: "Standard User",
  },
]
