using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers;
using ControlHub.Domain.Accounts.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Roles;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Persistence.Seeders
{
    public static class ControlHubSeeder
    {
        public static async Task SeedAsync(AppDbContext db, bool forceSeed = false)
        {
            // Check if database already has data
            var hasExistingData = await HasExistingDataAsync(db);
            
            if (hasExistingData && !forceSeed)
            {
                Console.WriteLine("Database already contains data. Use forceSeed=true to override.");
                return;
            }
            
            if (hasExistingData && forceSeed)
            {
                Console.WriteLine("Database contains data but forceSeed=true. Clearing and reseeding...");
            }
            
            // Seed Roles
            if (!await db.Roles.AnyAsync())
            {
                var superAdmin = Role.Create(
                    ControlHubDefaults.Roles.SuperAdminId, // Dùng ID cố định
                    ControlHubDefaults.Roles.SuperAdminName,
                    "System Super Admin");

                var admin = Role.Create(
                    ControlHubDefaults.Roles.AdminId, // Dùng ID cố định
                    ControlHubDefaults.Roles.AdminName,
                    "System Admin");

                var user = Role.Create(
                    ControlHubDefaults.Roles.UserId, // Dùng ID cố định
                    ControlHubDefaults.Roles.UserName,
                    "Standard User");

                await db.Roles.AddRangeAsync(superAdmin, admin, user);
                await db.SaveChangesAsync();
            }

            // Seed IdentifierConfigs using TestDataProvider
            await TestDataProvider.SeedTestIdentifierConfigsAsync(db, forceSeed);

            // Seed Permissions and assign to roles
            await TestDataProvider.SeedPermissionsAndRolesAsync(db, forceSeed);

            // Seed Test Accounts using TestDataProvider (clear and reseed for testing)
            var existingAccounts = await db.Accounts.ToListAsync();
            if (existingAccounts.Any())
            {
                db.Accounts.RemoveRange(existingAccounts);
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