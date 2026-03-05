using ControlHub.Domain.Identity.Enums;

namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class SignInRequest
    {
        public string Value { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid? IdentifierConfigId { get; set; }
    }
}
