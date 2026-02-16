using ControlHub.Domain.Identity.Enums;

namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class RegisterSupperAdminRequest
    {
        public string Value { get; set; } = null!;
        public string Password { get; set; } = null!;
        public IdentifierType Type { get; set; }
        public Guid? IdentifierConfigId { get; set; }
        public string MasterKey { get; set; } = null!;
    }
}
