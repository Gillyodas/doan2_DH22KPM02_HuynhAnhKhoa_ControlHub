using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;

namespace ControlHub.Domain.Identity.Entities
{
    public class User
    {
        // Properties
        public Guid Id { get; private set; }
        public string? Username { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? PhoneNumber { get; private set; }
        public bool IsDeleted { get; private set; }

        // Foreign Key & Navigation
        public Guid AccId { get; private set; }

        // Navigation property v? Account (Aggregate Root cha n?u User n?m trong Account Aggregate)
        // Ho?c ch? là reference n?u User là Aggregate riêng bi?t (tùy thi?t k? c?a b?n)
        // ? dây tôi khai báo nó, nhung EF Core s? set nó.
        // public Account Account { get; private set; } = null!; 

        // Constructor r?ng cho EF Core
        private User() { }

        public User(Guid id, Guid accId, string? username = null, string? firstName = null, string? lastName = null, string? phoneNumber = null)
        {
            if (id == Guid.Empty) throw new ArgumentException("User Id is required", nameof(id));
            if (accId == Guid.Empty) throw new ArgumentException("Account Id is required", nameof(accId));

            Id = id;
            AccId = accId;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            IsDeleted = false;
        }

        // Behavior
        public void Delete() => IsDeleted = true;

        public void SetUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));
            Username = username;
        }

        public Result UpdateUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Result.Failure(UserErrors.Required);

            Username = username;

            return Result.Success();
        }

        public void UpdateProfile(string? firstName, string? lastName, string? phoneNumber)
        {
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
        }
    }
}
