namespace ControlHub.SharedKernel.Constants
{
    public static class ControlHubDefaults
    {
        public static class Roles
        {
            public static readonly Guid SuperAdminId = Guid.Parse("9BA459E9-2A98-43C4-8530-392A63C66F1B");
            public static readonly Guid AdminId = Guid.Parse("0CD24FAC-ABD7-4AD9-A7E4-248058B8D404");
            public static readonly Guid UserId = Guid.Parse("8CF94B41-5AD8-4893-82B2-B193C91717AF");

            public const string SuperAdminName = "super_admin";
            public const string AdminName = "admin";
            public const string UserName = "user";
        }
    }
}
