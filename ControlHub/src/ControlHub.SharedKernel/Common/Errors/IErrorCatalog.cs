namespace ControlHub.SharedKernel.Common.Errors
{
    public interface IErrorCatalog
    {
        string? GetCodeByMessage(string message);
    }
}
