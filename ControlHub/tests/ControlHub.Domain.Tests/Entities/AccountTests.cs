using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;

namespace ControlHub.Domain.Tests.Entities
{
    public class AccountTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Arrange
            var id = Guid.NewGuid();
            var email = Email.Create("test@example.com").Value;
            var hash = new byte[] { 1, 2, 3 };
            var salt = new byte[] { 4, 5, 6 };

            // Act
            var account = new Account(id, email, hash, salt);

            // Assert
            Assert.Equal(id, account.Id);
            Assert.Equal(email, account.Email);
            Assert.Equal(hash, account.HashPassword);
            Assert.Equal(salt, account.Salt);
            Assert.True(account.IsActive);
            Assert.False(account.IsDeleted);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalse()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), Email.Create("a@b.com").Value, new byte[1], new byte[1]);

            // Act
            account.Deactivate();

            // Assert
            Assert.False(account.IsActive);
        }

        [Fact]
        public void Delete_ShouldSetIsDeletedTrue()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), Email.Create("a@b.com").Value, new byte[1], new byte[1]);

            // Act
            account.Delete();

            // Assert
            Assert.True(account.IsDeleted);
        }

        [Fact]
        public void Delete_ShouldAlsoDeleteUser_WhenUserExists()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), Guid.NewGuid(), "testuser");
            var account = Account.Rehydrate(Guid.NewGuid(), Email.Create("a@b.com").Value, new byte[1], new byte[1], true, false, user);

            // Act
            account.Delete();

            // Assert
            Assert.True(account.IsDeleted);
            Assert.True(account.User!.IsDeleted); // User đi theo account
        }
    }
}
