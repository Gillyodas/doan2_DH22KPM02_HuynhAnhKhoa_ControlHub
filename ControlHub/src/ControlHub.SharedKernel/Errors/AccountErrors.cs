using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ControlHub.SharedKernel.Errors
{
    public static class AccountErrors
    {
        public static readonly Error InvalidEmail =
        new("Account.InvalidEmail", "Email format is invalid.");

        public static readonly Error EmailAlreadyExists =
            new("Account.EmailAlreadyExists", "Email is already registered.");

        public static readonly Error PasswordTooWeak =
            new("Account.PasswordTooWeak", "Password does not meet security requirements.");

        public static readonly Error InvalidCredentials =
            new("Account.InvalidCredentials", "Email or password is incorrect.");

        public static readonly Error LockedOut =
            new("Account.LockedOut", "Account is temporarily locked due to multiple failed login attempts.");
    }
}
