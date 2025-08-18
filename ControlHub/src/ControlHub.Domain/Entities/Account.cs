namespace ControlHub.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public byte[] HashPassword { get; private set; }
        public byte[] Salt { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }
        public Guid UserId { get; private set; }

        // Navigation property (1-1)
        public User User { get; private set; }

        // Constructor
        public Account(Guid id, string email, byte[] hashPassword, byte[] salt, Guid userId)
        {
            Id = id;
            Email = email;
            HashPassword = hashPassword;
            Salt = salt;
            UserId = userId;
            IsActive = true;
            IsDeleted = false;
        }

        // Domain behaviors
        public void Deactivate() => IsActive = false;
        public void Delete() => IsDeleted = true;
    }
}
