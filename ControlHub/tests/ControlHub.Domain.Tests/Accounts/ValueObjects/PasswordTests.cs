using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using Moq;

namespace ControlHub.Domain.Tests.Accounts.ValueObjects
{
    public class PasswordTests
    {
        private readonly Mock<IPasswordHasher> _hasherMock = new();

        public PasswordTests()
        {
            // Setup m?c d?nh: Hasher tr? v? hash h?p l? (d? test logic validation c?a Domain)
            _hasherMock.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns(Password.From(new byte[32], new byte[16])); // 32 bytes hash, 16 bytes salt
        }

        // --- NHÓM 1: BUG HUNTING - Ð? M?NH M?T KH?U (COMPLEXITY) ---

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_ShouldFail_WhenPasswordIsNuLLOrEmpty(string? input)
        {
            var result = Password.Create(input!, _hasherMock.Object);

            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.PasswordIsWeak, result.Error);
        }

        [Theory]
        [InlineData("1234567")]           // < 8 ký t?
        [InlineData("Abcdef1")]           // Thi?u ký t? d?c bi?t
        [InlineData("abcdef1@")]          // Thi?u ch? Hoa
        [InlineData("ABCDEF1@")]          // Thi?u ch? thu?ng
        [InlineData("Abcdefgh@")]         // Thi?u s?
        public void Create_ShouldFail_WhenPasswordIsWeak(string weakPass)
        {
            // Act
            var result = Password.Create(weakPass, _hasherMock.Object);

            // Assert
            Assert.True(result.IsFailure, $"BUG: Domain ch?p nh?n m?t kh?u y?u: '{weakPass}'");
            Assert.Equal(AccountErrors.PasswordIsWeak, result.Error);
        }

        [Fact]
        public void Create_ShouldSucceed_WhenPasswordIsStrong()
        {
            // Arrange: Ð? 8 ký t?, Hoa, Thu?ng, S?, Ð?c bi?t
            var strongPass = "StrongP@ss1";

            // Act
            var result = Password.Create(strongPass, _hasherMock.Object);

            // Assert
            Assert.True(result.IsSuccess);
        }

        // --- NHÓM 2: BUG HUNTING - INTEGRITY (TOÀN V?N D? LI?U HASH) ---

        [Fact]
        public void Create_ShouldFail_WhenHasherReturnsInvalidHashLength()
        {
            // BUG HUNT: Gi? s? Hasher b? l?i, tr? v? m?ng byte r?ng ho?c quá ng?n.
            // Domain Password ph?i ch?n l?i d? không luu rác vào DB.

            // Arrange
            _hasherMock.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns(Password.From(new byte[0], new byte[16])); // Hash r?ng (Invalid)

            // Act
            var result = Password.Create("ValidP@ss1", _hasherMock.Object);

            // Assert
            Assert.True(result.IsFailure, "BUG: Domain ch?p nh?n Hash r?ng (Length=0).");
            Assert.Equal(AccountErrors.PasswordHashFailed, result.Error);
        }

        [Fact]
        public void Create_ShouldFail_WhenHasherReturnsInvalidSaltLength()
        {
            // Arrange
            _hasherMock.Setup(x => x.Hash(It.IsAny<string>()))
                .Returns(Password.From(new byte[32], new byte[5])); // Salt quá ng?n (< 16 bytes)

            // Act
            var result = Password.Create("ValidP@ss1", _hasherMock.Object);

            // Assert
            Assert.True(result.IsFailure, "BUG: Domain ch?p nh?n Salt quá ng?n.");
            Assert.Equal(AccountErrors.PasswordHashFailed, result.Error);
        }
    }
}
