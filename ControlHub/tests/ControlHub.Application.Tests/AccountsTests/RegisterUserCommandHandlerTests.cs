using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Accounts.Commands.RegisterUser;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class RegisterUserCommandHandlerTests
    {
        private readonly Mock<IAccountValidator> _accountValidatorMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountFactory> _accountFactoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        private readonly RegisterUserCommandHandler _handler;
        private readonly string _validRoleId = Guid.NewGuid().ToString();

        public RegisterUserCommandHandlerTests()
        {
            // Setup Happy Path: Config mặc định đúng
            _configMock.Setup(x => x["RoleSettings:UserRoleId"]).Returns(_validRoleId);

            _handler = new RegisterUserCommandHandler(
                _accountValidatorMock.Object,
                _accountRepositoryMock.Object,
                _loggerMock.Object,
                _accountFactoryMock.Object,
                _configMock.Object,
                _uowMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: BUG HUNTING - CẤU HÌNH & HỆ THỐNG (CONFIGURATION)
        // Mục tiêu: Bắt lỗi Handler bị Crash khi config thiếu hoặc sai.
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserRoleIdConfigIsMissing()
        {
            // 🐛 BUG HUNT: Nếu code dùng Guid.Parse(_config[...]) trực tiếp sẽ bị Crash (ArgumentNullException).
            // Test này ép buộc Handler phải dùng Guid.TryParse và check null.

            // Arrange
            _configMock.Setup(x => x["RoleSettings:UserRoleId"]).Returns((string?)null);
            var command = new RegisterUserCommand("user@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI CRASH: Handler bị sập (Exception) do thiếu config UserRoleId.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);

            // Verify Log
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid User Role ID") || v.ToString()!.Contains("System_ConfigMissing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "LỖI: Không ghi log Error khi cấu hình hệ thống bị sai.");
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserRoleIdConfigIsInvalidFormat()
        {
            // 🐛 BUG HUNT: Nếu config có giá trị nhưng không phải GUID -> Crash FormatException.

            // Arrange
            _configMock.Setup(x => x["RoleSettings:UserRoleId"]).Returns("invalid-guid-string");
            var command = new RegisterUserCommand("user@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI CRASH: Handler bị sập (Exception) do format UserRoleId sai.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);
        }

        // =================================================================================
        // NHÓM 2: LOGIC NGHIỆP VỤ (BUSINESS LOGIC)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierAlreadyExists()
        {
            // Arrange
            var command = new RegisterUserCommand("exist@test.com", IdentifierType.Email, "Pass123!");
            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.EmailAlreadyExists, result.Error);

            // Verify
            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenFactoryFails()
        {
            // Arrange
            var command = new RegisterUserCommand("new@test.com", IdentifierType.Email, "Pass123!");
            _accountValidatorMock.Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var expectedError = AccountErrors.InvalidEmail;
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(), command.Value, command.Type, command.Password, It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
                .ReturnsAsync(Result<Maybe<Account>>.Failure(expectedError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 3: LUỒNG THÀNH CÔNG (HAPPY PATH)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_AndPersistData_WhenAllValid()
        {
            // Arrange
            var command = new RegisterUserCommand("test@example.com", IdentifierType.Email, "Pass123!");

            _accountValidatorMock
                .Setup(v => v.IdentifierIsExist(It.IsAny<string>(), It.IsAny<IdentifierType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var dummyPassword = Password.From(new byte[32], new byte[16]);

            // Setup Factory trả về Success chứa Account
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(),
                    command.Value,
                    command.Type,
                    command.Password,
                    Guid.Parse(_validRoleId), // Verify: Phải dùng đúng UserRoleId từ Config
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
            _accountRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Account>(a => a.Id == result.Value),
                It.IsAny<CancellationToken>()), Times.Once);

            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}