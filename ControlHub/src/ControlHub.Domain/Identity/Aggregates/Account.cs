using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.Domain.Roles;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Identity.Entities;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;

namespace ControlHub.Domain.Identity.Aggregates
{
    public class Account
    {
        public Guid Id { get; private set; }

        // Value Object: Password (s? du?c map ph?ng vào b?ng Accounts)
        public Password Password { get; private set; } = default!;

        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }

        // Foreign Key
        public Guid RoleId { get; private set; }

        // Navigation Properties (EF Core c?n ki?u c? th?, không dùng Maybe<T> ? dây)
        public Role? Role { get; private set; }
        public User? User { get; private set; }

        // Collections (EF Core s? map vào field private)
        private readonly List<Identifier> _identifiers = new();
        public IReadOnlyCollection<Identifier> Identifiers => _identifiers.AsReadOnly();

        private readonly List<Token> _tokens = new();
        public IReadOnlyCollection<Token> Tokens => _tokens.AsReadOnly();

        // Constructor cho EF Core
        private Account() { }

        private Account(Guid id, Password pass, Guid roleId, bool isActive, bool isDeleted)
        {
            if (id == Guid.Empty) throw new ArgumentException("Id is required", nameof(id));
            if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required", nameof(roleId));

            Id = id;
            Password = pass ?? throw new ArgumentNullException(nameof(pass));
            RoleId = roleId;
            IsActive = isActive;
            IsDeleted = isDeleted;
        }

        // Factory
        public static Account Create(Guid id, Password pass, Guid roleId)
            => new Account(id, pass, roleId, true, false);

        // Behaviors
        public Result AddIdentifier(Identifier identifier)
        {
            if (_identifiers.Any(i => i.Type == identifier.Type && i.NormalizedValue == identifier.NormalizedValue))
                return Result.Failure(AccountErrors.IdentifierAlreadyExists);

            _identifiers.Add(identifier);
            return Result.Success();
        }

        public Result RemoveIdentifier(IdentifierType type, string normalized)
        {
            var found = _identifiers.FirstOrDefault(i => i.Type == type && i.NormalizedValue == normalized);
            if (found == null) return Result.Failure(AccountErrors.IdentifierNotFound);

            _identifiers.Remove(found);
            return Result.Success();
        }

        public Result AttachUser(User user)
        {
            if (user == null) return Result.Failure(UserErrors.Required);
            if (User != null) return Result.Failure(UserErrors.AlreadyAtached);

            User = user;
            return Result.Success();
        }

        public Result AttachRole(Role role)
        {
            if (role == null) return Result.Failure(AccountErrors.RoleRequired);
            Role = role;
            RoleId = role.Id;
            return Result.Success();
        }

        // Qu?n lý Token ngay trong Account (Aggregate Root)
        public void AddToken(Token token)
        {
            _tokens.Add(token);
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;

        public void Delete()
        {
            IsDeleted = true;
            User?.Delete();

            foreach (var token in _tokens)
            {
                token.Revoke();
            }

            foreach (var ident in _identifiers)
            {
                ident.Delete();
            }
        }

        public Result UpdatePassword(Password newPass)
        {
            if (newPass is null) return Result.Failure(AccountErrors.PasswordRequired);
            if (!newPass.IsValid()) return Result.Failure(AccountErrors.PasswordIsNotValid);

            Password = newPass;
            return Result.Success();
        }
    }
}
