using ControlHub.Application.Accounts.Commands.SignIn;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers.Rules;
using ControlHub.Domain.Accounts.Identifiers.Services;   // Chứa IdentifierFactory
using ControlHub.Domain.Accounts.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Tokens;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class SignInCommandHandlerTests
    {
        private readonly Mock<ILogger<SignInCommandHandler>> _loggerMock = new();
        private readonly Mock<IAccountQueries> _accountQueriesMock = new();

        // Thay Mock<IIdentifierValidatorFactory> bằng Mock Validator đơn lẻ
        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IAccessTokenGenerator> _accessTokenGeneratorMock = new();
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
        private readonly Mock<ITokenFactory> _tokenFactoryMock = new();
        private readonly Mock<ITokenRepository> _tokenRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        // Dùng Factory thật (Concrete Class)
        private readonly IdentifierFactory _identifierFactory;
        private readonly SignInCommandHandler _handler;

        public SignInCommandHandlerTests()
        {
            // Setup Validator mặc định cho Happy Path (Email)
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                          .Returns((true, "normalized@test.com", null));

            // Khởi tạo Factory thật
            _identifierFactory = new IdentifierFactory(
                new[] { _validatorMock.Object },
                new Mock<IIdentifierConfigRepository>().Object,
                new DynamicIdentifierValidator());

            _handler = new SignInCommandHandler(
                _loggerMock.Object,
                _accountQueriesMock.Object,
                _identifierFactory, // Inject Factory thật
                _passwordHasherMock.Object,
                _accessTokenGeneratorMock.Object,
                _refreshTokenGeneratorMock.Object,
                _tokenFactoryMock.Object,
                _tokenRepositoryMock.Object,
                _uowMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: BẢO MẬT TÀI KHOẢN (SECURITY RULES)
        // Mục tiêu: Đảm bảo tài khoản bị xóa hoặc bị khóa KHÔNG THỂ đăng nhập.
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsDeleted()
        {
            // 🐛 BUG HUNT: Nếu Handler chỉ check "Account != null" mà quên check "IsDeleted",
            // test này sẽ FAIL (vì login thành công). Đây là lỗ hổng bảo mật.

            // Arrange
            var command = new SignInCommand("deleted@test.com", "Pass123!", IdentifierType.Email);
            var account = CreateDummyAccount(isDeleted: true); // Account đã bị xóa

            SetupHappyPathDependencies(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI BẢO MẬT: Hệ thống cho phép tài khoản đã bị xóa (IsDeleted=true) đăng nhập.");

            // Trả về InvalidCredentials để bảo mật (không tiết lộ trạng thái tk) hoặc AccountDeleted tùy policy
            Assert.True(result.Error == AccountErrors.InvalidCredentials || result.Error == AccountErrors.AccountDeleted);

            // Verify: Không được sinh Token
            _tokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // 🐛 BUG HUNT: Tương tự, tài khoản bị Admin khóa (Deactivated) không được phép đăng nhập.

            // Arrange
            var command = new SignInCommand("locked@test.com", "Pass123!", IdentifierType.Email);
            var account = CreateDummyAccount(isActive: false); // Account bị khóa

            SetupHappyPathDependencies(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI BẢO MẬT: Hệ thống cho phép tài khoản đang bị khóa (IsActive=false) đăng nhập.");
            Assert.True(result.Error == AccountErrors.InvalidCredentials || result.Error == AccountErrors.AccountDisabled);

            // Verify
            _tokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Token>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 2: ĐỘ BỀN VỮNG (ROBUSTNESS)
        // Mục tiêu: Xử lý lỗi hệ thống (Token Generator hỏng) mà không Crash.
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenGenerationReturnsEmpty()
        {
            // 🐛 BUG HUNT: Nếu Generator trả về null/empty (do lỗi config hoặc thuật toán),
            // Handler phải bắt được và trả về Error, không được dùng chuỗi rỗng để tạo Token (sẽ gây crash ở Domain).

            // Arrange
            var command = new SignInCommand("test@test.com", "Pass123!", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account);

            // GIẢ LẬP LỖI: AccessToken Generator trả về chuỗi rỗng
            _accessTokenGeneratorMock
                .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI: Handler không xử lý trường hợp sinh Token thất bại.");
            Assert.Equal(TokenErrors.TokenGenerationFailed, result.Error);

            // Verify: Phải log Error để Admin biết hệ thống Token đang lỗi
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
        // NHÓM 3: LOGIC NGHIỆP VỤ CƠ BẢN & FACTORY
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsUnsupported()
        {
            // Arrange
            // Factory thật chỉ chứa EmailValidator (đã setup trong constructor).
            // Gửi loại Phone -> Factory sẽ không tìm thấy -> Trả về lỗi Unsupported.
            var command = new SignInCommand("0909123456", "Pass123!", IdentifierType.Phone);

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
            var command = new SignInCommand("invalid-email", "Pass123!", IdentifierType.Email);
            var validationError = Error.Validation("InvalidFormat", "Email format invalid");

            // Setup Validator trả về False
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
            var command = new SignInCommand("test@test.com", "WrongPass", IdentifierType.Email);
            var account = CreateDummyAccount();

            // Setup Validator & Account Query OK
            string normalized = "test@test.com";
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value)).Returns((true, normalized, Error.None));
            _accountQueriesMock.Setup(q => q.GetByIdentifierAsync(command.Type, normalized, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(account);

            // Hasher trả về False
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
            var command = new SignInCommand("test@test.com", "Pass123!", IdentifierType.Email);
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
            // 1. Phải lưu Access Token
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == accessTokenStr && t.Type == TokenType.AccessToken),
                It.IsAny<CancellationToken>()), Times.Once);

            // 2. Phải lưu Refresh Token
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == refreshTokenStr && t.Type == TokenType.RefreshToken),
                It.IsAny<CancellationToken>()), Times.Once);

            // 3. Phải Commit Transaction
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // =================================================================================
        // HELPER METHODS
        // =================================================================================

        private Account CreateDummyAccount(bool isDeleted = false, bool isActive = true)
        {
            var password = Password.From(new byte[32], new byte[16]);
            var account = Account.Create(Guid.NewGuid(), password, Guid.NewGuid());

            // Add Identifier để pass check
            account.AddIdentifier(Identifier.Create(IdentifierType.Email, "test@test.com", "test@test.com"));

            // Attach User để lấy username
            account.AttachUser(new User(Guid.NewGuid(), account.Id, "TestUser"));

            if (!isActive) account.Deactivate();
            if (isDeleted) account.Delete();

            return account;
        }

        private void SetupHappyPathDependencies(SignInCommand command, Account account, string accessToken = "at", string refreshToken = "rt")
        {
            string normalized = "test@test.com";

            // 1. Validator & Query
            // (Đã setup Type trong constructor, chỉ cần setup ValidateAndNormalize)
            _validatorMock.Setup(v => v.ValidateAndNormalize(command.Value)).Returns((true, normalized, Error.None));
            _accountQueriesMock.Setup(q => q.GetByIdentifierAsync(command.Type, normalized, It.IsAny<CancellationToken>()))
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