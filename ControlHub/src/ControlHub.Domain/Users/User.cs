namespace ControlHub.Domain.Users
{
    public class User
    {
        public Guid Id { get; private set; }
        public string? Username { get; private set; }
        public bool IsDeleted { get; private set; }
        public Guid AccId { get; private set; }

        public User(Guid id, Guid accId, string? username = null)
        {
            if (id == Guid.Empty) throw new ArgumentException("User Id is required", nameof(id));
            if (accId == Guid.Empty) throw new ArgumentException("Account Id is required", nameof(accId));

            Id = id;
            AccId = accId;
            Username = username;
            IsDeleted = false;
        }

        public void Delete() => IsDeleted = true;

        public void SetUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));
            Username = username;
        }
    }
}