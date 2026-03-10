namespace ControlHub.API.AccessControl.ViewModels.Responses
{
    public class CreateRolesResponse
    {
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public IEnumerable<string>? FailedRoles { get; set; }
    }
}
