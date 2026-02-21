using ControlHub.Application.Accounts.Commands.ForgotPassword;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Messaging.Outbox;
using ControlHub.Application.Messaging.Outbox.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Rules;
using ControlHub.Domain.Identity.Identifiers.Services;   // Namespace ch?a IdentifierFactory
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.Domain.TokenManagement.Aggregates;
using ControlHub.Domain.TokenManagement.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IPasswordResetTokenGenerator> _passwordResetTokenGeneratorMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _loggerMock = new();

        // Ðã xóa Mock<IIdentifierValidatorFactory>
        // private readonly Mock<IIdentifierValidatorFactory> _validatorFactoryMock = new();

        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ITokenRepository> _tokenRepositoryMock = new();
        private readonly Mock<ITokenFactory> _tokenFactoryMock = new();
        private readonly Mock<IOutboxRepository> _outboxRepositoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();

        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        // S? d?ng Factory th?t
        private readonly IdentifierFactory _identifierFactory;
        private readonly ForgotPasswordCommandHandler _handler;

        private const string ValidBaseUrl = "https://api.controlhub.com";

        public ForgotPasswordCommandHandlerTests()
        {
            // Setup Validator M?c d?nh (Email) cho Happy Path
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                          .Returns((true, "normalized_value", null));

            // Kh?i t?o Factory th?t v?i Mock Validator
            _identifierFactory = new IdentifierFactory(
                new[] { _validatorMock.Object },
                new Mock<IIdentifierConfigRepository>().Object,
                new DynamicIdentifierValidator());

            // Setup Happy Path Config
            _configMock.Setup(x => x["BaseUrl:DevBaseUrl"]).Returns(ValidBaseUrl);

            _handler = new ForgotPasswordCommandHandler(
                _passwordResetTokenGeneratorMock.Object,
                _accountRepositoryMock.Object,
                _loggerMock.Object,
                _identifierFactory, // Inject Factory th?t
                _uowMock.Object,
                _tokenRepositoryMock.Object,
                _tokenFactoryMock.Object,
                _outboxRepositoryMock.Object,
                _configMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: LOGIC NGHI?P V? & B?O M?T (SECURITY RULES)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsDeleted()
        {
            // Arrange
            var command = new ForgotPasswordCommand("deleted@test.com", IdentifierType.Email);
            var account = CreateDummyAccount(isDeleted: true);

            SetupHappyPathDependencies(command, account, "token123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I B?O M?T: H? th?ng v?n g?i email cho tài kho?n dã b? xóa (IsDeleted=true).");
            Assert.Equal(AccountErrors.AccountDeleted, result.Error);

            // Verify: Không du?c luu message g?i di
            _outboxRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // Arrange
            var command = new ForgotPasswordCommand("inactive@test.com", IdentifierType.Email);
            var account = CreateDummyAccount(isActive: false);

            SetupHappyPathDependencies(command, account, "token123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I LOGIC: H? th?ng v?n g?i email cho tài kho?n dã b? khóa (IsActive=false).");
            Assert.Equal(AccountErrors.AccountDisabled, result.Error);
            _outboxRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsUnsupported()
        {
            // Arrange
            // Factory hi?n t?i ch? có Email Validator (t? Constructor).
            // G?i yêu c?u Phone -> Factory s? không tìm th?y validator tuong ?ng.
            var command = new ForgotPasswordCommand("0909123456", IdentifierType.Phone);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.UnsupportedIdentifierType, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierInvalid()
        {
            // Arrange
            var command = new ForgotPasswordCommand("invalid-email", IdentifierType.Email);
            var validationError = Error.Validation("InvalidFormat", "Email bad format");

            // Setup Validator tr? v? l?i
            // Luu ý: Ph?i setup Type = Email d? Factory ch?n dúng validator này
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value))
                .Returns((false, string.Empty, validationError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(validationError, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountNotFound()
        {
            // Arrange
            var command = new ForgotPasswordCommand("notfound@test.com", IdentifierType.Email);
            string normalized = "notfound@test.com";

            // Setup Validator OK
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value))
                .Returns((true, normalized, Error.None));

            // Gi? l?p Query tr? v? Null
            _accountRepositoryMock
                .Setup(q => q.GetByIdentifierWithoutUserAsync(command.Type, normalized, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.IdentifierNotFound, result.Error);
        }

        // =================================================================================
        // NHÓM 2: C?U HÌNH & HARDCODE (ROBUSTNESS)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldUseBaseUrlFromConfig_AndNotHardcodedLocalhost()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account, "token123");

            // Setup Config tr? v? URL Production
            _configMock.Setup(x => x["BaseUrl:DevBaseUrl"]).Returns("https://production-api.com");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Verify
            _outboxRepositoryMock.Verify(x => x.AddAsync(
                It.Is<OutboxMessage>(msg =>
                    msg.Payload.Contains("https://production-api.com") && // Ph?i dùng URL t? config
                    !msg.Payload.Contains("localhost")),                 // Tuy?t d?i không hardcode localhost
                It.IsAny<CancellationToken>()),
                Times.Once,
                "L?I HARDCODE: Link reset password không s? d?ng Base URL t? c?u hình.");
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenClientBaseUrlConfigIsMissing()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account, "token123");

            // Gi? l?p quên c?u hình (tr? v? null ho?c r?ng)
            _configMock.Setup(x => x["BaseUrl:DevBaseUrl"]).Returns((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I: Handler không ki?m tra Config b? thi?u.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);
        }

        // =================================================================================
        // NHÓM 3: X? LÝ L?I PH? THU?C (DEPENDENCY FAILURES)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenGenerationReturnsEmpty()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupValidatorAndQuery(command, account);

            // Gi? l?p l?i: Generator tr? v? chu?i r?ng (thay vì null)
            _passwordResetTokenGeneratorMock
                .Setup(x => x.Generate(It.IsAny<string>()))
                .Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Mong d?i: Code ph?i check string.IsNullOrWhiteSpace và tr? v? l?i, KHÔNG ÐU?C CRASH (ArgumentException t? Domain)
            Assert.True(result.IsFailure, "L?I CRASH: Handler b? s?p do không ki?m tra k?t qu? t? Token Generator.");
            Assert.Equal(TokenErrors.TokenGenerationFailed, result.Error); // C?n d?m b?o Handler tr? v? dúng l?i này
        }

        // =================================================================================
        // NHÓM 4: HAPPY PATH
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_WhenAllValid()
        {
            // Arrange
            var command = new ForgotPasswordCommand("valid@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            var tokenStr = "valid-reset-token";

            SetupHappyPathDependencies(command, account, tokenStr);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify: 
            // 1. Token du?c luu
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == tokenStr && t.Type == TokenType.ResetPassword),
                It.IsAny<CancellationToken>()), Times.Once);

            // 2. Email du?c g?i
            _outboxRepositoryMock.Verify(r => r.AddAsync(
                It.Is<OutboxMessage>(m => m.Type == OutboxMessageType.Email && m.Payload.Contains(tokenStr)),
                It.IsAny<CancellationToken>()), Times.Once);

            // 3. Commit Transaction
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // =================================================================================
        // HELPER METHODS
        // =================================================================================

        private Account CreateDummyAccount(bool isDeleted = false, bool isActive = true)
        {
            var password = Password.From(new byte[32], new byte[16]);
            var account = Account.Create(Guid.NewGuid(), password, Guid.NewGuid());

            if (!isActive) account.Deactivate();
            if (isDeleted) account.Delete();

            return account;
        }

        private void SetupHappyPathDependencies(ForgotPasswordCommand command, Account account, string token)
        {
            SetupValidatorAndQuery(command, account);

            _passwordResetTokenGeneratorMock.Setup(x => x.Generate(It.IsAny<string>())).Returns(token);

            // Mock Factory tr? v? Token h?p l?
            var domainToken = Token.Create(account.Id, token, TokenType.ResetPassword, DateTime.UtcNow.AddMinutes(15));
            _tokenFactoryMock
                .Setup(x => x.Create(It.IsAny<Guid>(), token, TokenType.ResetPassword))
                .Returns(domainToken);
        }

        private void SetupValidatorAndQuery(ForgotPasswordCommand command, Account account)
        {
            // Setup Validator
            _validatorMock.Setup(v => v.Type).Returns(command.Type);
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value))
                .Returns((true, command.Value, Error.None));

            // Setup Query
            _accountRepositoryMock
                .Setup(q => q.GetByIdentifierWithoutUserAsync(command.Type, command.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);
        }
    }
}
