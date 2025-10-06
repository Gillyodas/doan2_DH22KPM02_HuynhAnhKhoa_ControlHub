using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Accounts
{
    public static class AccountLogs
    {
        // CreateAccount
        public static readonly LogCode CreateAccount_Started =
            new("Account.Create.Started", "Starting account creation");

        public static readonly LogCode CreateAccount_InvalidIdentifier =
            new("Account.Create.InvalidIdentifier", "Invalid identifier format");

        public static readonly LogCode CreateAccount_IdentifierExists =
            new("Account.Create.IdentifierExists", "Identifier already exists");

        public static readonly LogCode CreateAccount_FactoryFailed =
            new("Account.Create.FactoryFailed", "Account factory failed");

        public static readonly LogCode CreateAccount_Success =
            new("Account.Create.Success", "Account created successfully");

        // ChangePassword
        public static readonly LogCode ChangePassword_Started =
            new("Auth.ChangePassword.Started", "Starting change password flow");

        public static readonly LogCode ChangePassword_AccountNotFound =
            new("Auth.ChangePassword.AccountNotFound", "Account not found");

        public static readonly LogCode ChangePassword_InvalidPassword =
            new("Auth.ChangePassword.InvalidPassword", "Invalid current password");

        public static readonly LogCode ChangePassword_Success =
            new("Auth.ChangePassword.Success", "Password changed successfully");

        public static readonly LogCode ChangePassword_UpdateFailed =
            new("Account.ChangePassword.UpdateFailed", "Failed to update password");

        // ForgotPassword
        public static readonly LogCode ForgotPassword_Started =
            new("Account.ForgotPassword.Started", "Starting forgot password flow");

        public static readonly LogCode ForgotPassword_InvalidIdentifier =
            new("Account.ForgotPassword.InvalidIdentifier", "Invalid identifier format");

        public static readonly LogCode ForgotPassword_IdentifierNotFound =
            new("Account.ForgotPassword.IdentifierNotFound", "Identifier not found");

        public static readonly LogCode ForgotPassword_TokenGenerated =
            new("Account.ForgotPassword.TokenGenerated", "Password reset token generated");

        public static readonly LogCode ForgotPassword_NotificationSent =
            new("Account.ForgotPassword.NotificationSent", "Password reset notification sent successfully");

        // SignIn
        public static readonly LogCode SignIn_Started =
            new("Account.SignIn.Started", "Starting sign-in process");

        public static readonly LogCode SignIn_InvalidIdentifier =
            new("Account.SignIn.InvalidIdentifier", "Invalid identifier format");

        public static readonly LogCode SignIn_AccountNotFound =
            new("Account.SignIn.AccountNotFound", "Account not found");

        public static readonly LogCode SignIn_InvalidPassword =
            new("Account.SignIn.InvalidPassword", "Invalid password");

        public static readonly LogCode SignIn_Success =
            new("Account.SignIn.Success", "User signed in successfully");

        // ResetPassword
        public static readonly LogCode ResetPassword_Started =
        new("Account.ResetPassword.Started", "Starting reset password flow");

        public static readonly LogCode ResetPassword_TokenNotFound =
            new("Account.ResetPassword.TokenNotFound", "Reset token not found");

        public static readonly LogCode ResetPassword_TokenInvalid =
            new("Account.ResetPassword.TokenInvalid", "Reset token is invalid");

        public static readonly LogCode ResetPassword_TokenMismatch =
            new("Account.ResetPassword.TokenMismatch", "Reset token does not belong to account");

        public static readonly LogCode ResetPassword_AccountNotFound =
            new("Account.ResetPassword.AccountNotFound", "Account not found");

        public static readonly LogCode ResetPassword_AccountDisabled =
            new("Account.ResetPassword.AccountDisabled", "Account is disabled");

        public static readonly LogCode ResetPassword_PasswordHashFailed =
            new("Account.ResetPassword.PasswordHashFailed", "Failed to hash the new password");

        public static readonly LogCode ResetPassword_Success =
            new("Account.ResetPassword.Success", "Password reset successfully");
    }
}