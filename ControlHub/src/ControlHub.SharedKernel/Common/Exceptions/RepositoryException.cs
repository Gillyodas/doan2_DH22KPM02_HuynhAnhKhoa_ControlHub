namespace ControlHub.SharedKernel.Common.Exceptions
{
    public class RepositoryException : Exception
    {
        public RepositoryException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
