using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Users; // Ch?a UserErrors

namespace ControlHub.Domain.Tests.Accounts
{
    public class AccountTests
    {
        // Helper t?o Valid Password
        private static Password CreateValidPassword() => Password.From(new byte[32], new byte[16]);

        // --- NHÓM 1: BUG HUNTING - FACTORY VALIDATION ---

        [Fact]
        public void Create_ShouldThrowException_WhenIdIsEmpty()
        {
            // BUG HUNT: Không du?c phép t?o Account v?i Guid.Empty
            Assert.Throws<ArgumentException>(() =>
                Account.Create(Guid.Empty, CreateValidPassword(), Guid.NewGuid()));
        }

        [Fact]
        public void Create_ShouldThrowException_WhenRoleIdIsEmpty()
        {
            // BUG HUNT: Account ph?i luôn thu?c v? m?t Role
            Assert.Throws<ArgumentException>(() =>
                Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.Empty));
        }

        [Fact]
        public void Create_ShouldThrowException_WhenPasswordIsNull()
        {
            // BUG HUNT: Password không du?c null
            Assert.Throws<ArgumentNullException>(() =>
                Account.Create(Guid.NewGuid(), null!, Guid.NewGuid()));
        }

        // --- NHÓM 2: BUG HUNTING - IDENTIFIER LOGIC ---

        [Fact]
        public void AddIdentifier_ShouldFail_WhenIdentifierAlreadyExists()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.NewGuid());
            var identifier = Identifier.Create(IdentifierType.Email, "test@test.com", "test@test.com");

            // Add l?n 1 -> Success
            account.AddIdentifier(identifier);

            // Act: Add l?n 2 (trùng)
            var result = account.AddIdentifier(identifier);

            // Assert
            Assert.True(result.IsFailure, "BUG: Domain cho phép add trùng Identifier.");
            Assert.Equal(AccountErrors.IdentifierAlreadyExists, result.Error);
        }

        [Fact]
        public void RemoveIdentifier_ShouldFail_WhenIdentifierNotFound()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.NewGuid());

            // Act
            var result = account.RemoveIdentifier(IdentifierType.Email, "notfound@test.com");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AccountErrors.IdentifierNotFound, result.Error);
        }

        // --- NHÓM 3: BUG HUNTING - RELATIONSHIP LOGIC ---

        [Fact]
        public void AttachUser_ShouldFail_WhenUserIsNull()
        {
            var account = Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.NewGuid());

            var result = account.AttachUser(null!);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.Required, result.Error);
        }

        [Fact]
        public void AttachUser_ShouldFail_WhenUserAlreadyAttached()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.NewGuid());
            var user1 = new User(Guid.NewGuid(), account.Id, "User1");
            var user2 = new User(Guid.NewGuid(), account.Id, "User2");

            account.AttachUser(user1);

            // Act: C? g?n thêm user th? 2 vào cùng 1 account (1-1 violation)
            var result = account.AttachUser(user2);

            // Assert
            Assert.True(result.IsFailure, "BUG: Domain cho phép 1 Account g?n v?i nhi?u User (Vi ph?m 1-1).");
            Assert.Equal(UserErrors.AlreadyAtached, result.Error);
        }

        // --- NHÓM 4: BUG HUNTING - UPDATE PASSWORD ---

        [Fact]
        public void UpdatePassword_ShouldFail_WhenNewPasswordIsInvalid()
        {
            // Arrange
            var account = Account.Create(Guid.NewGuid(), CreateValidPassword(), Guid.NewGuid());

            // Gi? l?p m?t password object b? l?i (Hash length = 0)
            // Ðây là case "hack" vào h? th?ng type safety
            var invalidPass = Password.From(new byte[0], new byte[16]);

            // Act
            var result = account.UpdatePassword(invalidPass);

            // Assert
            Assert.True(result.IsFailure, "BUG: UpdatePassword không check tính h?p l? (IsValid) c?a password object m?i.");
            Assert.Equal(AccountErrors.PasswordIsNotValid, result.Error);
        }
    }
}
