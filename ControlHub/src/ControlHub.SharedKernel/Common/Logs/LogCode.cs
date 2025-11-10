namespace ControlHub.SharedKernel.Common.Logs
{
    public sealed record LogCode(string Code, string Message)
    {
        public static readonly LogCode None = new(string.Empty, string.Empty);
    }
}
