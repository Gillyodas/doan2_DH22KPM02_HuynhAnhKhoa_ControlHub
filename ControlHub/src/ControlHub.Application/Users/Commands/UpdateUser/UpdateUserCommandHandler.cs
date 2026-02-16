using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Users.DTOs;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;

using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Commands.UpdateUser
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserRepository userRepository,
            IAccountRepository accountRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _accountRepository = accountRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | UserId: {UserId}", UserLogs.UpdateUser_Started, request.Id);

            var user = await _userRepository.GetByIdAsync(request.Id, ct);
            if (user == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId}", UserLogs.UpdateUser_NotFound, request.Id);
                return Result<UserDto>.Failure(UserErrors.NotFound);
            }

            var account = await _accountRepository.GetWithoutUserByIdAsync(user.AccId, ct);
            if (account == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId} | AccId: {AccId}", UserLogs.UpdateUser_AccountNotFound, request.Id, user.AccId);
                return Result<UserDto>.Failure(AccountErrors.AccountNotFound);
            }

            // Update Email
            if (!string.IsNullOrEmpty(request.Email))
            {
                var currentEmailIdentifier = account.Identifiers.FirstOrDefault(i => i.Type == IdentifierType.Email);
                var normalizedNewEmail = request.Email.ToUpperInvariant();

                // If email changed
                if (currentEmailIdentifier == null || currentEmailIdentifier.NormalizedValue != normalizedNewEmail)
                {
                    // Check duplicate
                    var existingAccountWithEmail = await _accountRepository.GetByIdentifierWithoutUserAsync(
                        IdentifierType.Email, normalizedNewEmail, ct);

                    if (existingAccountWithEmail != null && existingAccountWithEmail.Id != account.Id)
                    {
                        _logger.LogWarning("{@LogCode} | Email: {Email}", UserLogs.UpdateUser_IdentifierConflict, request.Email);
                        return Result<UserDto>.Failure(AccountErrors.IdentifierAlreadyExists);
                    }

                    // Remove old if exists
                    if (currentEmailIdentifier != null)
                    {
                        var removeResult = account.RemoveIdentifier(IdentifierType.Email, currentEmailIdentifier.NormalizedValue);
                        if (!removeResult.IsSuccess) return Result<UserDto>.Failure(removeResult.Error);
                    }

                    // Add new
                    var newIdentifier = Identifier.Create(IdentifierType.Email, request.Email, normalizedNewEmail);
                    var addResult = account.AddIdentifier(newIdentifier);
                    if (!addResult.IsSuccess) return Result<UserDto>.Failure(addResult.Error);
                }
            }

            // Update Profile
            user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);

            // Update IsActive
            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value) account.Activate();
                else account.Deactivate();
            }

            await _unitOfWork.CommitAsync(ct);

            // Construct DTO
            var role = await _roleRepository.GetByIdAsync(account.RoleId, ct);
            var email = account.Identifiers.FirstOrDefault(i => i.Type == IdentifierType.Email)?.Value;

            _logger.LogInformation("{@LogCode} | UserId: {UserId}", UserLogs.UpdateUser_Success, request.Id);

            return Result<UserDto>.Success(new UserDto(
                user.Id,
                user.Username ?? "",
                email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                account.IsActive,
                account.RoleId,
                role?.Name
            ));
        }
    }
}
