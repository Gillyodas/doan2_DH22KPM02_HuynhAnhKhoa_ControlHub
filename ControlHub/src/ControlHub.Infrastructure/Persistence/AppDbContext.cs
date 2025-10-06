using ControlHub.Infrastructure.Accounts;
using ControlHub.Infrastructure.Outboxs;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AccountEntity> Accounts { get; set; }
        public DbSet<AccountIdentifierEntity> AccountIdentifiers { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<OutboxMessageEntity> OutboxMessages { get; set; }
        public DbSet<TokenEntity> Tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AccountConfig());
            modelBuilder.ApplyConfiguration(new AccountIdentifierConfig());
            modelBuilder.ApplyConfiguration(new UserConfig());
            modelBuilder.ApplyConfiguration(new OutboxMessageConfig());
            modelBuilder.ApplyConfiguration(new TokenConfig());

            base.OnModelCreating(modelBuilder);
        }
    }
}
