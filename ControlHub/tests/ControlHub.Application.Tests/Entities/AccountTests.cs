using System;
using ControlHub.Domain.Entities;
using Xunit;

namespace ControlHub.Domain.Tests.Entities
{
    public class AccountTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Arrange
            var id = Guid.NewGuid();
            var email = "test@example.com";
            var hash = new byte[] { 1, 2, 3 };
            var salt = new byte[] { 4, 5, 6 };
            var userId = Guid.NewGuid();

            // Act
            var account = new Account(id, email, hash, salt, userId);

            // Assert
            Assert.Equal(id, account.Id);
            Assert.Equal(email, account.Email);
            Assert.Equal(hash, account.HashPassword);
            Assert.Equal(salt, account.Salt);
            Assert.Equal(userId, account.UserId);
            Assert.True(account.IsActive);
            Assert.False(account.IsDeleted);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalse()
        {
            var account = new Account(Guid.NewGuid(), "a@b.com", new byte[1], new byte[1], Guid.NewGuid());

            account.Deactivate();

            Assert.False(account.IsActive);
        }

        [Fact]
        public void Delete_ShouldSetIsDeletedTrue()
        {
            var account = new Account(Guid.NewGuid(), "a@b.com", new byte[1], new byte[1], Guid.NewGuid());

            account.Delete();

            Assert.True(account.IsDeleted);
        }
    }
}
