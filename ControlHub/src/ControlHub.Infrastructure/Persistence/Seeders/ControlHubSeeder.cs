using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Persistence.Seeders
{
    public static class ControlHubSeeder
    {
        public static async Task SeedAsync(AppDbContext db, bool forceSeed = false)
        {
            if (!forceSeed && await HasExistingDataAsync(db))
            {
                return;
            }

            Console.WriteLine("Seeding database...");

            var existingAccounts = await db.Accounts.ToListAsync();
            var existingRoles = await db.Roles.ToListAsync();
            var existingPermissions = await db.Permissions.ToListAsync();

            if (existingAccounts.Any() || forceSeed)
            {
                db.Accounts.RemoveRange(existingAccounts);
                db.Roles.RemoveRange(existingRoles);
                db.Permissions.RemoveRange(existingPermissions);
                await db.SaveChangesAsync();
            }

            await TestDataProvider.SeedTestAccountsAsync(db, includeExtended: false, forceSeed: forceSeed);

            Console.WriteLine("Database seeding completed successfully.");
        }

        /// <summary>
        /// Checks if the database already contains essential data
        /// </summary>
        private static async Task<bool> HasExistingDataAsync(AppDbContext db)
        {
            // Check if any of the core tables have data
            var hasRoles = await db.Roles.AnyAsync();
            var hasAccounts = await db.Accounts.AnyAsync();
            var hasIdentifierConfigs = await db.IdentifierConfigs.AnyAsync();

            return hasRoles || hasAccounts || hasIdentifierConfigs;
        }
    }
}