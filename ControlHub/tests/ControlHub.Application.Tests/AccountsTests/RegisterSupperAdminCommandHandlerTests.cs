using ControlHub.Application.Accounts.Commands.RegisterSupperAdmin;
using ControlHub.Application.Accounts.Commands.RegisterUser;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Common.Logs; // Import namespace chứa CommonLogs
using ControlHub.SharedKernel.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class RegisterSupperAdminCommandHandlerTests
    {
        private readonly Mock<IAccountValidator> _accountValidatorMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        // Mock Logger khớp với loại được inject trong Handler (hiện tại là RegisterUserCommandHandler)
        private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountFactory> _accountFactoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        private readonly RegisterSupperAdminCommandHandler _handler;

        private const string ValidMasterKey = "SecretKey123";

        public RegisterSupperAdminCommandHandlerTests()
        {
            // 1. Setup Config mặc định hợp lệ
            _configMock.Setup(x => x["AppPassword:MasterKey"]).Returns(ValidMasterKey);
            _configMock.Setup(x => x["RoleSettings:SuperAdminRoleId"]).Returns(Guid.NewGuid().ToString());

            // 2. Init Handler
            _handler = new RegisterSupperAdminCommandHandler(
                _accountValidatorMock.Object,
                _accountRepositoryMock.Object,
                _loggerMock.Object,
                _accountFactoryMock.Object,
                _configMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenSystemConfigIsMissingMasterKey()
        {
            // Arrange
            _configMock.Setup(x => x["AppPassword:MasterKey"]).Returns((string?)null);

            var command = new RegisterSupperAdminCommand("super@hub.com", IdentifierType.Email, "Pass123!", ValidMasterKey);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);

            // Verify
            // SỬA LỖI: Dùng CommonLogs.System_ConfigMissing.Code thay vì chuỗi cứng dễ sai sót
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(CommonLogs.System_ConfigMissing.Code)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenMasterKeyIsInvalid()
        {
            // Arrange
            var command = new RegisterSupperAdminCommand("super@hub.com", IdentifierType.Email, "Pass123!", "WrongKey");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CommonErrors.InvalidMasterKey, result.Error);

            // Verify
            // SỬA LỖI: Dùng CommonLogs.Auth_InvalidMasterKey.Code
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(CommonLogs.Auth_InvalidMasterKey.Code)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenIdentifierAlreadyExists()
        {
            // Arrange
            var command = new RegisterSupperAdminCommand("super@hub.com", IdentifierType.Email, "Pass123!", ValidMasterKey);

            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.EmailAlreadyExists, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenFactoryReturnsFailure()
        {
            // Arrange
            var command = new RegisterSupperAdminCommand("super@hub.com", IdentifierType.Email, "Pass123!", ValidMasterKey);

            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var expectedError = AccountErrors.InvalidEmail;
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(),
                    command.Value,
                    command.Type,
                    command.Password,
                    It.IsAny<Guid>(),
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(Result<Maybe<Account>>.Failure(expectedError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldSucceed_WhenAllValid()
        {
            // Arrange
            var command = new RegisterSupperAdminCommand("super@hub.com", IdentifierType.Email, "Pass123!", ValidMasterKey);

            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var dummyPassword = Password.From(new byte[32], new byte[16]);

            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(),
                    command.Value,
                    command.Type,
                    command.Password,
                    It.IsAny<Guid>(),
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

            // Verify
            _accountRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Account>(a => a.Id == result.Value),
                It.IsAny<CancellationToken>()), Times.Once);

            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}