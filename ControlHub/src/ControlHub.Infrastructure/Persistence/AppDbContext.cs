using ControlHub.Infrastructure.Accounts;
using ControlHub.Infrastructure.Users;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Outboxs;
using ControlHub.Infrastructure.Roles;
using ControlHub.Infrastructure.Permissions;
using ControlHub.Infrastructure.AccountRoles;
using ControlHub.Infrastructure.RolePermissions;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Aggregate Roots
        public DbSet<AccountEntity> Accounts { get; set; } = default!;
        public DbSet<RoleEntity> Roles { get; set; } = default!;
        public DbSet<PermissionEntity> Permissions { get; set; } = default!;

        // Entities thuộc Aggregate
        public DbSet<UserEntity> Users { get; set; } = default!;
        public DbSet<TokenEntity> Tokens { get; set; } = default!;
        public DbSet<AccountIdentifierEntity> AccountIdentifiers { get; set; } = default!;
        public DbSet<OutboxMessageEntity> OutboxMessages { get; set; } = default!;

        // Join Entities
        public DbSet<AccountRoleEntity> AccountRoles { get; set; } = default!;
        public DbSet<RolePermissionEntity> RolePermissions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Load toàn bộ configuration trong Infrastructure assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}