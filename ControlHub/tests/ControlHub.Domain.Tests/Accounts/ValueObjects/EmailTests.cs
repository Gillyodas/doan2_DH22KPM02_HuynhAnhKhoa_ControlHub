using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;

namespace ControlHub.Domain.Tests.Accounts.ValueObjects
{
    public class EmailTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_ShouldFail_WhenEmailIsNullOrEmpty(string? email)
        {
            var result = Email.Create(email!);
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.EmailRequired, result.Error);
        }

        [Theory]
        // Case l?i d?nh d?ng
        [InlineData("plainaddress")]
        [InlineData("#@%^%#$@#$@#.com")]
        [InlineData("@example.com")]
        [InlineData("Joe Smith <email@example.com>")]
        [InlineData("email.example.com")]
        [InlineData("email@example@example.com")]
        [InlineData(".email@example.com")]
        [InlineData("email.@example.com")]
        [InlineData("email..email@example.com")]
        // Case l?i domain
        [InlineData("email@example.c")] // TLD quá ng?n (< 2 ký t?)
        [InlineData("email@example.123")] // TLD không ph?i ch? cái
        public void Create_ShouldFail_WhenEmailFormatIsInvalid(string invalidEmail)
        {
            var result = Email.Create(invalidEmail);

            Assert.True(result.IsFailure, $"BUG: Regex ch?p nh?n email sai d?nh d?ng: '{invalidEmail}'");
            Assert.Equal(AccountErrors.InvalidEmail, result.Error);
        }

        [Fact]
        public void Create_ShouldSucceed_WhenEmailIsValid()
        {
            var result = Email.Create("test.user+tag@sub.domain.com");

            Assert.True(result.IsSuccess);
            Assert.Equal("test.user+tag@sub.domain.com", result.Value.Value);
        }

        [Fact]
        public void Equality_ShouldWorkCorrectly()
        {
            var email1 = Email.Create("test@test.com").Value;
            var email2 = Email.Create("test@test.com").Value;
            var email3 = Email.Create("other@test.com").Value;

            Assert.Equal(email1, email2);
            Assert.NotEqual(email1, email3);
            // Case Insensitive check (n?u có trong yêu c?u, hi?n t?i regex c?a b?n có IgnoreCase)
            // Assert.Equal(Email.Create("TEST@TEST.COM").Value, email1); 
        }
    }
}
