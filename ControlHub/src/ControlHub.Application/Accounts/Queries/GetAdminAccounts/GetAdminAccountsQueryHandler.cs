using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControlHub.Application.Accounts.Queries.GetAdminAccounts
{
    public class GetAdminAccountsQueryHandler : IRequestHandler<GetAdminAccountsQuery, Result<List<AccountDto>>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly RoleSettings _roleSettings;
        private readonly ILogger<GetAdminAccountsQueryHandler> _logger;

        public GetAdminAccountsQueryHandler(
            IAccountRepository accountRepository,
            IOptions<RoleSettings> roleSettings,
            ILogger<GetAdminAccountsQueryHandler> logger)
        {
            _accountRepository = accountRepository;
            _roleSettings = roleSettings.Value;
            _logger = logger;
        }

        public async Task<Result<List<AccountDto>>> Handle(GetAdminAccountsQuery request, CancellationToken cancellationToken)
        {
            var adminRoleId = _roleSettings.AdminRoleId;
            if (adminRoleId == Guid.Empty)
            {
                _logger.LogWarning("AdminRoleId is empty in configuration. Using default.");
                adminRoleId = ControlHub.SharedKernel.Constants.ControlHubDefaults.Roles.AdminId;
            }

            _logger.LogInformation("Fetching admin accounts with RoleId: {RoleId}", adminRoleId);

            var accounts = await _accountRepository.GetByRoleIdAsync(adminRoleId, cancellationToken);
            
            var dtos = accounts.Select(a => new AccountDto(
                a.Id,
                a.Identifiers.FirstOrDefault(i => i.Type == Domain.Accounts.Enums.IdentifierType.Username)?.Value ?? "N/A",
                a.Role?.Name ?? "Admin",
                a.IsActive
            )).ToList();

            return Result<List<AccountDto>>.Success(dtos);
        }
    }
}
