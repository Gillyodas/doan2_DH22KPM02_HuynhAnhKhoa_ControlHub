using ControlHub.Application.Identity.Commands.SignIn;
using ControlHub.Application.Identity.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.TokenManagement.Interfaces;
using ControlHub.Application.TokenManagement.Interfaces.Generate;
using ControlHub.Application.TokenManagement.Interfaces.Repositories;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Rules;
using ControlHub.Domain.Identity.Identifiers.Services;   // Ch?a IdentifierFactory
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.Domain.TokenManagement.Aggregates;
using ControlHub.Domain.TokenManagement.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class SignInCommandHandlerTests
    {
        private readonly Mock<ILogger<SignInCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountQueries> _accountQueriesMock = new();

        // Thay Mock<IIdentifierValidatorFactory> b?ng Mock Validator don l?
        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IAccessTokenGenerator> _accessTokenGeneratorMock = new();
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
        private readonly Mock<ITokenFactory> _tokenFactoryMock = new();
        private readonly Mock<ITokenRepository> _tokenRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPublisher> _publisherMock = new();

        // Dùng Factory th?t (Concrete Class)
        private readonly IdentifierFactory _identifierFactory;
        private readonly SignInCommandHandler _handler;

        public SignInCommandHandlerTests()
        {
            // Setup Validator m?c d?nh cho Happy Path (Email)
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                          .Returns((true, "normalized@test.com", null));

            // Kh?i t?o Factory th?t
            _identifierFactory = new IdentifierFactory(
                new[] { _validatorMock.Object },
                new Mock<IIdentifierConfigRepository>().Object,
                new DynamicIdentifierValidator());

            _handler = new SignInCommandHandler(
                _loggerMock.Object,
                _accountQueriesMock.Object,
                _passwordHasherMock.Object,
                _accessTokenGeneratorMock.Object,
                _refreshTokenGeneratorMock.Object,
                _tokenFactoryMock.Object,
                _tokenRepositoryMock.Object,
                _uowMock.Object,
                _publisherMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: B?O M?T TÀI KHO?N (SECURITY RULES)
        // M?c tiêu: Ð?m b?o tài kho?n b? xóa ho?c b? khóa KHÔNG TH? dang nh?p.
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsDeleted()
        {
            // ?? BUG HUNT: N?u Handler ch? check "Account != null" mà quên check "IsDeleted",
            // test này s? FAIL (vì login thành công). Ðây là l? h?ng b?o m?t.

            // Arrange
            var command = new SignInCommand("deleted@test.com", "Pass123!");
            var account = CreateDummyAccount(isDeleted: true); // Account dã b? xóa

            SetupHappyPathDependencies(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I B?O M?T: H? th?ng cho phép tài kho?n dã b? xóa (IsDeleted=true) dang nh?p.");

            // Tr? v? InvalidCredentials d? b?o m?t (không ti?t l? tr?ng thái tk) ho?c AccountDeleted tùy policy
            Assert.True(result.Error == AccountErrors.InvalidCredentials || result.Error == AccountErrors.AccountDeleted);

            // Verify: Không du?c sinh Token
            _tokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // ?? BUG HUNT: Tuong t?, tài kho?n b? Admin khóa (Deactivated) không du?c phép dang nh?p.

            // Arrange
            var command = new SignInCommand("locked@test.com", "Pass123!");
            var account = CreateDummyAccount(isActive: false); // Account b? khóa

            SetupHappyPathDependencies(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I B?O M?T: H? th?ng cho phép tài kho?n dang b? khóa (IsActive=false) dang nh?p.");
            Assert.True(result.Error == AccountErrors.InvalidCredentials || result.Error == AccountErrors.AccountDisabled);

            // Verify
            _tokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 2: Ð? B?N V?NG (ROBUSTNESS)
        // M?c tiêu: X? lý l?i h? th?ng (Token Generator h?ng) mà không Crash.
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenGenerationReturnsEmpty()
        {
            // ?? BUG HUNT: N?u Generator tr? v? null/empty (do l?i config ho?c thu?t toán),
            // Handler ph?i b?t du?c và tr? v? Error, không du?c dùng chu?i r?ng d? t?o Token (s? gây crash ? Domain).

            // Arrange
            var command = new SignInCommand("test@test.com", "Pass123!");
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account);

            // GI? L?P L?I: AccessToken Generator tr? v? chu?i r?ng
            _accessTokenGeneratorMock
                .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "L?I: Handler không x? lý tru?ng h?p sinh Token th?t b?i.");
            Assert.Equal(TokenErrors.TokenGenerationFailed, result.Error);

            // Verify: Ph?i log Error d? Admin bi?t h? th?ng Token dang l?i
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to generate tokens") || v.ToString()!.Contains("TokenInvalid")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // =================================================================================
        // NHÓM 3: LOGIC NGHI?P V? CO B?N & FACTORY
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsUnsupported()
        {
            // Arrange
            // Factory th?t ch? ch?a EmailValidator (dã setup trong constructor).
            // G?i lo?i Phone -> Factory s? không tìm th?y -> Tr? v? l?i Unsupported.
            var command = new SignInCommand("0909123456", "Pass123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.UnsupportedIdentifierType, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierIsInvalid()
        {
            // Arrange
            var command = new SignInCommand("invalid-email", "Pass123!");
            var validationError = Error.Validation("InvalidFormat", "Email format invalid");

            // Setup Validator tr? v? False
            _validatorMock
                .Setup(v => v.ValidateAndNormalize(command.Value))
                .Returns((false, string.Empty, validationError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(validationError, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPasswordIsIncorrect()
        {
            // Arrange
            var command = new SignInCommand("test@test.com", "WrongPass");
            var account = CreateDummyAccount();

            // Setup Validator & Account Query OK
            string normalized = "test@test.com";
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value)).Returns((true, normalized, Error.None));
            _accountQueriesMock.Setup(q => q.GetByIdentifierAsync(normalized, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(account);

            // Hasher tr? v? False
            _passwordHasherMock.Setup(h => h.Verify(command.Password, It.IsAny<Password>())).Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.InvalidCredentials, result.Error);
        }

        // =================================================================================
        // NHÓM 4: HAPPY PATH
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_AndSaveTokens_WhenAllValid()
        {
            // Arrange
            var command = new SignInCommand("test@test.com", "Pass123!");
            var account = CreateDummyAccount();
            var accessTokenStr = "access_token_123";
            var refreshTokenStr = "refresh_token_456";

            SetupHappyPathDependencies(command, account, accessTokenStr, refreshTokenStr);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(accessTokenStr, result.Value.AccessToken);
            Assert.Equal(refreshTokenStr, result.Value.RefreshToken);
            Assert.Equal(account.Id, result.Value.AccountId);

            // Verify Side Effects
            // 1. Ph?i luu Access Token
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == accessTokenStr && t.Type == TokenType.AccessToken),
                It.IsAny<CancellationToken>()), Times.Once);

            // 2. Ph?i luu Refresh Token
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == refreshTokenStr && t.Type == TokenType.RefreshToken),
                It.IsAny<CancellationToken>()), Times.Once);

            // 3. Ph?i Commit Transaction
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // =================================================================================
        // HELPER METHODS
        // =================================================================================

        private Account CreateDummyAccount(bool isDeleted = false, bool isActive = true)
        {
            var password = Password.From(new byte[32], new byte[16]);
            var account = Account.Create(Guid.NewGuid(), password, Guid.NewGuid());

            // Add Identifier d? pass check
            account.AddIdentifier(Identifier.Create(IdentifierType.Email, "test@test.com", "test@test.com"));

            // Attach User d? l?y username
            account.AttachUser(new User(Guid.NewGuid(), account.Id, "TestUser"));

            if (!isActive) account.Deactivate();
            if (isDeleted) account.Delete();

            return account;
        }

        private void SetupHappyPathDependencies(SignInCommand command, Account account, string accessToken = "at", string refreshToken = "rt")
        {
            string normalized = "test@test.com";

            // 1. Validator & Query
            // (Ðã setup Type trong constructor, ch? c?n setup ValidateAndNormalize)
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value)).Returns((true, normalized, Error.None));
            _accountQueriesMock.Setup(q => q.GetByIdentifierAsync(normalized, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(account);

            // 2. Password Check OK
            _passwordHasherMock.Setup(h => h.Verify(command.Password, It.IsAny<Password>())).Returns(true);

            // 3. Generators OK
            _accessTokenGeneratorMock.Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                     .Returns(accessToken);
            _refreshTokenGeneratorMock.Setup(g => g.Generate()).Returns(refreshToken);

            // 4. Token Factory OK
            _tokenFactoryMock.Setup(f => f.Create(It.IsAny<Guid>(), accessToken, TokenType.AccessToken))
                .Returns(Token.Create(account.Id, accessToken, TokenType.AccessToken, DateTime.UtcNow.AddMinutes(15)));

            _tokenFactoryMock.Setup(f => f.Create(It.IsAny<Guid>(), refreshToken, TokenType.RefreshToken))
                .Returns(Token.Create(account.Id, refreshToken, TokenType.RefreshToken, DateTime.UtcNow.AddDays(7)));

            // 5. Role Query OK
            _accountQueriesMock.Setup(q => q.GetRoleIdByAccIdAsync(account.Id, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(account.RoleId);
        }
    }
}
