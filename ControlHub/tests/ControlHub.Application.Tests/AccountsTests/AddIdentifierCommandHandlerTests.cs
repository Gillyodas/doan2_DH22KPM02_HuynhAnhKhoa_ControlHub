using ControlHub.Application.Accounts.Commands.AddIdentifier;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Rules;
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Domain.Identity.ValueObjects;
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

        // Mock Validator d? di?u khi?n IdentifierFactory
        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        private readonly AddIdentifierCommandHandler _handler;

        public AddIdentifierCommandHandlerTests()
        {
            // Setup Validator m?c d?nh: H? tr? Email và luôn Valid
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock
                .Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                .Returns((true, "normalized_value", null));

            // Kh?i t?o Factory th?t v?i Mock Validator
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
        // NHÓM 1: LOGIC NGHI?P V? & B?O M?T (Security & Business Rules)
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

            // Verify: Không du?c g?i Commit
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsDeleted()
        {
            // ?? BUG HUNT: Ð?m b?o không th? thêm thông tin vào tài kho?n dã b? xóa.

            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount(isDeleted: true);

            SetupRepoToReturnAccount(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I B?O M?T: H? th?ng cho phép thêm identifier vào tài kho?n dã xóa.");
            Assert.Equal(AccountErrors.AccountDeleted, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // ?? BUG HUNT: Ð?m b?o tài kho?n b? khóa (Disabled) không du?c thay d?i thông tin.

            // Arrange
            var command = new AddIdentifierCommand("new@test.com", IdentifierType.Email, Guid.NewGuid());
            var account = CreateDummyAccount(isActive: false);

            SetupRepoToReturnAccount(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I LOGIC: H? th?ng cho phép thêm identifier vào tài kho?n dang b? khóa.");
            Assert.Equal(AccountErrors.AccountDisabled, result.Error);
        }

        // =================================================================================
        // NHÓM 2: VALIDATION D? LI?U Ð?U VÀO (Input Validation via Factory)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsNotSupported()
        {
            // Arrange
            // Command g?i lo?i Phone, nhung Validator mock ch? h? tr? Email
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

            // Setup Validator tr? v? l?i
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
            // ?? BUG HUNT: Ki?m tra xem Domain có ch?n trùng l?p trong danh sách Identifiers c?a Account không.

            // Arrange
            var existingEmail = "exist@test.com";
            var command = new AddIdentifierCommand(existingEmail, IdentifierType.Email, Guid.NewGuid());

            var account = CreateDummyAccount();
            // Ðã có s?n identifier này trong account
            account.AddIdentifier(Identifier.Create(IdentifierType.Email, existingEmail, existingEmail));

            SetupRepoToReturnAccount(account);

            // Validator setup (cho phép pass qua bu?c format check)
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

            // Verify State: Identifier dã du?c thêm vào Account trong b? nh? chua?
            Assert.Contains(account.Identifiers, i => i.Value == "new@test.com");

            // Verify Side Effect: Commit transaction
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once,
                "L?I: Quên g?i Commit d? luu thay d?i xu?ng DB.");
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
