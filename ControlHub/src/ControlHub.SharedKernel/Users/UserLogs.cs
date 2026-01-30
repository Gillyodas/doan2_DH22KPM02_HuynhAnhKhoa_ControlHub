using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Users
{
    public static class UserLogs
    {
        public static readonly LogCode UpdateUsername_Started =
            new("User.UpdateUsername.Started", "Starting username update process");

        public static readonly LogCode UpdateUsername_NotFound =
            new("User.UpdateUsername.NotFound", "User not found for username update");

        public static readonly LogCode UpdateUsername_Failed =
            new("User.UpdateUsername.Failed", "Failed to update username");

        public static readonly LogCode UpdateUsername_Success =
            new("User.UpdateUsername.Success", "Username updated successfully");

        // Update User
        public static readonly LogCode UpdateUser_Started =
            new("User.UpdateUser.Started", "Starting update user profile process");
        public static readonly LogCode UpdateUser_NotFound =
            new("User.UpdateUser.NotFound", "User not found for update");
        public static readonly LogCode UpdateUser_AccountNotFound =
            new("User.UpdateUser.AccountNotFound", "Account not found for user update");
        public static readonly LogCode UpdateUser_IdentifierConflict =
            new("User.UpdateUser.IdentifierConflict", "Identifier conflict during user update");
        public static readonly LogCode UpdateUser_Success =
            new("User.UpdateUser.Success", "User updated successfully");

        // Delete User
        public static readonly LogCode DeleteUser_Started =
            new("User.DeleteUser.Started", "Starting delete user process");
        public static readonly LogCode DeleteUser_NotFound =
            new("User.DeleteUser.NotFound", "User not found for deletion");
        public static readonly LogCode DeleteUser_Success =
            new("User.DeleteUser.Success", "User deleted successfully");

        // Get User By Id
        public static readonly LogCode GetUserById_Started =
            new("User.GetUserById.Started", "Fetching user by ID");
        public static readonly LogCode GetUserById_NotFound =
            new("User.GetUserById.NotFound", "User not found by ID");
        public static readonly LogCode GetUserById_Success =
            new("User.GetUserById.Success", "User found successfully");

        // Get Users
        public static readonly LogCode GetUsers_Started =
            new("User.GetUsers.Started", "Fetching paginated users");
        public static readonly LogCode GetUsers_Success =
            new("User.GetUsers.Success", "Users retrieved successfully");

        // Profile Management
        public static readonly LogCode GetMyProfile_Started =
            new("User.GetMyProfile.Started", "Fetching current user profile");

        public static readonly LogCode GetMyProfile_NotFound =
            new("User.GetMyProfile.NotFound", "Current user profile not found");

        public static readonly LogCode GetMyProfile_Success =
            new("User.GetMyProfile.Success", "Current user profile retrieved successfully");

        public static readonly LogCode UpdateMyProfile_Started =
            new("User.UpdateMyProfile.Started", "Starting update own profile process");

        public static readonly LogCode UpdateMyProfile_Success =
            new("User.UpdateMyProfile.Success", "Own profile updated successfully");
    }
}
