namespace ControlHub.API.AccessControl.ViewModels.Requests
{
    public class SearchRolesRequest
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string[] Conditions { get; set; } = null!;
    }
}
