using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Accounts
{
    public class AccountErrorsCatalog : IErrorCatalog
    {
        private static readonly Dictionary<string, string> _messageToCode;

        static AccountErrorsCatalog()
        {
            _messageToCode = typeof(AccountErrors).GetFields()
                .Select(f => f.GetValue(null) as Error)
                .Where(e => e != null)
                .ToDictionary(e => e.Message, e => e.Code);
        }

        public string? GetCodeByMessage(string message)
        {
            return _messageToCode.TryGetValue(message, out var code)
                ? code
                : "null";
        }
    }
}