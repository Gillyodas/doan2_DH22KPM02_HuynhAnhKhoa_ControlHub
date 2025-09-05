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

        private User? _user;
        public User? User => _user;

        public Account(Guid id, Email email, byte[] hashPassword, byte[] salt)
        {
            if (id == Guid.Empty) throw new ArgumentException("Id is required", nameof(id));

            Id = id;
            Email = email;
            HashPassword = hashPassword;
            Salt = salt;
            IsActive = true;
            IsDeleted = false;
        }

        // For persistence only
        private Account(Guid id, Email email, byte[] hash, byte[] salt, bool isActive, bool isDeleted, User? user)
        {
            Id = id;
            Email = email;
            HashPassword = hash;
            Salt = salt;
            IsActive = isActive;
            IsDeleted = isDeleted;
            _user = user;
        }

        public static Account Rehydrate(Guid id, Email email, byte[] hash, byte[] salt, bool isActive, bool isDeleted, User? user)
            => new Account(id, email, hash, salt, isActive, isDeleted, user);

        // Behaviors
        public Result AttachUser(User user)
        {
            if (user == null)
                return Result.Failure("User cannot be null.");

            if (_user != null)
                return Result.Failure("User already attached.");

            _user = user;
            return Result.Success();
        }
        public void Deactivate() => IsActive = false;   
        public void Delete()
        {
            IsDeleted = true;
            _user?.Delete(); // đảm bảo consistency: User đi theo
        }
    }
}