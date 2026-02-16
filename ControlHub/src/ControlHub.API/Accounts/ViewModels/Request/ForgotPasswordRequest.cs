using ControlHub.Domain.Identity.Enums;

namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class ForgotPasswordRequest
    {
        public string Value { get; set; } = null!;
        public IdentifierType Type { get; set; }
    }
}
