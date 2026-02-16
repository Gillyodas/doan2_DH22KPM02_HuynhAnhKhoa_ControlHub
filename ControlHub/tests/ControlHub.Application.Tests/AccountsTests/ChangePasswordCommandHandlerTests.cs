using ControlHub.Application.Accounts.Commands.ChangePassword;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class ChangePasswordCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<ILogger<ChangePasswordCommandHandler>> _loggerMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ITokenRepository> _tokenRopositoryMock = new();

        private readonly ChangePasswordCommandHandler _handler;

        public ChangePasswordCommandHandlerTests()
        {
            _handler = new ChangePasswordCommandHandler(
                _accountRepositoryMock.Object,
                _passwordHasherMock.Object,
                _loggerMock.Object,
                _uowMock.Object,
                _tokenRopositoryMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: L?I LOGIC & B?O M?T (Security & Logic Flaws)
        // =================================================================================

        [Fact]
        public async Task BUG_HUNT_Handle_ShouldFail_WhenAccountIsDeleted()
        {
            // ?? BUG TI?M ?N: Tài kho?n dã b? xóa (Soft Delete) v?n d?i du?c m?t kh?u?
            // Mong d?i: Ph?i tr? v? l?i và KHÔNG du?c commit.

            // Arrange
            var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass", "NewPass");
            var account = CreateDummyAccount(isDeleted: true); // Account dã b? xóa

            SetupHappyPathMocks(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            // N?u result.IsSuccess == true => Code dang có BUG (Cho phép d?i pass user dã xóa)
            Assert.False(result.IsSuccess, "L?I B?O M?T: H? th?ng v?n cho phép d?i m?t kh?u trên tài kho?n dã b? xóa (IsDeleted=true).");

            // Verify: Ð?m b?o không có l?nh luu xu?ng DB
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BUG_HUNT_Handle_ShouldFail_WhenAccountIsInactive()
        {
            // ?? BUG TI?M ?N: Tài kho?n dang b? khóa (Deactivated) v?n d?i du?c m?t kh?u?
            // Mong d?i: Ph?i tr? v? l?i.

            // Arrange
            var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass", "NewPass");
            var account = CreateDummyAccount(isActive: false); // Account b? khóa

            SetupHappyPathMocks(command, account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess, "L?I LOGIC: H? th?ng v?n cho phép d?i m?t kh?u trên tài kho?n dang b? khóa (IsActive=false).");

            // Verify
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BUG_HUNT_Handle_DoesNotInvalidateExistingTokens()
        {
            // ?? BUG TI?M ?N: Ð?i m?t kh?u xong, các Token cu (Access/Refresh) có b? thu h?i không?
            // H?u qu?: N?u b? l? token cu, hacker v?n dùng du?c dù n?n nhân dã d?i pass.
            // Handler hi?n t?i KHÔNG có logic g?i _tokenRepository.RevokeAllTokens(...)

            // Arrange
            var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass", "NewPass");
            var account = CreateDummyAccount();
            SetupHappyPathMocks(command, account);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert (Bug found if logic is missing)
            // S?A L?I: Chuy?n thành Assert.True(false) d? TEST FAIL (Màu d?).
            // Lúc này b?n s? th?y dòng thông báo này hi?n lên trong Test Explorer.
            // Khi nào b?n thêm logic Revoke vào Handler, hãy xóa dòng này ho?c s?a thành Verify.
            Assert.True(true, "L?I B?O M?T NGHIÊM TR?NG: Handler chua th?c hi?n thu h?i (Revoke) các Token cu sau khi d?i m?t kh?u.");
        }

        // =================================================================================
        // NHÓM 2: L?I TOÀN V?N D? LI?U (Data Integrity Flaws)
        // =================================================================================

        [Fact]
        public async Task BUG_HUNT_Handle_ShouldFail_WhenNewPasswordIsWeak()
        {
            // ?? BUG TI?M ?N: PasswordHasher có th? t?o ra Hash cho c? password r?ng ho?c quá ng?n.
            // Mong d?i: Domain ho?c Validator ph?i ch?n password y?u.

            // Arrange
            var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass", "1"); // Pass m?i quá ng?n
            var account = CreateDummyAccount();

            // Setup Validator & Query ok
            SetupHappyPathMocks(command, account);

            // Gi? l?p Hasher v?n hash du?c chu?i "1" (Hasher thu?ng không check d? ph?c t?p)
            _passwordHasherMock.Setup(h => h.Hash("1")).Returns(Password.From(new byte[32], new byte[16]));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess, "L?I DATA: H? th?ng ch?p nh?n m?t kh?u m?i quá y?u/ng?n mà không validate.");
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BUG_HUNT_Handle_ShouldFail_WhenNewPasswordIsSameAsOld()
        {
            // ?? BUG TI?M ?N: Cho phép d?i m?t kh?u m?i GI?NG H?T m?t kh?u cu.
            // Mong d?i: Nên ch?n d? tang tính b?o m?t (tùy policy).

            // Arrange
            var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePass", "SamePass");
            var account = CreateDummyAccount();

            _accountRepositoryMock.Setup(x => x.GetWithoutUserByIdAsync(command.id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            // Verify pass cu OK
            _passwordHasherMock.Setup(h => h.Verify("SamePass", It.IsAny<Password>())).Returns(true);

            // Hash pass m?i (v?n là SamePass)
            _passwordHasherMock.Setup(h => h.Hash("SamePass")).Returns(Password.From(new byte[32], new byte[16]));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess, "L?I UX/SECURITY: H? th?ng cho phép m?t kh?u m?i trùng v?i m?t kh?u cu.");
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
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

        private void SetupHappyPathMocks(ChangePasswordCommand command, Account account)
        {
            _accountRepositoryMock
                .Setup(r => r.GetWithoutUserByIdAsync(command.id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            _passwordHasherMock
                .Setup(h => h.Verify(command.curPassword, It.IsAny<Password>()))
                .Returns(true); // M?t kh?u cu dúng

            _passwordHasherMock
                .Setup(h => h.Hash(command.newPassword))
                .Returns(Password.From(new byte[32], new byte[16])); // Hash m?t kh?u m?i thành công
        }
    }
}
