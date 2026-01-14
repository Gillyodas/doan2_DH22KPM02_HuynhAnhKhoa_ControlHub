using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers;
using ControlHub.Domain.Accounts.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Roles;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Constants;
using ControlHub.Domain.Permissions;
using ControlHub.Infrastructure.Accounts.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Helper class for creating test data for development and testing
    /// </summary>
    public static class TestDataProvider
    {
        /// <summary>
        /// Test accounts with their credentials
        /// </summary>
        public static readonly List<TestAccount> TestAccounts = new()
        {
            new TestAccount
            {
                Role = "SuperAdmin",
                IdentifierType = IdentifierType.Email,
                IdentifierConfigName = "Email",
                IdentifierValue = "gillyodaswork@gmail.com",
                Password = "Admin@123",
                UserName = "Super Admin"
            },
            new TestAccount
            {
                Role = "Admin",
                IdentifierType = IdentifierType.Username,
                IdentifierConfigName = "Username",
                IdentifierValue = "admin123",
                Password = "Admin@123",
                UserName = "Admin User"
            },
            new TestAccount
            {
                Role = "User",
                IdentifierType = IdentifierType.Username,
                IdentifierConfigName = "EmployeeID",
                IdentifierValue = "EMP00001",
                Password = "Admin@123",
                UserName = "John Doe"
            },
            new TestAccount
            {
                Role = "User",
                IdentifierType = IdentifierType.Phone,
                IdentifierConfigName = "Phone",
                IdentifierValue = "+84123456789",
                Password = "Admin@123",
                UserName = "Jane Smith"
            }
        };

        /// <summary>
        /// Additional test accounts for extended testing
        /// </summary>
        public static readonly List<TestAccount> ExtendedTestAccounts = new()
        {
            new TestAccount
            {
                Role = "User",
                IdentifierType = IdentifierType.Username,
                IdentifierConfigName = "Username",
                IdentifierValue = "testuser",
                Password = "Admin@123",
                UserName = "Test User"
            },
            new TestAccount
            {
                Role = "User",
                IdentifierType = IdentifierType.Email,
                IdentifierConfigName = "Email",
                IdentifierValue = "test@example.com",
                Password = "Admin@123",
                UserName = "Test Email User"
            },
            new TestAccount
            {
                Role = "Admin",
                IdentifierType = IdentifierType.Username,
                IdentifierConfigName = "Username",
                IdentifierValue = "testadmin",
                Password = "Admin@123",
                UserName = "Test Admin"
            }
        };

        /// <summary>
        /// Creates test accounts and adds them to the database
        /// </summary>
        public static async Task SeedTestAccountsAsync(AppDbContext db, bool includeExtended = false, bool forceSeed = false)
        {
            // Check if accounts already exist
            var hasExistingAccounts = await db.Accounts.AnyAsync();
            
            if (hasExistingAccounts && !forceSeed)
            {
                Console.WriteLine("Test accounts already exist. Use forceSeed=true to override.");
                return;
            }
            
            if (hasExistingAccounts && forceSeed)
            {
                Console.WriteLine("Test accounts exist but forceSeed=true. Clearing and reseeding...");
                // Clear existing accounts
                var existingAccounts = await db.Accounts.ToListAsync();
                db.Accounts.RemoveRange(existingAccounts);
                await db.SaveChangesAsync();
            }
            
            var passwordHasher = new TestPasswordHasher();
            var accounts = new List<Account>();

            foreach (var testAccount in TestAccounts)
            {
                var account = CreateTestAccount(testAccount, passwordHasher);
                accounts.Add(account);
            }

            if (includeExtended)
            {
                foreach (var testAccount in ExtendedTestAccounts)
                {
                    var account = CreateTestAccount(testAccount, passwordHasher);
                    accounts.Add(account);
                }
            }

            await db.Accounts.AddRangeAsync(accounts);
            await db.SaveChangesAsync();
            
            Console.WriteLine($"Seeded {accounts.Count} test accounts successfully.");
        }

        /// <summary>
        /// Seeds permissions and assigns them to roles
        /// </summary>
        public static async Task SeedPermissionsAndRolesAsync(AppDbContext db, bool forceSeed = false)
        {
            // Seed Permissions
            var hasExistingPermissions = await db.Permissions.AnyAsync();
            
            if (hasExistingPermissions && !forceSeed)
            {
                Console.WriteLine("Permissions already exist. Use forceSeed=true to override.");
            }
            else if (hasExistingPermissions && forceSeed)
            {
                Console.WriteLine("Permissions exist but forceSeed=true. Clearing and reseeding...");
                // Clear existing permissions
                var existingPermissions = await db.Permissions.ToListAsync();
                db.Permissions.RemoveRange(existingPermissions);
                await db.SaveChangesAsync();
            }
            
            if (!hasExistingPermissions || forceSeed)
            {
                var allPermissions = new[]
                {
                    // Authentication Permissions
                    "auth.signin", "auth.register", "auth.refresh",
                    "auth.change_password", "auth.forgot_password", "auth.reset_password",

                    // User Management Permissions
                    "users.view", "users.create", "users.update",
                    "users.delete", "users.update_username",

                    // Role Management Permissions
                    "roles.view", "roles.create", "roles.update",
                    "roles.delete", "roles.assign",

                    // Identifier Configuration Permissions
                    "identifiers.view", "identifiers.create",
                    "identifiers.update", "identifiers.delete",
                    "identifiers.toggle",

                    // System Administration Permissions
                    "system.view_logs", "system.view_metrics",
                    "system.manage_settings", "system.view_audit",

                    // Profile Permissions
                    "profile.view_own", "profile.update_own",

                    // Permission Management Permissions
                    "permissions.view", "permissions.create",
                    "permissions.update", "permissions.delete", "permissions.assign"
                };

                var permissionEntities = allPermissions.Select(p => 
                {
                    var result = Domain.Permissions.Permission.Create(Guid.NewGuid(), p, GetPermissionDescription(p));
                    if (result.IsFailure)
                        throw new InvalidOperationException($"Failed to create permission {p}: {result.Error}");
                    return result.Value;
                });

                await db.Permissions.AddRangeAsync(permissionEntities);
                await db.SaveChangesAsync();
                
                Console.WriteLine($"Seeded {permissionEntities.Count()} permissions successfully.");
            }

            // Assign permissions to roles
            await AssignPermissionsToRolesAsync(db);
        }

        /// <summary>
        /// Assigns permissions to default roles
        /// </summary>
        private static async Task AssignPermissionsToRolesAsync(AppDbContext db)
        {
            var roles = await db.Roles.Include(r => r.Permissions).ToListAsync();
            var allPermissions = await db.Permissions.ToListAsync();

            foreach (var role in roles)
            {
                var permissionStrings = role.Name switch
                {
                    "SuperAdmin" => allPermissions.Select(p => p.Code), // SuperAdmin gets all permissions
                    "Admin" => allPermissions.Where(p => 
                        !p.Code.StartsWith("permissions") && // Admin cannot manage permissions
                        !p.Code.Contains("system")).Select(p => p.Code), // Admin cannot access system admin features
                    "User" => allPermissions.Where(p => 
                        p.Code.StartsWith("profile")).Select(p => p.Code), // Users only get profile permissions
                    _ => Enumerable.Empty<string>()
                };

                var permissionsToAdd = allPermissions
                    .Where(p => permissionStrings.Contains(p.Code));

                foreach (var permission in permissionsToAdd)
                {
                    role.AddPermission(permission);
                }
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets description for a permission
        /// </summary>
        private static string GetPermissionDescription(string permission)
        {
            return permission switch
            {
                "auth.signin" => "Allows users to sign in",
                "auth.register" => "Allows users to register new accounts",
                "auth.refresh" => "Allows users to refresh access tokens",
                "auth.change_password" => "Allows users to change their password",
                "auth.forgot_password" => "Allows users to request password reset",
                "auth.reset_password" => "Allows users to reset forgotten passwords",
                
                "users.view" => "Allows viewing user accounts",
                "users.create" => "Allows creating new user accounts",
                "users.update" => "Allows updating user account information",
                "users.delete" => "Allows deleting user accounts",
                "users.update_username" => "Allows updating user usernames",
                
                "roles.view" => "Allows viewing role definitions",
                "roles.create" => "Allows creating new roles",
                "roles.update" => "Allows updating role definitions",
                "roles.delete" => "Allows deleting roles",
                "roles.assign" => "Allows assigning roles to users",
                
                "identifiers.view" => "Allows viewing identifier configurations",
                "identifiers.create" => "Allows creating identifier configurations",
                "identifiers.update" => "Allows updating identifier configurations",
                "identifiers.delete" => "Allows deleting identifier configurations",
                "identifiers.toggle" => "Allows toggling identifier configuration active status",
                
                "system.view_logs" => "Allows viewing system logs",
                "system.view_metrics" => "Allows viewing system metrics",
                "system.manage_settings" => "Allows managing system settings",
                "system.view_audit" => "Allows viewing audit logs",
                
                "profile.view_own" => "Allows viewing own profile",
                "profile.update_own" => "Allows updating own profile",
                
                "permissions.view" => "Allows viewing permission definitions",
                "permissions.create" => "Allows creating new permissions",
                "permissions.update" => "Allows updating permission definitions",
                "permissions.delete" => "Allows deleting permissions",
                "permissions.assign" => "Allows assigning permissions to roles",
                
                _ => "Permission description not available"
            };
        }

        /// <summary>
        /// Creates a single test account
        /// </summary>
        private static Account CreateTestAccount(TestAccount testAccount, TestPasswordHasher passwordHasher)
        {
            var accountId = Guid.NewGuid();
            var password = passwordHasher.Hash(testAccount.Password);
            
            var roleId = testAccount.Role switch
            {
                "SuperAdmin" => ControlHubDefaults.Roles.SuperAdminId,
                "Admin" => ControlHubDefaults.Roles.AdminId,
                "User" => ControlHubDefaults.Roles.UserId,
                _ => ControlHubDefaults.Roles.UserId
            };

            var account = Account.Create(accountId, password, roleId);
            
            // Normalize the identifier value based on type
            var normalizedValue = testAccount.IdentifierType switch
            {
                IdentifierType.Email => testAccount.IdentifierValue.Trim().ToLowerInvariant(),
                IdentifierType.Username => testAccount.IdentifierValue.Trim(),
                IdentifierType.Phone => testAccount.IdentifierValue.Trim(),
                _ => testAccount.IdentifierValue
            };

            var identifier = Identifier.CreateWithName(
                testAccount.IdentifierType,
                testAccount.IdentifierConfigName,
                testAccount.IdentifierValue,
                normalizedValue);
            
            account.AddIdentifier(identifier);
            
            var user = new User(Guid.NewGuid(), accountId, testAccount.UserName);
            account.AttachUser(user);

            return account;
        }

        /// <summary>
        /// Gets test account by identifier value
        /// </summary>
        public static TestAccount? GetTestAccount(string identifierValue)
        {
            return TestAccounts.FirstOrDefault(x => x.IdentifierValue == identifierValue) ??
                   ExtendedTestAccounts.FirstOrDefault(x => x.IdentifierValue == identifierValue);
        }

        /// <summary>
        /// Gets all test accounts for a specific role
        /// </summary>
        public static List<TestAccount> GetTestAccountsByRole(string role)
        {
            return TestAccounts.Where(x => x.Role == role).ToList();
        }

        /// <summary>
        /// Creates test identifier configs
        /// </summary>
        public static async Task SeedTestIdentifierConfigsAsync(AppDbContext db, bool forceSeed = false)
        {
            var hasExistingConfigs = await db.IdentifierConfigs.AnyAsync();
            
            if (hasExistingConfigs && !forceSeed)
            {
                Console.WriteLine("Identifier configs already exist. Use forceSeed=true to override.");
                return;
            }
            
            if (hasExistingConfigs && forceSeed)
            {
                Console.WriteLine("Identifier configs exist but forceSeed=true. Clearing and reseeding...");
                // Clear existing configs
                var existingConfigs = await db.IdentifierConfigs.ToListAsync();
                db.IdentifierConfigs.RemoveRange(existingConfigs);
                await db.SaveChangesAsync();
            }

            var configs = new List<IdentifierConfig>();

            // Email Config
            var emailConfig = IdentifierConfig.Create("Email", "Email address validation");
            emailConfig.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
            emailConfig.AddRule(ValidationRuleType.Email, new Dictionary<string, object>());
            configs.Add(emailConfig);

            // Phone Config
            var phoneConfig = IdentifierConfig.Create("Phone", "Phone number validation");
            phoneConfig.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
            phoneConfig.AddRule(ValidationRuleType.Phone, new Dictionary<string, object>
            {
                { "pattern", @"^(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$" },
                { "allowInternational", true }
            });
            configs.Add(phoneConfig);

            // Employee ID Config
            var employeeIdConfig = IdentifierConfig.Create("EmployeeID", "Employee ID validation");
            employeeIdConfig.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
            employeeIdConfig.AddRule(ValidationRuleType.MinLength, new Dictionary<string, object> { { "length", 5 } });
            employeeIdConfig.AddRule(ValidationRuleType.MaxLength, new Dictionary<string, object> { { "length", 10 } });
            employeeIdConfig.AddRule(ValidationRuleType.Pattern, new Dictionary<string, object>
            {
                { "pattern", @"^EMP\d{4,9}$" },
                { "options", 0 }
            });
            configs.Add(employeeIdConfig);

            // Username Config
            var usernameConfig = IdentifierConfig.Create("Username", "Username validation");
            usernameConfig.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
            usernameConfig.AddRule(ValidationRuleType.MinLength, new Dictionary<string, object> { { "length", 3 } });
            usernameConfig.AddRule(ValidationRuleType.MaxLength, new Dictionary<string, object> { { "length", 20 } });
            usernameConfig.AddRule(ValidationRuleType.Custom, new Dictionary<string, object> { { "customLogic", "alphanumeric" } });
            configs.Add(usernameConfig);

            // Age Config
            var ageConfig = IdentifierConfig.Create("Age", "Age validation");
            ageConfig.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
            ageConfig.AddRule(ValidationRuleType.Range, new Dictionary<string, object>
            {
                { "min", 18 },
                { "max", 65 }
            });
            configs.Add(ageConfig);

            await db.IdentifierConfigs.AddRangeAsync(configs);
            await db.SaveChangesAsync();
            
            Console.WriteLine($"Seeded {configs.Count} identifier configs successfully.");
        }
    }

    /// <summary>
    /// Test account data structure
    /// </summary>
    public class TestAccount
    {
        public string Role { get; set; } = string.Empty;
        public IdentifierType IdentifierType { get; set; }
        public string IdentifierConfigName { get; set; } = string.Empty;
        public string IdentifierValue { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Password hasher for test data using the same hasher as runtime
    /// </summary>
    public class TestPasswordHasher
    {
        private readonly IPasswordHasher _passwordHasher;

        public TestPasswordHasher()
        {
            // Use the same hasher as runtime with default options (matching Argon2Options defaults)
            var options = new Argon2Options
            {
                SaltSize = 16,
                HashSize = 32,
                MemorySizeKB = 65536,
                Iterations = 3,
                DegreeOfParallelism = Environment.ProcessorCount // Use same as runtime
            };
            
            var optionsWrapper = new OptionsWrapper<Argon2Options>(options);
            _passwordHasher = new Argon2PasswordHasher(optionsWrapper);
        }

        public Password Hash(string password)
        {
            return _passwordHasher.Hash(password);
        }
    }

    /// <summary>
    /// Simple wrapper for IOptions<T>
    /// </summary>
    public class OptionsWrapper<T> : IOptions<T> where T : class, new()
    {
        public OptionsWrapper(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
