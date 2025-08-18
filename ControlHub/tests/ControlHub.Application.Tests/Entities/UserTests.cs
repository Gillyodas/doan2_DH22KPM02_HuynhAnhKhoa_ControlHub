using System;
using ControlHub.Domain.Entities;
using Xunit;

namespace ControlHub.Domain.Tests.Entities
{
    public class UserTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            var id = Guid.NewGuid();
            var accId = Guid.NewGuid();

            var user = new User(id, accId, "john");

            Assert.Equal(id, user.Id);
            Assert.Equal(accId, user.AccId);
            Assert.Equal("john", user.Username);
            Assert.False(user.IsDeleted);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenIdIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new User(Guid.Empty, Guid.NewGuid(), "john"));
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenAccountIdIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), Guid.Empty, "john"));
        }

        [Fact]
        public void Delete_ShouldSetIsDeletedTrue()
        {
            var user = new User(Guid.NewGuid(), Guid.NewGuid(), "john");

            user.Delete();

            Assert.True(user.IsDeleted);
        }

        [Fact]
        public void SetUsername_ShouldUpdateUsername_WhenValid()
        {
            var user = new User(Guid.NewGuid(), Guid.NewGuid());

            user.SetUsername("newname");

            Assert.Equal("newname", user.Username);
        }

        [Fact]
        public void SetUsername_ShouldThrow_WhenEmpty()
        {
            var user = new User(Guid.NewGuid(), Guid.NewGuid());

            Assert.Throws<ArgumentException>(() => user.SetUsername("  "));
        }
    }
}
