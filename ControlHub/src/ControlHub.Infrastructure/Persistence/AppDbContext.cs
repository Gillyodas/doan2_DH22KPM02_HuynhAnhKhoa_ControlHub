using ControlHub.Domain.Accounts;
using ControlHub.Domain.Outboxs;
using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Users;
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
        public DbSet<Account> Accounts { get; set; } = default!;
        public DbSet<Role> Roles { get; set; } = default!;
        public DbSet<Permission> Permissions { get; set; } = default!;

        // Entities thuộc Aggregate
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Token> Tokens { get; set; } = default!;
        //TODO: Add OutboxMessage to DbContext + sử dụng virtual DbSet để hỗ trợ testing
        public virtual DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;

        // Join Entities
        public DbSet<RolePermissionEntity> RolePermissions { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ControlHub");
            // Load toàn bộ configuration trong Infrastructure assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}