using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Accounts
{
    public class Account
    {
        public Guid Id { get; private set; }
        public Email Email { get; private set; }
        public byte[] HashPassword { get; private set; }
        public byte[] Salt { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }

        public Maybe<User> User { get; private set; }

        public Account(Guid id, Email email, byte[] hashPassword, byte[] salt, bool isActive, bool isDeleted, Maybe<User> user)
        {
            if (id == Guid.Empty) throw new ArgumentException("Id is required", nameof(id));

            Id = id;
            Email = email;
            HashPassword = hashPassword;
            Salt = salt;
            IsActive = isActive;
            IsDeleted = isDeleted;
            User = user;
        }

        // Factory khi khởi tạo mới account
        public static Account Create(Guid id, Email email, byte[] hashPassword, byte[] salt)
            => new Account(id, email, hashPassword, salt, true, false, Maybe<User>.None);

        // Factory cho persistence
        public static Account Rehydrate(Guid id, Email email, byte[] hash, byte[] salt, bool isActive, bool isDeleted, Maybe<User> user)
            => new Account(id, email, hash, salt, isActive, isDeleted, user);

        // Behaviors
        public Result AttachUser(User user)
        {
            if (user == null)
                return Result.Failure("User cannot be null.");

            if (User.HasValue)
                return Result.Failure("User already attached.");

            User = Maybe<User>.From(user);
            return Result.Success();
        }

        public void Deactivate() => IsActive = false;

        public void Delete()
        {
            IsDeleted = true;
            User.Match(
                some: u => u.Delete(),
                none: () => { }
            );
        }
    }
}