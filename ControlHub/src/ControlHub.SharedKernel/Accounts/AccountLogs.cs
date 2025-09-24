using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Accounts
{
    public static class AccountLogs
    {
        // CreateAccount
        public static readonly LogCode CreateAccount_Started =
            new("Account.Create.Started", "Starting account creation");

        public static readonly LogCode CreateAccount_InvalidEmail =
            new("Account.Create.InvalidEmail", "Invalid email format");

        public static readonly LogCode CreateAccount_EmailExists =
            new("Account.Create.EmailExists", "Email already exists");

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

        public static readonly LogCode ForgotPassword_InvalidEmail =
            new("Account.ForgotPassword.InvalidEmail", "Invalid email format");

        public static readonly LogCode ForgotPassword_EmailNotFound =
            new("Account.ForgotPassword.EmailNotFound", "Email not found");

        public static readonly LogCode ForgotPassword_TokenGenerated =
            new("Account.ForgotPassword.TokenGenerated", "Password reset token generated");

        public static readonly LogCode ForgotPassword_EmailSent =
            new("Account.ForgotPassword.EmailSent", "Password reset email sent successfully");

        // SignIn
        public static readonly LogCode SignIn_Started =
            new("Account.SignIn.Started", "Starting sign in process");

        public static readonly LogCode SignIn_InvalidEmail =
            new("Account.SignIn.InvalidEmail", "Invalid email format");

        public static readonly LogCode SignIn_AccountNotFound =
            new("Account.SignIn.AccountNotFound", "Account not found");

        public static readonly LogCode SignIn_InvalidPassword =
            new("Account.SignIn.InvalidPassword", "Invalid password");

        public static readonly LogCode SignIn_Success =
            new("Account.SignIn.Success", "User signed in successfully");
    }
}
