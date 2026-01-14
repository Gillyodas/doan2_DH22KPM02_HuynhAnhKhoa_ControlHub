🚀
## 📋 Giới thiệu

ControlHub.Core là một thư viện Identity và Authentication đầy đủ tính năng được xây dựng trên .NET 8, sử dụng các patterns hiện đại như CQRS, Domain-Driven Design (DDD), và Entity Framework Core.

### ✨ Tính năng chính

- 🔐 **Multi-Identifier Authentication**: Hỗ trợ Email, Phone, Username, và custom identifiers
- 🎛️ **Dynamic Identifier Configuration**: Cấu hình validation rules tại runtime
- 🏗️ **Clean Architecture**: DDD + CQRS + Repository Pattern
- 🗄️ **Entity Framework Core**: Code-first migrations với schema support
- 🔑 **JWT Authentication**: Access & Refresh tokens
- 📊 **OpenTelemetry**: Monitoring và tracing
- 📝 **Swagger Documentation**: API documentation tự động
- 🧪 **Test Data Provider**: Built-in test data seeding

## 🚀 Quick Start

### Installation

```bash
dotnet add package ControlHub.Core
```

### Basic Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add ControlHub services
builder.Services.AddControlHub(builder.Configuration);

var app = builder.Build();

app.UseControlHub(app.Environment);
app.Run();
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ControlHub;Trusted_Connection=true;"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "ControlHub",
    "Audience": "ControlHub.Users",
    "AccessTokenExpiration": 3600,
    "RefreshTokenExpiration": 86400
  },
  "Argon2": {
    "SaltSize": 16,
    "MemorySize": 65536,
    "Iterations": 3
  }
}
```

## 📚 API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/signin` | Đăng nhập |
| `POST` | `/api/auth/register/user` | Đăng ký User |
| `POST` | `/api/auth/register/admin` | Đăng ký Admin |
| `POST` | `/api/auth/register/superadmin` | Đăng ký SuperAdmin |
| `POST` | `/api/auth/refresh` | Refresh token |

### Identifier Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/Identifier` | Lấy tất cả configs (Auth required) |
| `GET` | `/api/Identifier/active` | Lấy configs active (No auth) |
| `POST` | `/api/Identifier` | Tạo config mới (Auth required) |
| `PUT` | `/api/Identifier/{id}` | Cập nhật config (Auth required) |
| `PATCH` | `/api/Identifier/{id}/toggle-active` | Toggle active status (Auth required) |

## 🎯 Usage Examples

### SignIn với Email

```bash
POST /api/auth/signin
Content-Type: application/json

{
  "value": "user@example.com",
  "password": "User@123",
  "type": 0
}
```

### SignIn với Username

```bash
POST /api/auth/signin
Content-Type: application/json

{
  "value": "username123",
  "password": "User@123",
  "type": 2
}
```

### Tạo Identifier Config mới

```bash
POST /api/Identifier
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "StudentID",
  "description": "Student ID validation",
  "rules": [
    {
      "type": 3,
      "parameters": { "pattern": "^STU\\d{6}$" },
      "errorMessage": "Invalid student ID format",
      "order": 1
    }
  ]
}
```

## 🔧 Identifier Types

| Type | Value | Description |
|------|-------|-------------|
| Email | 0 | Email address |
| Phone | 1 | Phone number |
| Username | 2 | Username or custom identifier |

## 🛠️ Validation Rules

### Built-in Rules

- **Required**: Field không được để trống
- **Email**: Validate email format
- **Phone**: Validate phone number (international support)
- **MinLength**: Độ dài tối thiểu
- **MaxLength**: Độ dài tối đa
- **Pattern**: Regular expression pattern
- **Range**: Numeric range validation
- **Custom**: Custom validation logic

### Example Configuration

```csharp
// EmployeeID validation
var config = IdentifierConfig.Create("EmployeeID", "EmployeeID validation");
config.AddRule(ValidationRuleType.Required, new Dictionary<string, object>());
config.AddRule(ValidationRuleType.MinLength, new Dictionary<string, object> { { "length", 5 } });
config.AddRule(ValidationRuleType.MaxLength, new Dictionary<string, object> { { "length", 10 } });
config.AddRule(ValidationRuleType.Pattern, new Dictionary<string, object>
{
    { "pattern", @"^EMP\d{4,9}$" },
    { "options", 0 }
});
```

## 🧪 Database Seeding

ControlHub includes a comprehensive database seeding system that allows you to populate your database with initial data for development and testing.

### Automatic Seeding

By default, the seeding system will:
- **Check if data exists**: If the database already contains data, seeding will be skipped
- **Seed only when empty**: Data is only seeded when the database is empty
- **Provide console feedback**: All seeding operations are logged to the console

### Manual Seeding Control

You can control seeding behavior programmatically:

```csharp
// In Program.cs or your startup configuration
using ControlHub.Infrastructure.Persistence.Seeders;

// Get database context
var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// Seed only if database is empty (default behavior)
await ControlHubSeeder.SeedAsync(db, forceSeed: false);

// Force seed even if data exists (will clear and reseed)
await ControlHubSeeder.SeedAsync(db, forceSeed: true);

// Seed individual components
await TestDataProvider.SeedTestAccountsAsync(db, includeExtended: true, forceSeed: false);
await TestDataProvider.SeedPermissionsAndRolesAsync(db, forceSeed: false);
await TestDataProvider.SeedTestIdentifierConfigsAsync(db, forceSeed: false);
```

### Seeding Configuration

The seeding system includes these components:

| Component | Description | Default Behavior |
|-----------|-------------|------------------|
| **Roles** | SuperAdmin, Admin, User roles | Seeded if no roles exist |
| **Permissions** | 20+ system permissions | Seeded if no permissions exist |
| **Identifier Configs** | Email, Phone, Username, EmployeeID, Age validation | Seeded if no configs exist |
| **Test Accounts** | Pre-configured test users | Always cleared and reseeded |

### Test Accounts

The system creates these test accounts by default:

| Role | Identifier | Password | Usage |
|------|------------|----------|-------|
| SuperAdmin | `gillyodaswork@gmail.com` | `Admin@123` | Full system access |
| Admin | `admin123` | `Admin@123` | Administrative access |
| User | `EMP00001` | `Admin@123` | Standard user access |
| User | `+84123456789` | `Admin@123` | Phone-based login |

### Environment-Specific Seeding

You can configure different seeding behavior per environment:

```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    // Development: Force seed to ensure fresh test data
    await ControlHubSeeder.SeedAsync(db, forceSeed: true);
}
else if (app.Environment.IsProduction())
{
    // Production: Only seed if database is empty
    await ControlHubSeeder.SeedAsync(db, forceSeed: false);
}
```

### Custom Seeding

You can extend the seeding system for your custom data:

```csharp
public static class CustomSeeder
{
    public static async Task SeedCustomDataAsync(AppDbContext db, bool forceSeed = false)
    {
        var hasExistingData = await db.CustomEntities.AnyAsync();
        
        if (hasExistingData && !forceSeed)
        {
            Console.WriteLine("Custom data already exists. Use forceSeed=true to override.");
            return;
        }
        
        // Your custom seeding logic here
        var customData = new List<CustomEntity>
        {
            // ... create your entities
        };
        
        await db.CustomEntities.AddRangeAsync(customData);
        await db.SaveChangesAsync();
        
        Console.WriteLine($"Seeded {customData.Count} custom entities successfully.");
    }
}
```

## 🧪 Test Data Provider

Bao gồm built-in test data provider để dễ dàng testing:

```csharp
// Seed test data
await TestDataProvider.SeedTestAccountsAsync(db, includeExtended: false);

// Get test account
var superAdmin = TestDataProvider.GetTestAccount("gillyodaswork@gmail.com");

// Get accounts by role
var adminAccounts = TestDataProvider.GetTestAccountsByRole("Admin");
```

### Test Accounts

| Role | Identifier | Password |
|------|------------|----------|
| SuperAdmin | `gillyodaswork@gmail.com` | `Admin@123` |
| Admin | `admin123` | `Admin@123` |
| User | `EMP00001` | `Admin@123` |
| User | `+84123456789` | `Admin@123` |

## 🏗️ Architecture

### Layers

```bash
📁 ControlHub.API
├── Controllers        # API Controllers
├── ViewModels        # DTOs
└── Configurations    # API Configurations

📁 ControlHub.Application
├── Commands          # CQRS Commands
├── Queries            # CQRS Queries
├── DTOs               # Data Transfer Objects
└── Services          # Application Services

📁 ControlHub.Domain
├── Accounts         # Domain Entities
├── Roles            # Role Management
├── Users              # User Management
└── SharedKernel       # Shared Domain Logic

📁 ControlHub.Infrastructure
├── Persistence        # EF Core
├── Repositories       # Repository Implementations
└── Seeders           # Data Seeding
```

### Design Patterns

- **Domain-Driven Design (DDD)**: Domain entities, value objects, aggregates
- **CQRS**: Command Query Responsibility Segregation via MediatR
- **Repository Pattern**: Abstract data access
- **Unit of Work**: Transaction management
- **Result Pattern**: Consistent error handling

## 🔐 Security Features

- **Argon2 Password Hashing**: Modern password hashing algorithm
- **JWT Tokens**: Secure access and refresh keys
- **Role-based Authorization**: SuperAdmin > Admin > User
- **Password Policies**: Strong password requirements
- **Token Revocation**: Secure logout functionality

## 📊 Monitoring & Observability

- **OpenTelemetry**: Distributed tracing
- **Prometheus Metrics**: Application metrics
- **Serilog Logging**: Structured logging
- **Health Checks**: Application health monitoring

## 🛡️ Database Schema

### Core Tables

- `Roles`: User roles and permissions
- `Accounts`: User accounts with passwords
- `AccountIdentifiers`: Multi-identifier support
- `IdentifierConfig`: Dynamic validation rules
- `IdentifierValidationRules`: Rule definitions
- `Users`: User profiles
- `Tokens`: JWT token management

### Schema Support

ControlHub supports schemas (useful: `ControlHub`) for multi-tenant scenarios.

## 🔄 Migration Guide

### From v1.0 to v1.1

```bash
# Create migration
dotnet ef migrations add UpdateTo_v110

# Apply migration
dotnet database update
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 Email: support@controlhub.dev
- 🐛 Issues: [Git Issues](https://github.com/your-repo/controlhub/issues)
- 📖 Documentation: [Wiki](https://github.com/your-repo/controlhub/wiki)

## 🙏 Acknowledgments

- Built with .NET 8
- Powered by Entity Framework Core
- Secured with Argon2
- Documented with Swagger/OpenAPI

---

**ControlHub** - Identity & Authentication made simple! 🚀
