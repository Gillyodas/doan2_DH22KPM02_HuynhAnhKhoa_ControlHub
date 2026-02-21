using ControlHub.Application.Messaging.Outbox;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Domain.AccessControl.Entities;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Domain.Identity.Identifiers;
using ControlHub.Domain.TokenManagement.Aggregates;
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

        // Entities thu?c Aggregate
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Token> Tokens { get; set; } = default!;
        //TODO: Add OutboxMessage to DbContext + s? d?ng virtual DbSet d? h? tr? testing
        public virtual DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;

        // Join Entities
        public DbSet<RolePermissionEntity> RolePermissions { get; set; } = default!;

        public DbSet<IdentifierConfig> IdentifierConfigs { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ControlHub");
            // Load toàn b? configuration trong Infrastructure assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
