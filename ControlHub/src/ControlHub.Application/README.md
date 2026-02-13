# 🚀 ControlHub - Identity & Access Management NuGet Package

**Last Updated**: 2026-01-26

## 📋 Giới thiệu

**ControlHub** là một NuGet package dành cho developer trên Visual Studio, cung cấp giải pháp **Authentication (AuthN)** và **Authorization (AuthZ)** hoàn chỉnh cho Backend .NET 8. Package bao gồm:

- **Public interfaces** để tùy chỉnh và mở rộng
- **Giao diện đồ họa (React Dashboard)** được nhúng sẵn để quản lý trực quan
- **Clean Architecture**: DDD + CQRS + Repository Pattern

### ✨ Tính năng chính

| Tính năng | Mô tả |
|-----------|-------|
| 🔐 **Multi-Identifier Auth** | Hỗ trợ Email, Phone, Username, và custom identifiers |
| 🎛️ **Dynamic Identifier Config** | Cấu hình validation rules tại runtime |
| 🤖 **AI Log Analysis** | Phân tích log và phản hồi bằng AI (Ollama + Qdrant) |
| 💾 **Static Data Caching** | Cache các data tĩnh (Roles, Permissions, IdentifierConfigs) |
| 🔑 **JWT Authentication** | Access & Refresh tokens với Argon2 password hashing |
| 📊 **OpenTelemetry** | Monitoring, tracing, và Prometheus metrics |
| 📝 **Swagger Documentation** | API documentation tự động |
| 🎨 **Embedded React UI** | Dashboard đồ họa được nhúng sẵn |

---

## 🚀 Quick Start

### 1. Installation

```bash
dotnet add package ControlHub.Core
```

### 2. Program.cs Configuration

```csharp
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. HOST CONFIGURATION (Logging, Metrics, Tracing) - OPTIONAL
// =========================================================================

// Config Serilog (bắt buộc nếu muốn dùng AI Log Analysis)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "YourApp.API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new CompactJsonFormatter(), "Logs/log-.json", 
        rollingInterval: RollingInterval.Day, 
        retainedFileCountLimit: 14, 
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Config OpenTelemetry (optional)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://otel-collector:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    });

// =========================================================================
// 2. CONTROL HUB LIBRARY (CORE LOGIC) - BẮT BUỘC
// =========================================================================

builder.Services.AddControlHub(builder.Configuration);

// Bắt buộc cho Caching
builder.Services.AddMemoryCache();

// =========================================================================
// 3. BUILD & PIPELINE
// =========================================================================

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>(); // Optional
app.MapMetrics(); // Prometheus Endpoint (optional)

// CORS Configuration
app.UseCors(policy => policy
    .WithOrigins("http://localhost:3000", "http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.InjectStylesheet("/custom-swagger.css");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Authentication & Authorization Middleware - BẮT BUỘC theo thứ tự này
app.UseAuthentication();
app.UseAuthorization();

// Kích hoạt ControlHub (Auto Migration & Seed Data)
app.UseControlHub();

app.MapControllers();
app.Run();
```

### 3. appsettings.json Configuration

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=YourAppDB;Trusted_Connection=True;TrustServerCertificate=True;"
    },
    "Jwt": {
        "Issuer": "ControlHub",
        "Audience": "ControlHubUsers",
        "Key": "your-super-secret-long-key-at-least-32-characters"
    },
    "TokenSettings": {
        "AccessTokenMinutes": 10,
        "RefreshTokenDays": 14,
        "ResetPasswordMinutes": 30,
        "VerifyEmailHours": 24
    },
    "RoleSettings": {
        "SuperAdminRoleId": "9BA459E9-2A98-43C4-8530-392A63C66F1B",
        "AdminRoleId": "0CD24FAC-ABD7-4AD9-A7E4-248058B8D404",
        "UserRoleId": "8CF94B41-5AD8-4893-82B2-B193C91717AF"
    },
    "AppPassword": {
        "MasterKey": "YourMasterKeyForSuperAdmin"
    },
    "AI": {
        "OllamaUrl": "http://localhost:11434/api/generate",
        "ModelName": "llama3"
    },
    "Smtp": {
        "Host": "smtp.gmail.com",
        "Port": "587",
        "Username": "your-email@gmail.com",
        "Password": "your-app-password",
        "From": "your-email@gmail.com"
    },
    "BaseUrl": {
        "ClientBaseUrl": "https://yourapp.com",
        "DevBaseUrl": "https://localhost:7110"
    },
    "ControlHub": {
        "DashboardUrl": "https://localhost:7015/control-hub/index.html"
    }
}
```

---

## 📚 API Endpoints

### 🔐 Authentication (`AuthController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `POST` | `/api/Auth/users/register` | Đăng ký User mới | ❌ |
| `POST` | `/api/Auth/admins/register` | Đăng ký Admin | ✅ `CanCreateUser` |
| `POST` | `/api/Auth/superadmins/register` | Đăng ký SuperAdmin (cần MasterKey) | ❌ |
| `POST` | `/api/Auth/auth/signin` | Đăng nhập | ❌ |
| `POST` | `/api/Auth/auth/refresh` | Refresh access token | ❌ |
| `POST` | `/api/Auth/auth/signout` | Đăng xuất | ✅ |

### 👤 Account (`AccountController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `PATCH` | `/api/Account/users/{id}/password` | Đổi mật khẩu (chỉ chủ tài khoản) | ✅ + Resource-based |
| `POST` | `/api/Account/auth/forgot-password` | Yêu cầu reset password | ❌ |
| `POST` | `/api/Account/auth/reset-password` | Reset password bằng token | ❌ |
| `GET` | `/api/Account/admins` | Lấy danh sách Admin | ✅ `CanViewUsers` |

### 🏷️ Identifier Configuration (`IdentifierController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/Identifier` | Lấy tất cả identifier configs | ✅ `CanViewIdentifierConfigs` |
| `GET` | `/api/Identifier/active` | Lấy configs đang active (cho login page) | ❌ |
| `POST` | `/api/Identifier` | Tạo identifier config mới | ✅ `CanCreateIdentifierConfig` |
| `PUT` | `/api/Identifier/{id}` | Cập nhật identifier config | ✅ `CanUpdateIdentifierConfig` |
| `PATCH` | `/api/Identifier/{id}/toggle-active` | Bật/tắt active status | ✅ `CanToggleIdentifierConfig` |

### 🎭 Roles (`RoleController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/Role` | Lấy danh sách roles (có phân trang) | ✅ `CanViewRoles` |
| `POST` | `/api/Role/roles` | Tạo roles mới | ✅ `CanCreateRole` |
| `POST` | `/api/Role/roles/{roleId}/permissions` | Gán permissions cho role | ✅ `CanAssignPermission` |

### 🔒 Permissions (`PermissionController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/api/Permission` | Lấy danh sách permissions (có phân trang) | ✅ `CanViewPermissions` |
| `POST` | `/api/Permission/permissions` | Tạo permissions mới | ✅ `CanCreatePermission` |

### 👥 Users (`UserController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `PATCH` | `/api/User/users/{id}/username` | Đổi username | ✅ |

### 🤖 AI Audit (`AuditController`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `POST` | `/api/Audit/learn` | AI học log definitions từ code | ✅ `CanManageSystemSettings` |
| `GET` | `/api/Audit/analyze/{correlationId}` | Phân tích session log bằng AI | ✅ `CanViewSystemLogs` |
| `POST` | `/api/Audit/chat` | Chat với AI về logs | ✅ `CanViewSystemLogs` |

---

## 🎯 Usage Examples

### SignIn với Email

```bash
POST /api/Auth/auth/signin
Content-Type: application/json

{
  "value": "user@example.com",
  "password": "User@123",
  "type": 0
}
```

### SignIn với Username

```bash
POST /api/Auth/auth/signin
Content-Type: application/json

{
  "value": "username123",
  "password": "User@123",
  "type": 2
}
```

### AI Chat với Logs

```bash
POST /api/Audit/chat?lang=vi
Authorization: Bearer {token}
Content-Type: application/json

{
  "question": "Có lỗi nào xảy ra trong 24 giờ qua?",
  "startTime": "2026-01-25T00:00:00Z",
  "endTime": "2026-01-26T00:00:00Z"
}
```

---

## 🔌 Public Interfaces

### Core Repositories

| Interface | Mô tả |
|-----------|-------|
| `IAccountRepository` | Quản lý Account CRUD operations |
| `IAccountQueries` | Query accounts (read-only) |
| `IUserRepository` | Quản lý User CRUD operations |
| `IUserQueries` | Query users (read-only) |
| `IRoleRepository` | Quản lý Roles |
| `IRoleQueries` | Query roles (có cache) |
| `IPermissionRepository` | Quản lý Permissions |
| `IPermissionQueries` | Query permissions (có cache) |
| `IIdentifierConfigRepository` | Quản lý Identifier Configurations (có cache) |
| `ITokenRepository` | Quản lý JWT Tokens |

### Security & Tokens

| Interface | Mô tả |
|-----------|-------|
| `IPasswordHasher` | Hash/Verify passwords (Argon2) |
| `IAccessTokenGenerator` | Generate JWT access tokens |
| `IRefreshTokenGenerator` | Generate refresh tokens |
| `ITokenFactory` | Factory pattern cho token generation |
| `ITokenVerifier` | Verify token validity |

### Validation

| Interface | Mô tả |
|-----------|-------|
| `IIdentifierValidator` | Validate identifier values (Email/Phone/Username) |
| `IAccountValidator` | Validate account operations |
| `IPermissionValidator` | Validate permissions |

### AI & Logging

| Interface | Mô tả |
|-----------|-------|
| `ILogReaderService` | Đọc logs từ file/database |
| `IAIAnalysisService` | Gọi AI model để phân tích |
| `IEmbeddingService` | Tạo embeddings cho RAG |
| `IVectorDatabase` | Lưu trữ vectors (Qdrant) |

### Infrastructure

| Interface | Mô tả |
|-----------|-------|
| `IUnitOfWork` | Transaction management |
| `IEmailSender` | Gửi email (SMTP) |
| `IOutboxHandler` | Outbox pattern cho async operations |

---

## ⚙️ CQRS Handlers

### Commands (Write Operations)

| Handler | Command | Mô tả |
|---------|---------|-------|
| `SignInCommandHandler` | `SignInCommand` | Xử lý đăng nhập |
| `RegisterUserCommandHandler` | `RegisterUserCommand` | Đăng ký user |
| `RegisterAdminCommandHandler` | `RegisterAdminCommand` | Đăng ký admin |
| `RegisterSupperAdminCommandHandler` | `RegisterSupperAdminCommand` | Đăng ký super admin |
| `RefreshAccessTokenCommandHandler` | `RefreshAccessTokenCommand` | Refresh token |
| `SignOutCommandHandler` | `SignOutCommand` | Đăng xuất |
| `ChangePasswordCommandHandler` | `ChangePasswordCommand` | Đổi mật khẩu |
| `ForgotPasswordCommandHandler` | `ForgotPasswordCommand` | Quên mật khẩu |
| `ResetPasswordCommandHandler` | `ResetPasswordCommand` | Reset mật khẩu |
| `CreateIdentifierConfigCommandHandler` | `CreateIdentifierConfigCommand` | Tạo identifier config |
| `UpdateIdentifierConfigCommandHandler` | `UpdateIdentifierConfigCommand` | Cập nhật identifier config |
| `ToggleIdentifierActiveCommandHandler` | `ToggleIdentifierActiveCommand` | Toggle active status |
| `CreateRolesCommandHandler` | `CreateRolesCommand` | Tạo roles |
| `AddPermissonsForRoleCommandHandler` | `AddPermissonsForRoleCommand` | Gán permissions cho role |
| `CreatePermissionsCommandHandler` | `CreatePermissionsCommand` | Tạo permissions |
| `UpdateUsernameCommandHandler` | `UpdateUsernameCommand` | Cập nhật username |
| `AddIdentifierCommandHandler` | `AddIdentifierCommand` | Thêm identifier cho account |

### Queries (Read Operations)

| Handler | Query | Mô tả |
|---------|-------|-------|
| `GetIdentifierConfigsQueryHandler` | `GetIdentifierConfigsQuery` | Lấy tất cả identifier configs |
| `GetActiveIdentifierConfigsQueryHandler` | `GetActiveIdentifierConfigsQuery` | Lấy identifier configs active |
| `GetAdminAccountsQueryHandler` | `GetAdminAccountsQuery` | Lấy danh sách admin |
| `SearchRolesQueryHandler` | `SearchRolesQuery` | Tìm kiếm roles (phân trang) |
| `SearchPermissionsQueryHandler` | `SearchPermissionsQuery` | Tìm kiếm permissions (phân trang) |

---

## 💾 Caching Strategy

ControlHub sử dụng **Decorator Pattern** để cache các data tĩnh thông qua `IMemoryCache`:

### Cached Repositories

| Repository | Cache Duration | Cache Keys |
|------------|----------------|------------|
| `CachedRoleRepository` | 4 hours (sliding: 30 min) | `Role-{id}`, `Role-Name-{name}` |
| `CachedPermissionRepository` | 4 hours (sliding: 30 min) | `Permission-{id}` |
| `CachedIdentifierConfigRepository` | 4 hours (sliding: 30 min) | `IdentifierConfig-Active`, `IdentifierConfig-Id-{id}`, `IdentifierConfig-Name-{name}` |

### Cache Invalidation

Cache tự động invalidate khi:
- Thêm mới entity (`AddAsync`)
- Cập nhật entity (`UpdateAsync`)
- Xóa entity (`DeleteAsync`)

---

## 🤖 AI Log Analysis (RAG)

ControlHub tích hợp **Retrieval-Augmented Generation (RAG)** để phân tích logs thông minh.

### Components

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  LogReaderService │ → │ LogKnowledgeService │ → │ LocalAIAdapter   │
│  (Read JSON Logs) │    │  (RAG Orchestrator) │    │  (Ollama LLM)   │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                              │
                              ↓
                ┌─────────────────────────┐
                │  QdrantVectorStore      │
                │  (Vector Embeddings)    │
                └─────────────────────────┘
```

### Setup AI Services

1. **Start Ollama** (local LLM):
```bash
docker run -d -p 11434:11434 ollama/ollama
docker exec -it <container_id> ollama pull llama3
```

2. **Start Qdrant** (Vector DB):
```bash
docker run -d -p 6333:6333 qdrant/qdrant
```

3. Configure `appsettings.json`:
```json
{
  "AI": {
    "OllamaUrl": "http://localhost:11434/api/generate",
    "ModelName": "llama3"
  }
}
```

### AI Endpoints

- `POST /api/Audit/learn` - Ingest log definitions vào Vector DB
- `GET /api/Audit/analyze/{correlationId}` - Phân tích session log
- `POST /api/Audit/chat` - Chat tự do với logs

---

## 🏗️ Architecture

```bash
📁 ControlHub.API          # API Controllers & ViewModels
├── Controllers/           # Shared controllers (AuditController)
├── Accounts/              # Auth, Identifier, Account controllers
├── Roles/                 # Role management
├── Permissions/           # Permission management
├── Users/                 # User management
└── Middlewares/           # Global exception handling

📁 ControlHub.Application  # Business Logic Layer
├── Accounts/              # Account CQRS (Commands, Queries, DTOs)
├── Roles/                 # Role CQRS
├── Permissions/           # Permission CQRS
├── Tokens/                # Token management
├── Users/                 # User CQRS
├── AI/                    # LogKnowledgeService (RAG)
├── Common/                # Behaviors, Interfaces, Logging
└── Emails/                # Email interfaces

📁 ControlHub.Domain       # Domain Entities & Business Rules
├── Accounts/              # Account, Identifier entities
├── Roles/                 # Role entity
├── Permissions/           # Permission constants
└── Users/                 # User entity

📁 ControlHub.Infrastructure  # Implementations
├── Accounts/              # Repositories, Validators
├── Roles/                 # Role repositories (with cache)
├── Permissions/           # Permission repositories (with cache)
├── AI/                    # Ollama, Qdrant implementations
├── Persistence/           # EF Core DbContext, Migrations
└── Extensions/            # AddControlHub, UseControlHub

📁 ControlHub.SharedKernel # Shared utilities
├── Common/                # Errors, Logs
└── Results/               # Result pattern
```

---

## 🧪 Test Accounts

| Role | Identifier | Password | Type |
|------|------------|----------|------|
| SuperAdmin | `gillyodaswork@gmail.com` | `Admin@123` | Email |
| Admin | `admin123` | `Admin@123` | Username |
| User | `EMP00001` | `Admin@123` | EmployeeID |
| User | `+84123456789` | `Admin@123` | Phone |

---

## 🔧 Identifier Types

| Type | Value | Mô tả |
|------|-------|-------|
| Email | `0` | Email address |
| Phone | `1` | Phone number |
| Username | `2` | Username hoặc custom identifier |

---

## 🔐 Security Features

- **Argon2 Password Hashing**: Modern password hashing algorithm
- **JWT Tokens**: Secure access and refresh tokens
- **Policy-based Authorization**: `Policies.CanViewUsers`, `Policies.CanCreateRole`, etc.
- **Resource-based Authorization**: `SameUserRequirement` cho change password
- **Token Revocation**: Secure logout functionality
- **CORS Configuration**: Configurable cross-origin policies

---

## 📊 Monitoring & Observability

- **OpenTelemetry**: Distributed tracing (OTLP export)
- **Prometheus Metrics**: `/metrics` endpoint
- **Serilog Logging**: Structured JSON logs
- **Health Checks**: Application health monitoring

---

## 🎨 Embedded Dashboard

ControlHub bao gồm React Dashboard được nhúng sẵn:

- **Mặc định**: `/control-hub/index.html` (Tương đối theo host)
- **Cấu hình custom**: `ControlHub:DashboardUrl` trong `appsettings.json`
- **Features**: Quản lý Users, Roles, Permissions, Identifier Configs

---

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

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

**ControlHub** - Identity & Access Management made simple! 🚀
