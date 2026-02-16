using ControlHub.Application.Accounts.Commands.RegisterAdmin;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class RegisterAdminCommandHandlerTests
    {
        private readonly Mock<IAccountValidator> _accountValidatorMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        // S?a l?i Mock Logger cho dúng lo?i (Admin, không ph?i User)
        private readonly Mock<ILogger<RegisterAdminCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountFactory> _accountFactoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        private readonly RegisterAdminCommandHandler _handler;
        private readonly string _validRoleId = Guid.NewGuid().ToString();

        public RegisterAdminCommandHandlerTests()
        {
            // Setup m?c d?nh: Config dúng
            _configMock.Setup(x => x["RoleSettings:AdminRoleId"]).Returns(_validRoleId);

            _handler = new RegisterAdminCommandHandler(
                _accountValidatorMock.Object,
                _accountRepositoryMock.Object,
                _loggerMock.Object,
                _accountFactoryMock.Object,
                _configMock.Object,
                _uowMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: BUG HUNTING - CONFIGURATION & ROBUSTNESS
        // M?c tiêu: B?t l?i code dang b? Crash khi c?u hình sai
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminRoleIdConfigIsMissing()
        {
            // ?? BUG HUNT: Code hi?n t?i dùng Guid.Parse tr?c ti?p.
            // N?u ch?y test này, nó s? Fail do ArgumentNullException.
            // Ði?u này CH?NG MINH code thi?u logic x? lý l?i c?u hình.

            // Arrange
            _configMock.Setup(x => x["RoleSettings:AdminRoleId"]).Returns((string?)null);
            var command = new RegisterAdminCommand("admin@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Mong d?i: Code ph?i b?t l?i và tr? v? Failure
            Assert.True(result.IsFailure, "L?I: Handler b? Crash (Exception) thay vì tr? v? Result.Failure khi thi?u Config.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);

            // Verify: Không du?c g?i DB khi l?i h? th?ng
            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminRoleIdConfigIsInvalidFormat()
        {
            // ?? BUG HUNT: Code hi?n t?i s? Crash (FormatException).

            // Arrange
            _configMock.Setup(x => x["RoleSettings:AdminRoleId"]).Returns("invalid-guid-string");
            var command = new RegisterAdminCommand("admin@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I: Handler b? Crash (FormatException) thay vì tr? v? Result.Failure khi Config sai d?nh d?ng.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);
        }

        // =================================================================================
        // NHÓM 2: LOGIC NGHI?P V? (BUSINESS LOGIC)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierAlreadyExists()
        {
            // Arrange
            var command = new RegisterAdminCommand("exist@test.com", IdentifierType.Email, "Pass123!");
            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.EmailAlreadyExists, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenFactoryFails()
        {
            // Arrange
            var command = new RegisterAdminCommand("new@test.com", IdentifierType.Email, "Pass123!");

            _accountValidatorMock.Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var domainError = AccountErrors.InvalidEmail;
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(), command.Value, command.Type, command.Password, It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
                .ReturnsAsync(Result<Maybe<Account>>.Failure(domainError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(domainError, result.Error);
        }

        // =================================================================================
        // NHÓM 3: HAPPY PATH
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_AndPersistData_WhenAllValid()
        {
            // Arrange
            var command = new RegisterAdminCommand("admin@test.com", IdentifierType.Email, "Pass123!");

            _accountValidatorMock.Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var dummyPassword = Password.From(new byte[32], new byte[16]);

            // Mock Factory: Ki?m tra xem có truy?n dúng RoleId t? Config không
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(),
                    command.Value,
                    command.Type,
                    command.Password,
                    Guid.Parse(_validRoleId), // Verify logic l?y config
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync((Guid id, string v, IdentifierType t, string p, Guid r, string? u, Guid? cid) =>
                {
                    var account = Account.Create(id, dummyPassword, r);
                    return Result<Maybe<Account>>.Success(Maybe<Account>.From(account));
                });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            // Verify Side Effects
            _accountRepositoryMock.Verify(r => r.AddAsync(It.Is<Account>(a => a.Id == result.Value), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
