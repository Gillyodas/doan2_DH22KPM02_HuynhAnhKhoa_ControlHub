using ControlHub.Application.Accounts.Commands.AddIdentifier;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers.Rules;
using ControlHub.Domain.Accounts.Identifiers.Services;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class AddIdentifierCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<AddIdentifierCommandHandler>> _loggerMock = new();

        // Mock Validator để điều khiển IdentifierFactory
        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        private readonly AddIdentifierCommandHandler _handler;

        public AddIdentifierCommandHandlerTests()
        {
            // Setup Validator mặc định: Hỗ trợ Email và luôn Valid
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock
                .Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                .Returns((true, "normalized_value", null));

            // Khởi tạo Factory thật với Mock Validator
            var identifierFactory = new IdentifierFactory(
                new[] { _validatorMock.Object },
                new Mock<IIdentifierConfigRepository>().Object,
                new DynamicIdentifierValidator());

            _handler = new AddIdentifierCommandHandler(
                _loggerMock.Object,
                _uowMock.Object,
                _accountRepositoryMock.Object,
                identifierFactory
            );
        }

        // =================================================================================
        // NHÓM 1: LOGIC NGHIỆP VỤ & BẢO MẬT (Security & Business Rules)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountNotFound()
        {
            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());

            _accountRepositoryMock
                .Setup(r => r.GetWithoutUserByIdAsync(command.id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.AccountNotFound, result.Error);

            // Verify: Không được gọi Commit
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsDeleted()
        {
            // 🐛 BUG HUNT: Đảm bảo không thể thêm thông tin vào tài khoản đã bị xóa.

            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount(isDeleted: true);

            SetupRepoToReturnAccount(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI BẢO MẬT: Hệ thống cho phép thêm identifier vào tài khoản đã xóa.");
            Assert.Equal(AccountErrors.AccountDeleted, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // 🐛 BUG HUNT: Đảm bảo tài khoản bị khóa (Disabled) không được thay đổi thông tin.

            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount(isActive: false);

            SetupRepoToReturnAccount(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI LOGIC: Hệ thống cho phép thêm identifier vào tài khoản đang bị khóa.");
            Assert.Equal(AccountErrors.AccountDisabled, result.Error);
        }

        // =================================================================================
        // NHÓM 2: VALIDATION DỮ LIỆU ĐẦU VÀO (Input Validation via Factory)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsNotSupported()
        {
            // Arrange
            // Command gửi loại Phone, nhưng Validator mock chỉ hỗ trợ Email
            var command = new AddIdentifierCommand("0909123456", IdentifierType.Phone, Guid.NewGuid());
            var account = CreateDummyAccount();
            SetupRepoToReturnAccount(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.UnsupportedIdentifierType, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierFormatIsInvalid()
        {
            // Arrange
            var command = new AddIdentifierCommand("invalid-email", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount();
            SetupRepoToReturnAccount(account);

            var validationError = Error.Validation("InvalidFormat", "Bad email");

            // Setup Validator trả về lỗi
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.value))
                .Returns((false, string.Empty, validationError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(validationError, result.Error);
        }

        // =================================================================================
        // NHÓM 3: LOGIC DOMAIN (Duplicate Check)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierAlreadyExistsInAccount()
        {
            // 🐛 BUG HUNT: Kiểm tra xem Domain có chặn trùng lặp trong danh sách Identifiers của Account không.

            // Arrange
            var existingEmail = "exist@test.com";
            var command = new AddIdentifierCommand(existingEmail, IdentifierType.Email, Guid.NewGuid());

            var account = CreateDummyAccount();
            // Đã có sẵn identifier này trong account
            account.AddIdentifier(Identifier.Create(IdentifierType.Email, existingEmail, existingEmail));

            SetupRepoToReturnAccount(account);

            // Validator setup (cho phép pass qua bước format check)
            _validatorMock.Setup(v => v.ValidateAndNormalize(existingEmail))
                .Returns((true, existingEmail, null));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.IdentifierAlreadyExists, result.Error);

            // Verify: Không commit
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 4: HAPPY PATH (Thành Công)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_AndPersist_WhenAllValid()
        {
            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount();
            SetupRepoToReturnAccount(account);

            // Validator OK
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.value))
                .Returns((true, "new@test.com", null));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify State: Identifier đã được thêm vào Account trong bộ nhớ chưa?
            Assert.Contains(account.Identifiers, i => i.Value == "new@test.com");

            // Verify Side Effect: Commit transaction
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once,
                "LỖI: Quên gọi Commit để lưu thay đổi xuống DB.");
        }

        // --- Helpers ---

        private Account CreateDummyAccount(bool isDeleted = false, bool isActive = true)
        {
            var password = Password.From(new byte[32], new byte[16]);
            var account = Account.Create(Guid.NewGuid(), password, Guid.NewGuid());

            if (!isActive) account.Deactivate();
            if (isDeleted) account.Delete();

            return account;
        }

        private void SetupRepoToReturnAccount(Account account)
        {
            _accountRepositoryMock
                .Setup(r => r.GetWithoutUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);
        }
    }
}