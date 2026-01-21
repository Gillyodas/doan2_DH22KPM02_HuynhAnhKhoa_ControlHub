using ControlHub.Application.Accounts.Commands.ForgotPassword;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers.Rules;
using ControlHub.Domain.Accounts.Identifiers.Services;   // Namespace chứa IdentifierFactory
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Outboxs;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Tokens.Enums;
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

        // Đã xóa Mock<IIdentifierValidatorFactory>
        // private readonly Mock<IIdentifierValidatorFactory> _validatorFactoryMock = new();

        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ITokenRepository> _tokenRepositoryMock = new();
        private readonly Mock<ITokenFactory> _tokenFactoryMock = new();
        private readonly Mock<IOutboxRepository> _outboxRepositoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();

        private readonly Mock<IIdentifierValidator> _validatorMock = new();

        // Sử dụng Factory thật
        private readonly IdentifierFactory _identifierFactory;
        private readonly ForgotPasswordCommandHandler _handler;

        private const string ValidBaseUrl = "https://api.controlhub.com";

        public ForgotPasswordCommandHandlerTests()
        {
            // Setup Validator Mặc định (Email) cho Happy Path
            _validatorMock.Setup(v => v.Type).Returns(IdentifierType.Email);
            _validatorMock.Setup(v => v.ValidateAndNormalize(It.IsAny<string>()))
                          .Returns((true, "normalized_value", null));

            // Khởi tạo Factory thật với Mock Validator
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
                _identifierFactory, // Inject Factory thật
                _uowMock.Object,
                _tokenRepositoryMock.Object,
                _tokenFactoryMock.Object,
                _outboxRepositoryMock.Object,
                _configMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: LOGIC NGHIỆP VỤ & BẢO MẬT (SECURITY RULES)
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
            Assert.True(result.IsFailure, "LỖI BẢO MẬT: Hệ thống vẫn gửi email cho tài khoản đã bị xóa (IsDeleted=true).");
            Assert.Equal(AccountErrors.AccountDeleted, result.Error);

            // Verify: Không được lưu message gửi đi
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
            Assert.True(result.IsFailure, "LỖI LOGIC: Hệ thống vẫn gửi email cho tài khoản đã bị khóa (IsActive=false).");
            Assert.Equal(AccountErrors.AccountDisabled, result.Error);
            _outboxRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenIdentifierTypeIsUnsupported()
        {
            // Arrange
            // Factory hiện tại chỉ có Email Validator (từ Constructor).
            // Gửi yêu cầu Phone -> Factory sẽ không tìm thấy validator tương ứng.
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

            // Setup Validator trả về lỗi
            // Lưu ý: Phải setup Type = Email để Factory chọn đúng validator này
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

            // Giả lập Query trả về Null
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
        // NHÓM 2: CẤU HÌNH & HARDCODE (ROBUSTNESS)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldUseBaseUrlFromConfig_AndNotHardcodedLocalhost()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account, "token123");

            // Setup Config trả về URL Production
            _configMock.Setup(x => x["BaseUrl:DevBaseUrl"]).Returns("https://production-api.com");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Verify
            _outboxRepositoryMock.Verify(x => x.AddAsync(
                It.Is<OutboxMessage>(msg =>
                    msg.Payload.Contains("https://production-api.com") && // Phải dùng URL từ config
                    !msg.Payload.Contains("localhost")),                 // Tuyệt đối không hardcode localhost
                It.IsAny<CancellationToken>()),
                Times.Once,
                "LỖI HARDCODE: Link reset password không sử dụng Base URL từ cấu hình.");
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenClientBaseUrlConfigIsMissing()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupHappyPathDependencies(command, account, "token123");

            // Giả lập quên cấu hình (trả về null hoặc rỗng)
            _configMock.Setup(x => x["BaseUrl:DevBaseUrl"]).Returns((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure, "LỖI: Handler không kiểm tra Config bị thiếu.");
            Assert.Equal(CommonErrors.SystemConfigurationError, result.Error);
        }

        // =================================================================================
        // NHÓM 3: XỬ LÝ LỖI PHỤ THUỘC (DEPENDENCY FAILURES)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenGenerationReturnsEmpty()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@test.com", IdentifierType.Email);
            var account = CreateDummyAccount();
            SetupValidatorAndQuery(command, account);

            // Giả lập lỗi: Generator trả về chuỗi rỗng (thay vì null)
            _passwordResetTokenGeneratorMock
                .Setup(x => x.Generate(It.IsAny<string>()))
                .Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Mong đợi: Code phải check string.IsNullOrWhiteSpace và trả về lỗi, KHÔNG ĐƯỢC CRASH (ArgumentException từ Domain)
            Assert.True(result.IsFailure, "LỖI CRASH: Handler bị sập do không kiểm tra kết quả từ Token Generator.");
            Assert.Equal(TokenErrors.TokenGenerationFailed, result.Error); // Cần đảm bảo Handler trả về đúng lỗi này
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
            // 1. Token được lưu
            _tokenRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Token>(t => t.Value == tokenStr && t.Type == TokenType.ResetPassword),
                It.IsAny<CancellationToken>()), Times.Once);

            // 2. Email được gửi
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

            // Mock Factory trả về Token hợp lệ
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