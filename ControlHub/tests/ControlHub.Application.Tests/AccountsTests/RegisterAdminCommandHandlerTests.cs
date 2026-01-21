using ControlHub.Application.Accounts.Commands.RegisterAdmin;
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
    public class RegisterAdminCommandHandlerTests
    {
        private readonly Mock<IAccountValidator> _accountValidatorMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        // Sửa lại Mock Logger cho đúng loại (Admin, không phải User)
        private readonly Mock<ILogger<RegisterAdminCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountFactory> _accountFactoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        private readonly RegisterAdminCommandHandler _handler;
        private readonly string _validRoleId = Guid.NewGuid().ToString();

        public RegisterAdminCommandHandlerTests()
        {
            // Setup mặc định: Config đúng
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
        // Mục tiêu: Bắt lỗi code đang bị Crash khi cấu hình sai
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminRoleIdConfigIsMissing()
        {
            // 🐛 BUG HUNT: Code hiện tại dùng Guid.Parse trực tiếp.
            // Nếu chạy test này, nó sẽ Fail do ArgumentNullException.
            // Điều này CHỨNG MINH code thiếu logic xử lý lỗi cấu hình.

            // Arrange
            _configMock.Setup(x => x["RoleSettings:AdminRoleId"]).Returns((string?)null);
            var command = new RegisterAdminCommand("admin@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Mong đợi: Code phải bắt lỗi và trả về Failure
            Assert.True(result.IsFailure, "LỖI: Handler bị Crash (Exception) thay vì trả về Result.Failure khi thiếu Config.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);

            // Verify: Không được gọi DB khi lỗi hệ thống
            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminRoleIdConfigIsInvalidFormat()
        {
            // 🐛 BUG HUNT: Code hiện tại sẽ Crash (FormatException).

            // Arrange
            _configMock.Setup(x => x["RoleSettings:AdminRoleId"]).Returns("invalid-guid-string");
            var command = new RegisterAdminCommand("admin@test.com", IdentifierType.Email, "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI: Handler bị Crash (FormatException) thay vì trả về Result.Failure khi Config sai định dạng.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);
        }

        // =================================================================================
        // NHÓM 2: LOGIC NGHIỆP VỤ (BUSINESS LOGIC)
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

            // Mock Factory: Kiểm tra xem có truyền đúng RoleId từ Config không
            _accountFactoryMock
                .Setup(f => f.CreateWithUserAndIdentifierAsync(
                    It.IsAny<Guid>(),
                    command.Value,
                    command.Type,
                    command.Password,
                    Guid.Parse(_validRoleId), // Verify logic lấy config
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