using ControlHub.Domain.Identity.Enums;

namespace ControlHub.API.Identity.ViewModels.Request
{
    public class RegisterUserRequest
    {
        public string Value { get; set; } = null!;
        public string Password { get; set; } = null!;
        public IdentifierType Type { get; set; }
        public Guid? IdentifierConfigId { get; set; }
    }
}
