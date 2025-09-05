using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common;

namespace ControlHub.SharedKernel.Accounts
{
    public static class AccountErrorsCatalog
    {
        private static readonly Dictionary<string, string> _messageToCode;

        static AccountErrorsCatalog()
        {
            _messageToCode = typeof(AccountErrors).GetFields()
                .Select(f => f.GetValue(null) as Error)
                .Where(e => e != null)
                .ToDictionary(e => e.Message, e => e.Code);
        }

        public static string GetCodeByMessage(string message)
        {
            return _messageToCode.TryGetValue(message, out var code)
                ? code
                : "Validation.Unknown";
        }
    }
}