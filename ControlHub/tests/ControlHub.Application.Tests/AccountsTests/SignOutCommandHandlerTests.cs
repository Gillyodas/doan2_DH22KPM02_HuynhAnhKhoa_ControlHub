using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Accounts.Commands.SignOut;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Tokens;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AccountsTests
{
    public class SignOutCommandHandlerTests
    {
        private readonly Mock<ITokenRepository> _tokenRepositoryMock = new();
        private readonly Mock<ITokenQueries> _tokenQueriesMock = new();
        private readonly Mock<ITokenVerifier> _tokenVerifierMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ILogger<SignOutCommandHandler>> _loggerMock = new();

        private readonly SignOutCommandHandler _handler;

        public SignOutCommandHandlerTests()
        {
            _handler = new SignOutCommandHandler(
                _tokenRepositoryMock.Object,
                _tokenQueriesMock.Object,
                _tokenVerifierMock.Object,
                _uowMock.Object,
                _loggerMock.Object
            );
        }

        // =================================================================================
        // NHÓM 1: SECURITY & VALIDATION (B?o m?t & Xác th?c)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenVerificationFails()
        {
            // ?? BUG HUNT: Access Token không h?p l? (h?t h?n, sai ch? ký) -> Ph?i ch?n ngay.
            var command = new SignOutCommand("invalid_access_token", "refresh_token");

            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns((ClaimsPrincipal?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(TokenErrors.TokenInvalid, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIdInClaimIsInvalid()
        {
            // ?? BUG HUNT: Token gi? m?o v?i Claim "sub" không ph?i GUID -> Ph?i ch?n.
            var command = new SignOutCommand("access_token", "refresh_token");

            var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid") });
            var principal = new ClaimsPrincipal(identity);

            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns(principal);

            // Mock DB tr? v? token (d? vu?t qua check null tru?c dó)
            var fakeToken = Token.Create(Guid.NewGuid(), "val", TokenType.AccessToken, DateTime.UtcNow.AddMinutes(1));
            _tokenQueriesMock.Setup(q => q.GetByValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(fakeToken);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(TokenErrors.TokenInvalid, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIdMismatch()
        {
            // ?? BUG HUNT: Hacker dùng Token c?a mình d? logout Token c?a ngu?i khác -> Ph?i ch?n.
            // Token g?i lên (trong DB) thu?c Account A, nhung Claim trong Token l?i là Account B.

            var command = new SignOutCommand("access_token", "refresh_token");
            var tokenOwnerId = Guid.NewGuid();
            var attackerId = Guid.NewGuid();

            var principal = CreatePrincipal(attackerId); // Ngu?i g?i là Attacker
            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns(principal);

            // Token trong DB thu?c v? Owner
            var accessToken = Token.Create(tokenOwnerId, command.accessToken, TokenType.AccessToken, DateTime.UtcNow.AddMinutes(15));
            var refreshToken = Token.Create(tokenOwnerId, command.refreshToken, TokenType.RefreshToken, DateTime.UtcNow.AddDays(7));

            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.accessToken, It.IsAny<CancellationToken>())).ReturnsAsync(accessToken);
            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(refreshToken);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailure, "L?I B?O M?T: Cho phép logout token không thu?c v? ngu?i g?i (Mismatch ID).");
            Assert.Equal(TokenErrors.TokenInvalid, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 2: LOGIC NGHI?P V? (BUSINESS LOGIC)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokensNotFoundInStorage()
        {
            // ?? BUG HUNT: Token h?p l? v? m?t ch? ký nhung không có trong DB (dã b? xóa c?ng?) -> Coi nhu l?i.
            var command = new SignOutCommand("access_token", "refresh_token");
            var principal = CreatePrincipal(Guid.NewGuid());

            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns(principal);

            // DB tr? v? null
            _tokenQueriesMock.Setup(q => q.GetByValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync((Token?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(TokenErrors.TokenNotFound, result.Error);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenTokenAlreadyRevoked()
        {
            // ?? BUG HUNT: C? g?ng logout l?i m?t token dã logout r?i.
            // Domain logic c?a Token.Revoke() s? tr? v? Failure n?u IsRevoked=true.

            var command = new SignOutCommand("access_token", "refresh_token");
            var accountId = Guid.NewGuid();
            var principal = CreatePrincipal(accountId);

            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns(principal);

            // T?o token dã b? Revoke
            var revokedToken = Token.Rehydrate(Guid.NewGuid(), accountId, command.accessToken, TokenType.AccessToken,
                DateTime.UtcNow.AddMinutes(15), isUsed: false, isRevoked: true, DateTime.UtcNow); // isRevoked = true

            var refreshToken = Token.Create(accountId, command.refreshToken, TokenType.RefreshToken, DateTime.UtcNow.AddDays(7));

            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.accessToken, It.IsAny<CancellationToken>())).ReturnsAsync(revokedToken);
            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(refreshToken);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailure, "L?I LOGIC: Không ch?n vi?c logout l?i token dã b? thu h?i.");
            Assert.Equal(TokenErrors.TokenAlreadyRevoked, result.Error);
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // =================================================================================
        // NHÓM 3: LU?NG THÀNH CÔNG (HAPPY PATH)
        // =================================================================================

        [Fact]
        public async Task Handle_ShouldSucceed_AndRevokeBothTokens_WhenAllValid()
        {
            // Arrange
            var command = new SignOutCommand("access_token", "refresh_token");
            var accountId = Guid.NewGuid();
            var principal = CreatePrincipal(accountId);

            _tokenVerifierMock.Setup(v => v.Verify(command.accessToken)).Returns(principal);

            // T?o token h?p l?
            var accessToken = Token.Create(accountId, command.accessToken, TokenType.AccessToken, DateTime.UtcNow.AddMinutes(15));
            var refreshToken = Token.Create(accountId, command.refreshToken, TokenType.RefreshToken, DateTime.UtcNow.AddDays(7));

            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.accessToken, It.IsAny<CancellationToken>())).ReturnsAsync(accessToken);
            _tokenQueriesMock.Setup(q => q.GetByValueAsync(command.refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify State Change: C? 2 token ph?i chuy?n sang IsRevoked = true
            Assert.True(accessToken.IsRevoked, "L?I DATA: Access Token chua du?c dánh d?u Revoked.");
            Assert.True(refreshToken.IsRevoked, "L?I DATA: Refresh Token chua du?c dánh d?u Revoked.");

            // Verify Side Effect: Ph?i Commit xu?ng DB
            _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once, "L?I DATA: Quên g?i Commit d? luu tr?ng thái.");
        }

        // Helper
        private ClaimsPrincipal CreatePrincipal(Guid accountId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString())
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
    }
}
