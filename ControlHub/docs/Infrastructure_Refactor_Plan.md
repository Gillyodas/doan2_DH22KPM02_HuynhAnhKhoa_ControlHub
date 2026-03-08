# Implementation Plan: Infrastructure Refactor — Entity-Centric → DDD Bounded Context

> **Mục tiêu:** Tái cấu trúc `ControlHub.Infrastructure` từ tổ chức theo Entity sang Bounded Context, align với Domain layer hiện tại.
> **Nguyên tắc cốt lõi:** Move files only — không thay đổi logic, không thêm abstraction, không đổi tên class trong cùng PR này.
> **Dành cho:** AI Agent thực thi. Mỗi task phải pass constraint trước khi sang task kế tiếp.

---

## TRẠNG THÁI HIỆN TẠI (AS-IS)

```
ControlHub.Infrastructure/
├── Accounts/
│   ├── Factories/
│   │   └── AccountFactory.cs
│   ├── Repositories/
│   │   ├── AccountQueries.cs
│   │   ├── AccountRepository.cs
│   │   ├── CachedIdentifierConfigRepository.cs
│   │   ├── IdentifierConfigRepository.cs
│   │   └── UserQueries.cs          ← nằm sai chỗ, User thuộc Identity BC
│   ├── Security/                   ← ⚠️ BỊ THIẾU TRONG PLAN CŨ
│   │   ├── Argon2Options.cs
│   │   └── Argon2PasswordHasher.cs
│   └── Validators/
│       └── AccountValidator.cs
│           ← ⚠️ V4: EmailIdentifierValidator, UsernameIdentifierValidator, PhoneIdentifierValidator
│              KHÔNG nằm ở đây — chúng là Domain classes tại
│              ControlHub.Domain/Identity/Identifiers/Rules/, không phải Infrastructure
│              DynamicIdentifierValidator cũng là Domain class tại
│              ControlHub.Domain/Identity/Identifiers/Services/
├── AI/                             ← KHÔNG TOUCH
│   ├── V1/
│   ├── V25/
│   └── V3/
├── Authorization/                  ← ⚠️ BỊ THIẾU TRONG PLAN CŨ
│   ├── Handlers/
│   │   ├── PermissionAuthorizationHandler.cs
│   │   ├── RolePermissionChangedHandler.cs   ← ⚠️ V4: BỊ THIẾU trong mọi plan trước
│   │   └── SameUserAuthorizationHandler.cs
│   └── Permissions/
│       ├── PermissionClaimsTransformation.cs
│       └── PermissionPolicyProvider.cs
├── DependencyInjection/
│   ├── AccountExtensions.cs
│   ├── AIExtensions.cs
│   ├── DatabaseExtensions.cs
│   ├── MessagingExtensions.cs
│   ├── ObservabilityExtensions.cs
│   ├── RolePermissionExtensions.cs
│   ├── SecurityExtensions.cs       ← ⚠️ BỊ THIẾU TRONG PLAN CŨ
│   └── TokenExtensions.cs
├── Emails/
│   └── SmtpEmailSender.cs
├── Logging/
│   └── LogReaderService.cs         ← KHÔNG TOUCH
├── Outboxs/
│   ├── Handler/
│   │   ├── EmailOutboxHandler.cs
│   │   └── OutboxHandlerFactory.cs
│   ├── Repositories/
│   │   └── OutboxRepository.cs
│   └── OutboxProcessor.cs
├── Permissions/
│   ├── AuthZ/                      ← ⚠️ BỊ THIẾU TRONG PLAN CŨ
│   │   └── PermissionClaimsTransformation.cs
│   ├── Repositories/
│   │   ├── CachedPermissionRepository.cs
│   │   ├── PermissionQueries.cs
│   │   └── PermissionRepository.cs
│   ├── PermissionConfig.cs         ← ⚠️ V3: nằm ở ROOT Permissions/, KHÔNG phải Persistence/Configurations/
│   └── PermissionValidator.cs      ← ⚠️ V3: FILE MỚI, chưa có trong plan V2
├── Persistence/
│   ├── Configurations/
│   │   ├── AccountConfig.cs
│   │   ├── IdentifierConfig.cs       ← ⚠️ tên trùng với Domain class
│   │   ├── OutboxMessageConfig.cs
│   │   ├── RoleConfig.cs             ← ⚠️ V3: cần verify vị trí thực tế
│   │   ├── RolePermissionConfig.cs   ← ⚠️ V3: cần verify vị trí thực tế
│   │   ├── TokenConfig.cs
│   │   └── UserConfig.cs
│   ├── Seeders/
│   │   └── TestDataProvider.cs
│   ├── AppDbContext.cs
│   └── UnitOfWork.cs
├── RealTime/                        ← KHÔNG TOUCH
├── RolePermissions/
│   ├── RolePermissionEntity.cs
│   └── Repositories/               ← ⚠️ V3: THIẾU trong plan V2
│       ├── RolePermissionRepository.cs
│       └── RolePermissionQuery.cs
├── Roles/
│   └── Repositories/
│       ├── CachedRoleQueries.cs
│       ├── CachedRoleRepository.cs
│       ├── RoleQueries.cs
│       └── RoleRepository.cs
├── Services/
│   └── CurrentUserService.cs
├── Tokens/
│   ├── ConfigureJwtBearerOptions.cs  ← ⚠️ V4: nằm ở ROOT Tokens/, KHÔNG phải Authorization/Permissions/
│   ├── Generate/
│   │   ├── AccessTokenGenerator.cs
│   │   ├── EmailConfirmationTokenGenerator.cs
│   │   ├── PasswordResetTokenGenerator.cs
│   │   ├── RefreshTokenGenerator.cs
│   │   └── TokenGeneratorBase.cs    ← ⚠️ BỊ THIẾU TRONG PLAN CŨ
│   ├── Repositories/
│   │   ├── TokenQueries.cs
│   │   └── TokenRepository.cs
│   ├── Sender/
│   │   ├── EmailTokenSender.cs
│   │   ├── SmsTokenSender.cs
│   │   └── TokenSenderFactory.cs
│   ├── TokenFactory.cs
│   ├── TokenSettings.cs
│   └── TokenVerifier.cs
├── Users/
│   └── Repositories/
│       └── UserRepository.cs
└── Migrations/                      ← KHÔNG TOUCH
```

---

## TRẠNG THÁI MỤC TIÊU (TO-BE)

```
ControlHub.Infrastructure/
├── Identity/                                    ← BC: Identity
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── AccountConfiguration.cs          ← rename từ AccountConfig.cs
│   │   │   ├── IdentifierConfiguration.cs       ← rename từ IdentifierConfig.cs (fix ambiguity)
│   │   │   └── UserConfiguration.cs             ← rename từ UserConfig.cs
│   │   └── Repositories/
│   │       ├── AccountFactory.cs
│   │       ├── AccountQueries.cs
│   │       ├── AccountRepository.cs
│   │       ├── AccountValidator.cs
│   │       ├── CachedIdentifierConfigRepository.cs
│   │       ├── IdentifierConfigRepository.cs
│   │       ├── UserQueries.cs
│   │       └── UserRepository.cs
│   ├── Security/                                ← ⚠️ MỚI — giữ nguyên tên folder
│   │   ├── Argon2Options.cs
│   │   └── Argon2PasswordHasher.cs
│   └── Persistence/
│       ├── Configurations/
│       │   ├── AccountConfiguration.cs          ← rename từ AccountConfig.cs
│       │   ├── IdentifierConfiguration.cs       ← rename từ IdentifierConfig.cs (fix ambiguity)
│       │   └── UserConfiguration.cs             ← rename từ UserConfig.cs
│       └── Repositories/
│           ├── AccountFactory.cs
│           ├── AccountQueries.cs
│           ├── AccountRepository.cs
│           ├── AccountValidator.cs              ← move từ Accounts/Validators/
│           ├── CachedIdentifierConfigRepository.cs
│           ├── IdentifierConfigRepository.cs
│           ├── UserQueries.cs
│           └── UserRepository.cs
│
│   ← ⚠️ V4: KHÔNG có Identity/Validators/ folder.
│     EmailIdentifierValidator, UsernameIdentifierValidator, PhoneIdentifierValidator,
│     DynamicIdentifierValidator đều là Domain classes, KHÔNG di chuyển.
│
├── AccessControl/                               ← BC: AccessControl
│   ├── Authorization/                           ← ⚠️ MỚI — move từ root Authorization/
│   │   ├── Handlers/
│   │   │   ├── PermissionAuthorizationHandler.cs
│   │   │   ├── RolePermissionChangedHandler.cs  ← ⚠️ V4: thêm mới
│   │   │   └── SameUserAuthorizationHandler.cs
│   │   └── Permissions/
│   │       ├── ConfigureJwtBearerOptions.cs     ← ⚠️ V4: move từ Tokens/ (không phải Authorization/Permissions/)
│   │       ├── PermissionClaimsTransformation.cs  ← move từ Authorization/Permissions/
│   │       └── PermissionPolicyProvider.cs
│   └── Persistence/
│       ├── Configurations/
│       │   ├── PermissionConfiguration.cs       ← rename từ Permissions/PermissionConfig.cs ⚠️V3
│       │   ├── RoleConfiguration.cs             ← rename từ RoleConfig.cs
│       │   └── RolePermissionConfiguration.cs   ← rename từ RolePermissionConfig.cs
│       └── Repositories/
│           ├── CachedPermissionRepository.cs
│           ├── CachedRoleQueries.cs
│           ├── CachedRoleRepository.cs
│           ├── PermissionQueries.cs
│           ├── PermissionRepository.cs
│           ├── PermissionValidator.cs            ← move từ Permissions/ ⚠️V3
│           ├── RolePermissionEntity.cs
│           ├── RolePermissionQuery.cs            ← move từ RolePermissions/Repositories/ ⚠️V3
│           ├── RolePermissionRepository.cs       ← move từ RolePermissions/Repositories/ ⚠️V3
│           ├── RoleQueries.cs
│           └── RoleRepository.cs
│
├── TokenManagement/                             ← BC: TokenManagement (rename từ Tokens/)
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   └── TokenConfiguration.cs            ← rename từ TokenConfig.cs
│   │   └── Repositories/
│   │       ├── TokenQueries.cs
│   │       └── TokenRepository.cs
│   └── Services/
│       ├── Generate/
│       │   ├── AccessTokenGenerator.cs
│       │   ├── EmailConfirmationTokenGenerator.cs
│       │   ├── PasswordResetTokenGenerator.cs
│       │   ├── RefreshTokenGenerator.cs
│       │   └── TokenGeneratorBase.cs            ← ⚠️ phải move cùng
│       ├── Sender/
│       │   ├── EmailTokenSender.cs
│       │   ├── SmsTokenSender.cs
│       │   └── TokenSenderFactory.cs
│       ├── TokenFactory.cs
│       ├── TokenSettings.cs
│       └── TokenVerifier.cs
│
├── Messaging/                                   ← Outbox + Email
│   ├── Email/
│   │   └── SmtpEmailSender.cs
│   └── Outbox/
│       ├── Handler/
│       │   ├── EmailOutboxHandler.cs
│       │   └── OutboxHandlerFactory.cs
│       ├── Repositories/
│       │   └── OutboxRepository.cs
│       └── OutboxProcessor.cs
│
├── Persistence/                                 ← EF Core cross-cutting (giữ nguyên)
│   ├── Configurations/
│   │   └── OutboxMessageConfiguration.cs        ← rename từ OutboxMessageConfig.cs
│   ├── Seeders/
│   │   └── TestDataProvider.cs
│   ├── AppDbContext.cs
│   └── UnitOfWork.cs
│
├── Common/                                      ← Shared infrastructure services
│   └── Services/
│       └── CurrentUserService.cs
│
├── AI/                                          ← KHÔNG TOUCH
├── Logging/                                     ← KHÔNG TOUCH
├── RealTime/                                    ← KHÔNG TOUCH
├── DependencyInjection/                         ← Update namespace refs cuối cùng
│   ├── AccountExtensions.cs    → IdentityExtensions.cs
│   ├── AIExtensions.cs         ← KHÔNG TOUCH
│   ├── DatabaseExtensions.cs   ← KHÔNG TOUCH
│   ├── MessagingExtensions.cs  ← update using only
│   ├── ObservabilityExtensions.cs ← KHÔNG TOUCH
│   ├── RolePermissionExtensions.cs → AccessControlExtensions.cs
│   ├── SecurityExtensions.cs   ← update using only (Argon2, Authorization namespaces)
│   └── TokenExtensions.cs      → TokenManagementExtensions.cs
└── Migrations/                                  ← KHÔNG TOUCH TUYỆT ĐỐI
```

---

## GLOBAL CONSTRAINTS (Áp dụng cho mọi task)

```
CONSTRAINT-G1: KHÔNG được thay đổi logic bên trong bất kỳ file nào.
               Chỉ được thay đổi: namespace declaration, using directives, file path.

CONSTRAINT-G2: KHÔNG được touch các folder/files sau:
               - Migrations/ (bất kỳ file nào)
               - AI/ (bất kỳ file nào)
               - Logging/ (bất kỳ file nào)
               - RealTime/ (bất kỳ file nào)
               - DependencyInjection/DatabaseExtensions.cs
               - DependencyInjection/AIExtensions.cs
               - DependencyInjection/ObservabilityExtensions.cs

CONSTRAINT-G3: Sau mỗi Phase, phải chạy: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj
               Build phải pass (0 errors) trước khi sang Phase tiếp theo.

CONSTRAINT-G4: Namespace convention:
               - Identity BC:       ControlHub.Infrastructure.Identity.*
               - AccessControl BC:  ControlHub.Infrastructure.AccessControl.*
               - TokenManagement BC: ControlHub.Infrastructure.TokenManagement.*
               - Messaging:         ControlHub.Infrastructure.Messaging.*
               - Common services:   ControlHub.Infrastructure.Common.*
               - Persistence:       ControlHub.Infrastructure.Persistence.* (giữ nguyên)

CONSTRAINT-G5: Class names KHÔNG được đổi. Chỉ file name và namespace thay đổi.
               Exception duy nhất: EF Configuration files (xem CONSTRAINT-G6).

CONSTRAINT-G6: Các EF Configuration class phải rename theo pattern [Entity]Configuration
               để tránh ambiguity với Domain classes:
               - AccountConfig     → AccountConfiguration     (class name + file name)
               - IdentifierConfig  → IdentifierConfiguration  (class name + file name) ⚠️ CRITICAL
               - UserConfig        → UserConfiguration        (class name + file name)
               - PermissionConfig  → PermissionConfiguration  (class name + file name)
               - RoleConfig        → RoleConfiguration        (class name + file name)
               - RolePermissionConfig → RolePermissionConfiguration
               - TokenConfig       → TokenConfiguration       (class name + file name)
               - OutboxMessageConfig → OutboxMessageConfiguration

CONSTRAINT-G7: AppDbContext.cs dùng ApplyConfigurationsFromAssembly() — 
               KHÔNG cần update AppDbContext khi rename Configuration classes,
               nhưng phải verify sau rename rằng không còn explicit ApplyConfiguration<T>() calls.

CONSTRAINT-G8: Sau Phase cuối, phải chạy full build toàn solution:
               dotnet build src\ControlHub.sln
               Phải pass 0 errors, 0 warnings liên quan đến namespace.
```

---

## PHASE 0 — CHUẨN BỊ

### Task 0.1 — Tạo Git branch
```
ACTION: git checkout -b refactor/infrastructure-ddd-structure
CONSTRAINT: Phải từ branch main/master hiện tại.
VERIFY: git branch --show-current == "refactor/infrastructure-ddd-structure"
```

### Task 0.2 — Scan toàn bộ using references
```
ACTION: Chạy lệnh sau để lấy danh sách tất cả files có using ControlHub.Infrastructure.*
        grep -r "using ControlHub.Infrastructure" src/ --include="*.cs" -l > /tmp/infra_refs.txt
        cat /tmp/infra_refs.txt

PURPOSE: Có danh sách đầy đủ files cần update using sau khi move.
VERIFY: File /tmp/infra_refs.txt tồn tại và không rỗng.
```

### Task 0.3 — Snapshot build baseline
```
ACTION: dotnet build src\ControlHub.sln 2>&1 | tail -5
VERIFY: Output phải chứa "Build succeeded" với 0 error(s).
        Ghi lại số warnings hiện tại để so sánh sau.
```

---

## PHASE 1 — IDENTITY BOUNDED CONTEXT

**Scope:** Move Accounts/ + Users/ → Identity/
**Estimated files:** ~15 files

### Task 1.1 — Tạo folder structure
```
ACTION: Tạo các folders sau (tạo .gitkeep nếu cần):
        src\ControlHub.Infrastructure\Identity\
        src\ControlHub.Infrastructure\Identity\Persistence\
        src\ControlHub.Infrastructure\Identity\Persistence\Configurations\
        src\ControlHub.Infrastructure\Identity\Persistence\Repositories\
        src\ControlHub.Infrastructure\Identity\Security\

NOTE: KHÔNG tạo Identity\Validators\ — không có Infrastructure file nào cần folder này.
      (EmailIdentifierValidator, UsernameIdentifierValidator, PhoneIdentifierValidator,
       DynamicIdentifierValidator đều là Domain classes tại Domain/Identity/Identifiers/)

VERIFY: Tất cả folders tồn tại.
CONSTRAINT: Chưa move file nào trong task này.
```

### Task 1.2 — Move và rename EF Configurations
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Persistence\Configurations\AccountConfig.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Configurations\AccountConfiguration.cs

  src\ControlHub.Infrastructure\Persistence\Configurations\IdentifierConfig.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Configurations\IdentifierConfiguration.cs

  src\ControlHub.Infrastructure\Persistence\Configurations\UserConfig.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Configurations\UserConfiguration.cs

CHANGES IN EACH FILE:
  1. Đổi namespace: ControlHub.Infrastructure.Persistence.Configurations
                  → ControlHub.Infrastructure.Identity.Persistence.Configurations
  2. Đổi class name: AccountConfig → AccountConfiguration
                     IdentifierConfig → IdentifierConfiguration  ← ⚠️ CRITICAL: fix ambiguity
                     UserConfig → UserConfiguration
  3. Cập nhật class declaration: 
     public class AccountConfiguration : IEntityTypeConfiguration<Account>
     public class IdentifierConfiguration : IEntityTypeConfiguration<IdentifierConfig>   ← type arg giữ nguyên
     public class UserConfiguration : IEntityTypeConfiguration<User>

CONSTRAINT: Logic bên trong Configure() method KHÔNG được thay đổi.
VERIFY: 3 files mới tồn tại tại đúng path, 3 files cũ đã xóa.
```

### Task 1.3 — Move Repositories (Accounts)
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Accounts\Repositories\AccountQueries.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\AccountQueries.cs

  src\ControlHub.Infrastructure\Accounts\Repositories\AccountRepository.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\AccountRepository.cs

  src\ControlHub.Infrastructure\Accounts\Repositories\CachedIdentifierConfigRepository.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\CachedIdentifierConfigRepository.cs

  src\ControlHub.Infrastructure\Accounts\Repositories\IdentifierConfigRepository.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\IdentifierConfigRepository.cs

CHANGES IN EACH FILE:
  - Namespace: ControlHub.Infrastructure.Accounts.Repositories
             → ControlHub.Infrastructure.Identity.Persistence.Repositories

CONSTRAINT: Class names giữ nguyên. Logic giữ nguyên.
VERIFY: 4 files tại path mới. 4 files cũ đã xóa.
```

### Task 1.4 — Move Factories
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Accounts\Factories\AccountFactory.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\AccountFactory.cs

CHANGES: Namespace: ControlHub.Infrastructure.Accounts.Factories
                  → ControlHub.Infrastructure.Identity.Persistence.Repositories

NOTE: Đặt vào Repositories/ vì AccountFactory implement interface từ Application,
      không phải factory pattern thuần túy.

VERIFY: File tại path mới. File cũ đã xóa.
```

### Task 1.5 — Move AccountValidator
```
⚠️ V4 CRITICAL CORRECTION:
  EmailIdentifierValidator, UsernameIdentifierValidator, PhoneIdentifierValidator,
  DynamicIdentifierValidator KHÔNG phải Infrastructure files.
  Chúng nằm tại ControlHub.Domain/Identity/Identifiers/Rules/ và Services/ với
  namespace ControlHub.Domain.Identity.Identifiers.Rules — KHÔNG di chuyển, KHÔNG đổi namespace.

FILE TO MOVE:
  src\ControlHub.Infrastructure\Accounts\Validators\AccountValidator.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\AccountValidator.cs

CHANGES:
  AccountValidator.cs: ControlHub.Infrastructure.Accounts.Validators
                     → ControlHub.Infrastructure.Identity.Persistence.Repositories

NOTE về DI (AccountExtensions.cs): 3 using lines sau sẽ vẫn đúng sau refactor,
  KHÔNG cần thay đổi vì chúng reference Domain namespace:
    using ControlHub.Domain.Identity.Identifiers.Rules;     ← giữ nguyên
    using ControlHub.Domain.Identity.Identifiers.Services;  ← giữ nguyên
  Chỉ cần xóa:
    using ControlHub.Infrastructure.Accounts.Validators;    ← bỏ, thay bằng Identity namespace

VERIFY: AccountValidator.cs tại path mới. File cũ đã xóa.
        Folder Accounts\Validators\ trống → xóa.
```

### Task 1.6 — Move User Repositories
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Users\Repositories\UserRepository.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\UserRepository.cs

  src\ControlHub.Infrastructure\Accounts\Repositories\UserQueries.cs
    → src\ControlHub.Infrastructure\Identity\Persistence\Repositories\UserQueries.cs

CHANGES: Namespace → ControlHub.Infrastructure.Identity.Persistence.Repositories

VERIFY: 2 files tại path mới. 2 files cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Users\ phải trống (xóa nếu trống).
        Folder src\ControlHub.Infrastructure\Accounts\ phải trống (xóa nếu trống).
```

### Task 1.7 — Move Security files (Argon2)
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Accounts\Security\Argon2Options.cs
    → src\ControlHub.Infrastructure\Identity\Security\Argon2Options.cs

  src\ControlHub.Infrastructure\Accounts\Security\Argon2PasswordHasher.cs
    → src\ControlHub.Infrastructure\Identity\Security\Argon2PasswordHasher.cs

ACTION: Tạo folder src\ControlHub.Infrastructure\Identity\Security\ trước.

CHANGES: Namespace: ControlHub.Infrastructure.Accounts.Security
                  → ControlHub.Infrastructure.Identity.Security

CRITICAL NOTE: SecurityExtensions.cs trong DependencyInjection/ đang dùng:
  using ControlHub.Infrastructure.Accounts.Security;  ← phải update ở Phase 6
  
VERIFY: 2 files mới tồn tại. Folder Accounts\Security\ trống → xóa.
        Folder Accounts\ hoàn toàn trống → xóa.
```

### Task 1.8 — Build check Phase 1
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj

EXPECTED: Build succeeded, 0 errors.

NẾU CÓ LỖI: Các lỗi thường gặp và cách fix:
  - "CS0246: The type or namespace 'AccountConfig' could not be found"
    → AppDbContext.cs có ApplyConfiguration<AccountConfig>() explicit call.
      Fix: đổi thành ApplyConfiguration<AccountConfiguration>()
  
  - "CS0234: namespace 'Accounts' does not exist"
    → Còn using cũ trong file nào đó. Grep và fix.
  
  - Ambiguous reference 'IdentifierConfig'
    → Thiếu using, hoặc cần qualify tên đầy đủ. Fix bằng cách thêm using alias.
  
  - "Argon2Options/Argon2PasswordHasher not found" trong SecurityExtensions.cs
    → Chưa fix ở phase này. Update using tạm:
      using ControlHub.Infrastructure.Identity.Security;

VERIFY: 0 errors. Ghi lại warnings nếu có.
```

### Task 1.9 — Commit Phase 1
```
ACTION: git add -A
        git commit -m "refactor: move Identity infrastructure to DDD BC structure

        - Move Accounts/, Users/ → Identity/Persistence/Repositories/
        - Move AccountValidator → Identity/Persistence/Repositories/
        - Move Accounts/Security/ → Identity/Security/ (Argon2PasswordHasher, Argon2Options)
        - Move identity EF configs → Identity/Persistence/Configurations/
        - Rename EF config classes: *Config → *Configuration (fix Domain name ambiguity)
        - Namespace update: *.Accounts.* → *.Identity.*"

VERIFY: git log --oneline -1 hiển thị commit message trên.
```

---

## PHASE 2 — ACCESS CONTROL BOUNDED CONTEXT

**Scope:** Move Roles/ + Permissions/ + RolePermissions/ → AccessControl/
**Estimated files:** ~10 files

### Task 2.1 — Tạo folder structure
```
ACTION: Tạo các folders:
        src\ControlHub.Infrastructure\AccessControl\
        src\ControlHub.Infrastructure\AccessControl\Persistence\
        src\ControlHub.Infrastructure\AccessControl\Persistence\Configurations\
        src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\

VERIFY: Folders tồn tại.
```

### Task 2.2 — Move và rename EF Configurations
```
FILES TO MOVE:
  ⚠️ V3 CORRECTION: PermissionConfig.cs KHÔNG nằm ở Persistence/Configurations/ mà nằm ở Permissions/
  
  src\ControlHub.Infrastructure\Permissions\PermissionConfig.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Configurations\PermissionConfiguration.cs

  src\ControlHub.Infrastructure\Persistence\Configurations\RoleConfig.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Configurations\RoleConfiguration.cs

  src\ControlHub.Infrastructure\Persistence\Configurations\RolePermissionConfig.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Configurations\RolePermissionConfiguration.cs

CHANGES IN EACH FILE:
  1. Namespace → ControlHub.Infrastructure.AccessControl.Persistence.Configurations
  2. Class rename: PermissionConfig → PermissionConfiguration
                   RoleConfig → RoleConfiguration
                   RolePermissionConfig → RolePermissionConfiguration

VERIFY: 3 files mới tồn tại. 3 files cũ đã xóa.
```

### Task 2.3 — Move Role Repositories
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Roles\Repositories\CachedRoleQueries.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\CachedRoleQueries.cs

  src\ControlHub.Infrastructure\Roles\Repositories\CachedRoleRepository.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\CachedRoleRepository.cs

  src\ControlHub.Infrastructure\Roles\Repositories\RoleQueries.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\RoleQueries.cs

  src\ControlHub.Infrastructure\Roles\Repositories\RoleRepository.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\RoleRepository.cs

CHANGES: Namespace: ControlHub.Infrastructure.Roles.Repositories
                  → ControlHub.Infrastructure.AccessControl.Persistence.Repositories

VERIFY: 4 files mới. 4 files cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Roles\ trống → xóa.
```

### Task 2.4 — Move Permission Repositories + PermissionValidator
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Permissions\Repositories\CachedPermissionRepository.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\CachedPermissionRepository.cs

  src\ControlHub.Infrastructure\Permissions\Repositories\PermissionQueries.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\PermissionQueries.cs

  src\ControlHub.Infrastructure\Permissions\Repositories\PermissionRepository.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\PermissionRepository.cs

  ⚠️ V3 NEW: PermissionValidator.cs nằm ở ROOT Permissions/ (không phải Repositories/)
  src\ControlHub.Infrastructure\Permissions\PermissionValidator.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\PermissionValidator.cs

CHANGES: Namespace: ControlHub.Infrastructure.Permissions.Repositories (và ControlHub.Infrastructure.Permissions)
                  → ControlHub.Infrastructure.AccessControl.Persistence.Repositories

VERIFY: 4 files mới. 4 files cũ đã xóa.
        Permissions\Repositories\ trống → xóa.
        Permissions\ chỉ còn AuthZ\ (xử lý Task 2.6) → chưa xóa.
```

### Task 2.5 — Move RolePermissionEntity + RolePermissions Repositories
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\RolePermissions\RolePermissionEntity.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\RolePermissionEntity.cs

  ⚠️ V3 NEW: RolePermissions/Repositories/ chứa 2 files thêm
  src\ControlHub.Infrastructure\RolePermissions\Repositories\RolePermissionRepository.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\RolePermissionRepository.cs

  src\ControlHub.Infrastructure\RolePermissions\Repositories\RolePermissionQuery.cs
    → src\ControlHub.Infrastructure\AccessControl\Persistence\Repositories\RolePermissionQuery.cs

CHANGES:
  RolePermissionEntity.cs:      ControlHub.Infrastructure.RolePermissions
                               → ControlHub.Infrastructure.AccessControl.Persistence.Repositories
  
  RolePermissionRepository.cs:  ControlHub.Infrastructure.RolePermissions.Repositories
                               → ControlHub.Infrastructure.AccessControl.Persistence.Repositories
  
  RolePermissionQuery.cs:       ControlHub.Infrastructure.RolePermissions.Repositories
                               → ControlHub.Infrastructure.AccessControl.Persistence.Repositories

VERIFY: 3 files mới tồn tại. 3 files cũ đã xóa.
        Folder src\ControlHub.Infrastructure\RolePermissions\ trống → xóa.
```

### Task 2.6 — Move Authorization handlers + PermissionClaimsTransformation + ConfigureJwtBearerOptions
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Authorization\Handlers\PermissionAuthorizationHandler.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Handlers\PermissionAuthorizationHandler.cs

  src\ControlHub.Infrastructure\Authorization\Handlers\SameUserAuthorizationHandler.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Handlers\SameUserAuthorizationHandler.cs

  ⚠️ V4 NEW: RolePermissionChangedHandler nằm trong Authorization/Handlers/ (chưa có trong plan trước)
  src\ControlHub.Infrastructure\Authorization\Handlers\RolePermissionChangedHandler.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Handlers\RolePermissionChangedHandler.cs

  src\ControlHub.Infrastructure\Authorization\Permissions\PermissionClaimsTransformation.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Permissions\PermissionClaimsTransformation.cs

  src\ControlHub.Infrastructure\Authorization\Permissions\PermissionPolicyProvider.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Permissions\PermissionPolicyProvider.cs

  ⚠️ V4 CRITICAL CORRECTION: ConfigureJwtBearerOptions.cs KHÔNG nằm ở Authorization/Permissions/
  mà nằm ở ROOT của Tokens/ folder.
  src\ControlHub.Infrastructure\Tokens\ConfigureJwtBearerOptions.cs
    → src\ControlHub.Infrastructure\AccessControl\Authorization\Permissions\ConfigureJwtBearerOptions.cs

ACTION: Tạo folders:
        src\ControlHub.Infrastructure\AccessControl\Authorization\
        src\ControlHub.Infrastructure\AccessControl\Authorization\Handlers\
        src\ControlHub.Infrastructure\AccessControl\Authorization\Permissions\

CHANGES:
  Authorization/Handlers files:    ControlHub.Infrastructure.Authorization.Handlers
                                 → ControlHub.Infrastructure.AccessControl.Authorization.Handlers
  
  Authorization/Permissions files: ControlHub.Infrastructure.Authorization.Permissions
                                 → ControlHub.Infrastructure.AccessControl.Authorization.Permissions
  
  ConfigureJwtBearerOptions:       ControlHub.Infrastructure.Tokens
                                 → ControlHub.Infrastructure.AccessControl.Authorization.Permissions

VERIFY: 6 files tại path mới (tăng từ 5 lên 6 so với V3). 
        Folder src\ControlHub.Infrastructure\Authorization\ trống → xóa.
        Folder src\ControlHub.Infrastructure\Permissions\AuthZ\ không còn tồn tại (đã xóa ở Task 2.4).
        Folder src\ControlHub.Infrastructure\Permissions\ trống (nếu Repositories/ cũng xong) → xóa.
```

### Task 2.7 — Build check Phase 2
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj

EXPECTED: 0 errors.

COMMON ERRORS:
  - "RolePermissionEntity" not found trong RolePermissionConfiguration.cs
    → Update using trong RolePermissionConfiguration.cs:
      using ControlHub.Infrastructure.AccessControl.Persistence.Repositories;

  - DI registration files (RolePermissionExtensions.cs) có fully-qualified class references
    → Ví dụ: new CachedRoleRepository(sp.GetRequiredService<RoleRepository>(), ...)
      Nếu dùng fully-qualified, phải update: ControlHub.Infrastructure.Roles.Repositories.RoleRepository
      → ControlHub.Infrastructure.AccessControl.Persistence.Repositories.RoleRepository
    → Hoặc thêm using alias để giữ code đơn giản.

  - "PermissionAuthorizationHandler not found" trong SecurityExtensions.cs
    → Update using tạm: using ControlHub.Infrastructure.AccessControl.Authorization.Handlers;
    
  - "PermissionPolicyProvider not found"
    → Update using tạm: using ControlHub.Infrastructure.AccessControl.Authorization.Permissions;

VERIFY: 0 errors.
```

### Task 2.8 — Commit Phase 2
```
ACTION: git commit -m "refactor: move AccessControl infrastructure to DDD BC structure

        - Move Roles/, Permissions/, RolePermissions/ → AccessControl/Persistence/Repositories/
        - Move Authorization/, Permissions/AuthZ/ → AccessControl/Authorization/
        - Move AccessControl EF configs → AccessControl/Persistence/Configurations/
        - Rename EF config classes: *Config → *Configuration
        - Namespace update: *.Roles.*, *.Permissions.*, *.RolePermissions.*, *.Authorization.* → *.AccessControl.*"
```

---

## PHASE 3 — TOKEN MANAGEMENT BOUNDED CONTEXT

**Scope:** Move Tokens/ → TokenManagement/
**Estimated files:** ~10 files

### Task 3.1 — Tạo folder structure
```
ACTION: Tạo các folders:
        src\ControlHub.Infrastructure\TokenManagement\
        src\ControlHub.Infrastructure\TokenManagement\Persistence\
        src\ControlHub.Infrastructure\TokenManagement\Persistence\Configurations\
        src\ControlHub.Infrastructure\TokenManagement\Persistence\Repositories\
        src\ControlHub.Infrastructure\TokenManagement\Services\
        src\ControlHub.Infrastructure\TokenManagement\Services\Generate\
        src\ControlHub.Infrastructure\TokenManagement\Services\Sender\

VERIFY: Folders tồn tại.
```

### Task 3.2 — Move EF Configuration
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Persistence\Configurations\TokenConfig.cs
    → src\ControlHub.Infrastructure\TokenManagement\Persistence\Configurations\TokenConfiguration.cs

CHANGES:
  1. Namespace → ControlHub.Infrastructure.TokenManagement.Persistence.Configurations
  2. Class rename: TokenConfig → TokenConfiguration

VERIFY: File mới tồn tại. File cũ đã xóa.
```

### Task 3.3 — Move Token Repositories
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Tokens\Repositories\TokenQueries.cs
    → src\ControlHub.Infrastructure\TokenManagement\Persistence\Repositories\TokenQueries.cs

  src\ControlHub.Infrastructure\Tokens\Repositories\TokenRepository.cs
    → src\ControlHub.Infrastructure\TokenManagement\Persistence\Repositories\TokenRepository.cs

CHANGES: Namespace: ControlHub.Infrastructure.Tokens.Repositories
                  → ControlHub.Infrastructure.TokenManagement.Persistence.Repositories

VERIFY: 2 files mới. 2 files cũ đã xóa.
```

### Task 3.4 — Move Token Services (Generate)
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Tokens\Generate\AccessTokenGenerator.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Generate\AccessTokenGenerator.cs

  src\ControlHub.Infrastructure\Tokens\Generate\EmailConfirmationTokenGenerator.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Generate\EmailConfirmationTokenGenerator.cs

  src\ControlHub.Infrastructure\Tokens\Generate\PasswordResetTokenGenerator.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Generate\PasswordResetTokenGenerator.cs

  src\ControlHub.Infrastructure\Tokens\Generate\RefreshTokenGenerator.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Generate\RefreshTokenGenerator.cs

  src\ControlHub.Infrastructure\Tokens\Generate\TokenGeneratorBase.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Generate\TokenGeneratorBase.cs

CHANGES: Namespace: ControlHub.Infrastructure.Tokens.Generate
                  → ControlHub.Infrastructure.TokenManagement.Services.Generate

NOTE: TokenGeneratorBase là abstract base class — tất cả generator concrete classes
      phải được move trong cùng task để tránh missing base class compile error.

VERIFY: 5 files mới. 5 files cũ đã xóa.
```

### Task 3.5 — Move Token Services (Sender + Core)
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Tokens\Sender\EmailTokenSender.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Sender\EmailTokenSender.cs

  src\ControlHub.Infrastructure\Tokens\Sender\SmsTokenSender.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Sender\SmsTokenSender.cs

  src\ControlHub.Infrastructure\Tokens\Sender\TokenSenderFactory.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\Sender\TokenSenderFactory.cs

  src\ControlHub.Infrastructure\Tokens\TokenFactory.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\TokenFactory.cs

  src\ControlHub.Infrastructure\Tokens\TokenSettings.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\TokenSettings.cs

  src\ControlHub.Infrastructure\Tokens\TokenVerifier.cs
    → src\ControlHub.Infrastructure\TokenManagement\Services\TokenVerifier.cs

NOTE: ConfigureJwtBearerOptions.cs đã được move ở Task 2.6 (Phase 2) → KHÔNG move ở đây.

CHANGES:
  Sender files:  ControlHub.Infrastructure.Tokens.Sender
              → ControlHub.Infrastructure.TokenManagement.Services.Sender
  
  Core files:   ControlHub.Infrastructure.Tokens
              → ControlHub.Infrastructure.TokenManagement.Services

VERIFY: 6 files mới. 6 files cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Tokens\ trống → xóa.
```

### Task 3.6 — Build check Phase 3
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj
EXPECTED: 0 errors.
VERIFY: 0 errors.
```

### Task 3.7 — Commit Phase 3
```
ACTION: git commit -m "refactor: move TokenManagement infrastructure to DDD BC structure

        - Move Tokens/ → TokenManagement/ (rename BC folder)
        - Separate repositories vs services subfolder
        - Move EF config → TokenManagement/Persistence/Configurations/
        - Namespace update: *.Tokens.* → *.TokenManagement.*"
```

---

## PHASE 4 — MESSAGING

**Scope:** Move Outboxs/ + Emails/ → Messaging/
**Estimated files:** ~6 files

### Task 4.1 — Tạo folder structure
```
ACTION: Tạo các folders:
        src\ControlHub.Infrastructure\Messaging\
        src\ControlHub.Infrastructure\Messaging\Email\
        src\ControlHub.Infrastructure\Messaging\Outbox\
        src\ControlHub.Infrastructure\Messaging\Outbox\Handler\
        src\ControlHub.Infrastructure\Messaging\Outbox\Repositories\

VERIFY: Folders tồn tại.
```

### Task 4.2 — Move Outbox MessageConfig
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Persistence\Configurations\OutboxMessageConfig.cs
    → src\ControlHub.Infrastructure\Persistence\Configurations\OutboxMessageConfiguration.cs
    (KHÔNG move sang Messaging — OutboxMessage entity config giữ trong Persistence)

CHANGES:
  1. File rename only (giữ trong Persistence/Configurations/)
  2. Namespace giữ nguyên: ControlHub.Infrastructure.Persistence.Configurations
  3. Class rename: OutboxMessageConfig → OutboxMessageConfiguration

RATIONALE: OutboxMessage là cross-cutting infrastructure concern,
           không thuộc về một BC cụ thể → config nằm ở Persistence chung.

VERIFY: File OutboxMessageConfiguration.cs tồn tại. File OutboxMessageConfig.cs đã xóa.
```

### Task 4.3 — Move Email Service
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Emails\SmtpEmailSender.cs
    → src\ControlHub.Infrastructure\Messaging\Email\SmtpEmailSender.cs

CHANGES: Namespace: ControlHub.Infrastructure.Emails
                  → ControlHub.Infrastructure.Messaging.Email

VERIFY: File mới tồn tại. File cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Emails\ trống → xóa.
```

### Task 4.4 — Move Outbox Handler
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Outboxs\Handler\EmailOutboxHandler.cs
    → src\ControlHub.Infrastructure\Messaging\Outbox\Handler\EmailOutboxHandler.cs

  src\ControlHub.Infrastructure\Outboxs\Handler\OutboxHandlerFactory.cs
    → src\ControlHub.Infrastructure\Messaging\Outbox\Handler\OutboxHandlerFactory.cs

CHANGES: Namespace: ControlHub.Infrastructure.Outboxs.Handler
                  → ControlHub.Infrastructure.Messaging.Outbox.Handler

VERIFY: 2 files mới. 2 files cũ đã xóa.
```

### Task 4.5 — Move Outbox Repository + Processor
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Outboxs\Repositories\OutboxRepository.cs
    → src\ControlHub.Infrastructure\Messaging\Outbox\Repositories\OutboxRepository.cs

  src\ControlHub.Infrastructure\Outboxs\OutboxProcessor.cs
    → src\ControlHub.Infrastructure\Messaging\Outbox\OutboxProcessor.cs

CHANGES:
  OutboxRepository:  ControlHub.Infrastructure.Outboxs.Repositories
                   → ControlHub.Infrastructure.Messaging.Outbox.Repositories
  
  OutboxProcessor:  ControlHub.Infrastructure.Outboxs
                  → ControlHub.Infrastructure.Messaging.Outbox

VERIFY: 2 files mới. 2 files cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Outboxs\ trống → xóa.
```

### Task 4.6 — Build check Phase 4
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj
EXPECTED: 0 errors.
VERIFY: 0 errors.
```

### Task 4.7 — Commit Phase 4
```
ACTION: git commit -m "refactor: consolidate Messaging infrastructure (Outbox + Email)

        - Move Outboxs/ → Messaging/Outbox/ (fix typo: Outboxs → Outbox)
        - Move Emails/ → Messaging/Email/
        - Namespace update: *.Outboxs.* → *.Messaging.Outbox.*
                           *.Emails.* → *.Messaging.Email.*
        - Rename OutboxMessageConfig → OutboxMessageConfiguration"
```

---

## PHASE 5 — COMMON SERVICES + CLEANUP

**Scope:** Move CurrentUserService, dọn dẹp Services/ folder

### Task 5.1 — Move CurrentUserService
```
FILES TO MOVE:
  src\ControlHub.Infrastructure\Services\CurrentUserService.cs
    → src\ControlHub.Infrastructure\Common\Services\CurrentUserService.cs

ACTION: Tạo folder src\ControlHub.Infrastructure\Common\Services\ trước.

CHANGES: Namespace: ControlHub.Infrastructure.Services
                  → ControlHub.Infrastructure.Common.Services

VERIFY: File mới tồn tại. File cũ đã xóa.
        Folder src\ControlHub.Infrastructure\Services\ trống → xóa.
```

### Task 5.2 — Build check Phase 5
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj
EXPECTED: 0 errors.
```

### Task 5.3 — Commit Phase 5
```
ACTION: git commit -m "refactor: move CurrentUserService to Common/Services

        - Move Services/ → Common/Services/
        - Namespace update: *.Services.* → *.Common.Services.*"
```

---

## PHASE 6 — UPDATE DEPENDENCY INJECTION REGISTRATIONS

**Scope:** Update DependencyInjection/ files để reflect namespace mới
**QUAN TRỌNG:** Đây là phase dễ gây lỗi nhất — phải cẩn thận với fully-qualified type names.

### Task 6.1 — Update AccountExtensions.cs → IdentityExtensions.cs
```
FILE: src\ControlHub.Infrastructure\DependencyInjection\AccountExtensions.cs
ACTION: Rename file → IdentityExtensions.cs

THỰC TẾ — AccountExtensions.cs có đúng các using sau:
  using ControlHub.Application.Accounts.Interfaces;
  using ControlHub.Application.Accounts.Interfaces.Repositories;
  using ControlHub.Application.Common.Interfaces;
  using ControlHub.Application.Users.Interfaces.Repositories;
  using ControlHub.Application.Emails.Interfaces;
  using ControlHub.Domain.Identity.Identifiers.Rules;      ← Domain, GIỮ NGUYÊN
  using ControlHub.Domain.Identity.Identifiers.Services;   ← Domain, GIỮ NGUYÊN
  using ControlHub.Infrastructure.Accounts.Factories;      ← xóa
  using ControlHub.Infrastructure.Accounts.Repositories;   ← xóa
  using ControlHub.Infrastructure.Accounts.Validators;     ← xóa
  using ControlHub.Infrastructure.Emails;                  ← xóa (Phase 4 đã move)
  using ControlHub.Infrastructure.Services;                ← xóa (Phase 5 đã move)
  using ControlHub.Infrastructure.Users.Repositories;      ← xóa

CHANGES:
  1. Namespace trong file: ControlHub.Infrastructure.DependencyInjection (giữ nguyên)
  2. Class rename: AccountExtensions → IdentityExtensions
  3. Method rename: AddControlHubAccounts → AddControlHubIdentity
  4. Update using directives:
     REMOVE: using ControlHub.Infrastructure.Accounts.Factories;
             using ControlHub.Infrastructure.Accounts.Repositories;
             using ControlHub.Infrastructure.Accounts.Validators;
             using ControlHub.Infrastructure.Users.Repositories;
             using ControlHub.Infrastructure.Emails;
             using ControlHub.Infrastructure.Services;
     ADD:    using ControlHub.Infrastructure.Identity.Persistence.Repositories;
             using ControlHub.Infrastructure.Messaging.Email;
             using ControlHub.Infrastructure.Common.Services;

     GIỮ NGUYÊN (Application + Domain namespaces):
             using ControlHub.Application.Accounts.Interfaces;
             using ControlHub.Application.Accounts.Interfaces.Repositories;
             using ControlHub.Application.Common.Interfaces;
             using ControlHub.Application.Users.Interfaces.Repositories;
             using ControlHub.Application.Emails.Interfaces;
             using ControlHub.Domain.Identity.Identifiers.Rules;
             using ControlHub.Domain.Identity.Identifiers.Services;

  5. Update fully-qualified references bên trong method body:
     Infrastructure.Accounts.Repositories.IdentifierConfigRepository
     → Infrastructure.Identity.Persistence.Repositories.IdentifierConfigRepository

VERIFY: File IdentityExtensions.cs tồn tại, compile không lỗi.
        SmtpEmailSender, CurrentUserService, AccountValidator, AccountFactory,
        AccountQueries, AccountRepository, UserRepository, UserQueries,
        CachedIdentifierConfigRepository, IdentifierConfigRepository đều resolved.
```

### Task 6.2 — Update RolePermissionExtensions.cs → AccessControlExtensions.cs
```
FILE: src\ControlHub.Infrastructure\DependencyInjection\RolePermissionExtensions.cs
ACTION: Rename file → AccessControlExtensions.cs

CHANGES:
  1. Class rename: RolePermissionExtensions → AccessControlExtensions
  2. Method rename: AddControlHubRolePermissions → AddControlHubAccessControl
  3. Update using directives:
     THỰC TẾ — RolePermissionExtensions.cs có:
       using ControlHub.Infrastructure.Permissions.Repositories;  ← xóa
       using ControlHub.Infrastructure.Roles.Repositories;        ← xóa
     ADD:    using ControlHub.Infrastructure.AccessControl.Persistence.Repositories;

     GIỮ NGUYÊN (Application + Domain):
       using ControlHub.Application.Common.Settings;
       using ControlHub.Application.Permissions.Interfaces.Repositories;
       using ControlHub.Application.Roles.Interfaces.Repositories;
       using ControlHub.Domain.AccessControl.Services;

  4. Update fully-qualified references trong method body:
     Infrastructure.Permissions.Repositories.PermissionRepository
     → Infrastructure.AccessControl.Persistence.Repositories.PermissionRepository

  ⚠️ NOTE: RolePermissionRepository và RolePermissionQuery có trong codebase
     nhưng KHÔNG được đăng ký trong RolePermissionExtensions.cs — KHÔNG thêm/xóa
     registration nào (G1: no logic changes).

VERIFY: File AccessControlExtensions.cs tồn tại, compile không lỗi.
        RoleRepository, RoleQueries, CachedRoleQueries, CachedRoleRepository,
        PermissionRepository, CachedPermissionRepository, PermissionQueries đều resolved.
```

### Task 6.3 — Update TokenExtensions.cs → TokenManagementExtensions.cs
```
FILE: src\ControlHub.Infrastructure\DependencyInjection\TokenExtensions.cs
ACTION: Rename file → TokenManagementExtensions.cs

CHANGES:
  1. Class rename: TokenExtensions → TokenManagementExtensions
  2. Method rename: AddControlHubTokens → AddControlHubTokenManagement
  3. Update using directives:
     REMOVE: using ControlHub.Infrastructure.Tokens;
             using ControlHub.Infrastructure.Tokens.Generate;
             using ControlHub.Infrastructure.Tokens.Repositories;
             using ControlHub.Infrastructure.Tokens.Sender;
     ADD:    using ControlHub.Infrastructure.TokenManagement.Services;
             using ControlHub.Infrastructure.TokenManagement.Services.Generate;
             using ControlHub.Infrastructure.TokenManagement.Persistence.Repositories;
             using ControlHub.Infrastructure.TokenManagement.Services.Sender;

VERIFY: File TokenManagementExtensions.cs tồn tại, compile không lỗi.
```

### Task 6.4 — Update MessagingExtensions.cs
```
FILE: src\ControlHub.Infrastructure\DependencyInjection\MessagingExtensions.cs
ACTION: Update in-place (không rename file/class)

CHANGES: Update using directives:
  REMOVE: using ControlHub.Infrastructure.Outboxs;
          using ControlHub.Infrastructure.Outboxs.Handler;
          using ControlHub.Infrastructure.Outboxs.Repositories;
  ADD:    using ControlHub.Infrastructure.Messaging.Outbox;
          using ControlHub.Infrastructure.Messaging.Outbox.Handler;
          using ControlHub.Infrastructure.Messaging.Outbox.Repositories;

VERIFY: File compile không lỗi.
```

### Task 6.5 — Update SecurityExtensions.cs
```
FILE: src\ControlHub.Infrastructure\DependencyInjection\SecurityExtensions.cs
ACTION: Update in-place (không rename file/class)

CHANGES: Update using directives:
  REMOVE: using ControlHub.Infrastructure.Accounts.Security;
          using ControlHub.Infrastructure.Authorization.Handlers;
          using ControlHub.Infrastructure.Authorization.Permissions;
          using ControlHub.Infrastructure.Permissions.AuthZ;
          using ControlHub.Infrastructure.Tokens;               ← ConfigureJwtBearerOptions cũ ở đây
  ADD:    using ControlHub.Infrastructure.Identity.Security;
          using ControlHub.Infrastructure.AccessControl.Authorization.Handlers;
          using ControlHub.Infrastructure.AccessControl.Authorization.Permissions;
          (ConfigureJwtBearerOptions, PermissionClaimsTransformation, PermissionPolicyProvider
           đều covered bởi AccessControl.Authorization.Permissions using)

NOTE: `using ControlHub.Infrastructure.Tokens;` được dùng cho ConfigureJwtBearerOptions
      ở codebase hiện tại — sau khi move sang AccessControl.Authorization.Permissions,
      using này phải được xóa và thay bằng AccessControl namespace. Nhưng cần kiểm tra
      có class nào khác trong SecurityExtensions.cs còn dùng Tokens namespace không.

VERIFY: File compile không lỗi. IPasswordHasher, PermissionPolicyProvider, 
        PermissionAuthorizationHandler, SameUserAuthorizationHandler, 
        ConfigureJwtBearerOptions đều resolved.
```

### Task 6.6 — Update entry point ControlHubExtensions.cs
```
FILE: src\ControlHub.Infrastructure\Extensions\ControlHubExtensions.cs
NOTE: File này thuộc CONSTRAINT-G2 "DO NOT TOUCH" — nhưng cần update call sites vì method names đổi.
      Chỉ đổi tên method calls, KHÔNG đổi gì khác. Không thêm using mới vì ControlHubExtensions.cs
      nằm trong namespace ControlHub (global entry), và tất cả extension methods là internal.

THỰC TẾ từ codebase: File này gọi đúng chuỗi sau:
  services
      .AddControlHubDatabase(configuration)
      .AddControlHubSecurity(configuration)
      .AddControlHubTokens(configuration)        ← đổi
      .AddControlHubAccounts()                   ← đổi
      .AddControlHubRolePermissions(configuration) ← đổi
      .AddControlHubMessaging()                  ← GIỮ NGUYÊN (method name không đổi)
      .AddControlHubAi(configuration)            ← DO NOT TOUCH
      .AddControlHubObservability()              ← DO NOT TOUCH
      .AddControlHubPresentation(configuration); ← DO NOT TOUCH

CHANGES (3 dòng duy nhất):
  .AddControlHubTokens(configuration)          → .AddControlHubTokenManagement(configuration)
  .AddControlHubAccounts()                     → .AddControlHubIdentity()
  .AddControlHubRolePermissions(configuration) → .AddControlHubAccessControl(configuration)

VERIFY: Build passes. Không còn reference đến 3 method names cũ trong toàn bộ solution.
```

### Task 6.7 — Update SmtpEmailSender và CurrentUserService usings trong IdentityExtensions.cs
```
THỰC TẾ từ codebase: SmtpEmailSender và CurrentUserService đều được đăng ký trong
AccountExtensions.cs (→ IdentityExtensions.cs), KHÔNG phải MessagingExtensions.cs.

Cụ thể AccountExtensions.cs có 2 using lines liên quan:
  using ControlHub.Infrastructure.Emails;    ← SmtpEmailSender
  using ControlHub.Infrastructure.Services;  ← CurrentUserService

Sau Phase 4 (Emails → Messaging/Email) và Phase 5 (Services → Common/Services),
các using này sẽ broken. Task 6.1 đã bao gồm việc update IdentityExtensions — cần
đảm bảo 2 lines này được xử lý trong Task 6.1 (không phải task riêng).

KIỂM TRA: Confirm Task 6.1 bao gồm:
  REMOVE: using ControlHub.Infrastructure.Emails;
          using ControlHub.Infrastructure.Services;
  ADD:    using ControlHub.Infrastructure.Messaging.Email;
          using ControlHub.Infrastructure.Common.Services;

NOTE về MessagingExtensions.cs: File này chỉ dùng Outbox namespaces, KHÔNG đăng ký SmtpEmailSender.
  → Task 6.4 chỉ cần update Outbox usings. SmtpEmailSender không liên quan.

VERIFY: IdentityExtensions.cs compile không lỗi với SmtpEmailSender và CurrentUserService resolved.
```

### Task 6.8 — Build check Phase 6
```
ACTION: dotnet build src\ControlHub.Infrastructure\ControlHub.Infrastructure.csproj
EXPECTED: 0 errors.
VERIFY: 0 errors.
```

### Task 6.9 — Commit Phase 6
```
ACTION: git commit -m "refactor: update DI registrations to reflect new DDD BC namespaces

        - Rename AccountExtensions → IdentityExtensions (method: AddControlHubIdentity)
        - Rename RolePermissionExtensions → AccessControlExtensions (method: AddControlHubAccessControl)
        - Rename TokenExtensions → TokenManagementExtensions (method: AddControlHubTokenManagement)
        - Update SecurityExtensions: Argon2 → Identity.Security, AuthZ → AccessControl.Authorization
        - Update all using directives to new namespace paths
        - Update call sites in entry point"
```

---

## PHASE 7 — FULL SOLUTION VERIFICATION

### Task 7.1 — Full solution build
```
ACTION: dotnet build src\ControlHub.sln

EXPECTED: Build succeeded, 0 error(s).

NẾU CÓ LỖI: Thường là các project khác (Application, API) có using/reference đến
             Infrastructure namespace cũ thông qua DI extension methods.
             
             Các lỗi điển hình:
             - API project gọi extension method với tên cũ
               → Fix: Update call site trong Program.cs hoặc Startup.cs của API
             
             - Application layer có interface với implementation namespace cũ
               → KHÔNG xảy ra nếu các interface không change
               → Nếu xảy ra, check lại Task 6 có sót file nào không

VERIFY: 0 errors across all projects.
```

### Task 7.2 — Verify AppDbContext still scans all configurations
```
ACTION: Kiểm tra AppDbContext.cs:
        cat src\ControlHub.Infrastructure\Persistence\AppDbContext.cs | grep -A5 "OnModelCreating"

EXPECTED: Phải có: modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)
          Nếu có explicit calls kiểu ApplyConfiguration<AccountConfig>() → phải update thành AccountConfiguration.

VERIFY: ApplyConfigurationsFromAssembly đang dùng, KHÔNG có hardcoded config class references.
```

### Task 7.3 — Verify no orphan folders
```
ACTION: Liệt kê tất cả folders trong Infrastructure (chỉ level 1):
        dir src\ControlHub.Infrastructure\ /AD /B

EXPECTED FOLDERS (chỉ các folders này):
  AccessControl
  AI
  Common
  DependencyInjection
  Identity            ← chứa Identity/Persistence/ và Identity/Security/ (KHÔNG có Identity/Validators/)
  Logging
  Messaging
  Migrations
  Persistence
  RealTime
  TokenManagement

UNEXPECTED FOLDERS (phải không còn tồn tại):
  Accounts, Authorization, Emails, Outboxs, Permissions,
  RolePermissions, Roles, Services, Tokens, Users

VERIFY: Không còn folder nào trong danh sách UNEXPECTED.
```

### Task 7.4 — Final commit và merge
```
ACTION: git commit -m "refactor: verify Infrastructure DDD BC restructure complete

        Final verification:
        - Full solution build: 0 errors
        - AppDbContext using ApplyConfigurationsFromAssembly
        - No orphan entity-centric folders remaining"

        git checkout main
        git merge refactor/infrastructure-ddd-structure
        git branch -d refactor/infrastructure-ddd-structure

VERIFY: git log --oneline -7 hiển thị 7 commits refactor theo đúng thứ tự Phase.
```

---

## QUICK REFERENCE: NAMESPACE MAPPING TABLE

| Old Namespace | New Namespace |
|---|---|
| `ControlHub.Infrastructure.Accounts.Factories` | `ControlHub.Infrastructure.Identity.Persistence.Repositories` |
| `ControlHub.Infrastructure.Accounts.Repositories` | `ControlHub.Infrastructure.Identity.Persistence.Repositories` |
| `ControlHub.Infrastructure.Accounts.Validators` | `ControlHub.Infrastructure.Identity.Persistence.Repositories` (AccountValidator only — see note) |
| `ControlHub.Infrastructure.Accounts.Security` | `ControlHub.Infrastructure.Identity.Security` |
| `ControlHub.Infrastructure.Users.Repositories` | `ControlHub.Infrastructure.Identity.Persistence.Repositories` |
| `ControlHub.Infrastructure.Roles.Repositories` | `ControlHub.Infrastructure.AccessControl.Persistence.Repositories` |
| `ControlHub.Infrastructure.Permissions.Repositories` | `ControlHub.Infrastructure.AccessControl.Persistence.Repositories` |
| `ControlHub.Infrastructure.RolePermissions` | `ControlHub.Infrastructure.AccessControl.Persistence.Repositories` |
| `ControlHub.Infrastructure.Authorization.Handlers` | `ControlHub.Infrastructure.AccessControl.Authorization.Handlers` |
| `ControlHub.Infrastructure.Authorization.Permissions` | `ControlHub.Infrastructure.AccessControl.Authorization.Permissions` |
| `ControlHub.Infrastructure.Permissions.AuthZ` | `ControlHub.Infrastructure.AccessControl.Authorization.Permissions` |
| `ControlHub.Infrastructure.Tokens` (ConfigureJwtBearerOptions only) | `ControlHub.Infrastructure.AccessControl.Authorization.Permissions` |
| `ControlHub.Infrastructure.Tokens` | `ControlHub.Infrastructure.TokenManagement.Services` |
| `ControlHub.Infrastructure.Tokens.Generate` | `ControlHub.Infrastructure.TokenManagement.Services.Generate` |
| `ControlHub.Infrastructure.Tokens.Repositories` | `ControlHub.Infrastructure.TokenManagement.Persistence.Repositories` |
| `ControlHub.Infrastructure.Tokens.Sender` | `ControlHub.Infrastructure.TokenManagement.Services.Sender` |
| `ControlHub.Infrastructure.Outboxs` | `ControlHub.Infrastructure.Messaging.Outbox` |
| `ControlHub.Infrastructure.Outboxs.Handler` | `ControlHub.Infrastructure.Messaging.Outbox.Handler` |
| `ControlHub.Infrastructure.Outboxs.Repositories` | `ControlHub.Infrastructure.Messaging.Outbox.Repositories` |
| `ControlHub.Infrastructure.Emails` | `ControlHub.Infrastructure.Messaging.Email` |
| `ControlHub.Infrastructure.Services` | `ControlHub.Infrastructure.Common.Services` |
| `ControlHub.Infrastructure.Persistence.Configurations` (Identity) | `ControlHub.Infrastructure.Identity.Persistence.Configurations` |
| `ControlHub.Infrastructure.Persistence.Configurations` (AccessControl) | `ControlHub.Infrastructure.AccessControl.Persistence.Configurations` |
| `ControlHub.Infrastructure.Persistence.Configurations` (Token) | `ControlHub.Infrastructure.TokenManagement.Persistence.Configurations` |
| `ControlHub.Infrastructure.Permissions` | `ControlHub.Infrastructure.AccessControl.Persistence.Repositories` |
| `ControlHub.Infrastructure.RolePermissions.Repositories` | `ControlHub.Infrastructure.AccessControl.Persistence.Repositories` |

## CLASS RENAME TABLE (EF Configurations only)

| Old Class Name | New Class Name | Location |
|---|---|---|
| `AccountConfig` | `AccountConfiguration` | Identity/Persistence/Configurations/ |
| `IdentifierConfig` (EF class) | `IdentifierConfiguration` | Identity/Persistence/Configurations/ |
| `UserConfig` | `UserConfiguration` | Identity/Persistence/Configurations/ |
| `PermissionConfig` | `PermissionConfiguration` | AccessControl/Persistence/Configurations/ |
| `RoleConfig` | `RoleConfiguration` | AccessControl/Persistence/Configurations/ |
| `RolePermissionConfig` | `RolePermissionConfiguration` | AccessControl/Persistence/Configurations/ |
| `TokenConfig` | `TokenConfiguration` | TokenManagement/Persistence/Configurations/ |
| `OutboxMessageConfig` | `OutboxMessageConfiguration` | Persistence/Configurations/ (giữ nguyên chỗ) |

## DI METHOD RENAME TABLE

| Old Method | New Method | File |
|---|---|---|
| `AddControlHubAccounts()` | `AddControlHubIdentity()` | IdentityExtensions.cs |
| `AddControlHubRolePermissions()` | `AddControlHubAccessControl()` | AccessControlExtensions.cs |
| `AddControlHubTokens()` | `AddControlHubTokenManagement()` | TokenManagementExtensions.cs |

---

*Tổng số commits: 7 (1 per Phase)*
*Tổng số files di chuyển: ~52 files (bao gồm RolePermissionChangedHandler, ConfigureJwtBearerOptions từ Tokens/)*
*Tổng thời gian ước tính (AI Agent): 20-30 phút*
*Zero logic changes — pure structural refactor*

## CHANGELOG SO VỚI PLAN V4 (V5 Corrections — sau audit lần 5)

| # | Vấn đề trong Plan V4 | Sửa trong Plan V5 |
|---|---|---|
| 1 | Task 6.6 mơ hồ — dùng grep tìm entry point thay vì ghi cụ thể | Thay bằng nội dung thực tế: `ControlHubExtensions.cs` 3 dòng thay. `.AddControlHubMessaging()` GIỮ NGUYÊN |
| 2 | Task 6.7 nhắm sai file — ghi MessagingExtensions đăng ký SmtpEmailSender, thực tế AccountExtensions | Sửa Task 6.7 giải thích đúng, xử lý trong Task 6.1 |
| 3 | Task 6.1 thiếu: `using ControlHub.Infrastructure.Emails` và `using ControlHub.Infrastructure.Services` | Thêm vào REMOVE, ADD: Messaging.Email + Common.Services |
| 4 | Task 6.1 không liệt kê Application/Domain usings cần GIỮ NGUYÊN | Bổ sung đầy đủ |
| 5 | Task 6.2 thiếu Application/Domain usings cần GIỮ NGUYÊN + missing closing fence | Bổ sung và fix |
## CHANGELOG SO VỚI PLAN V1

| # | Vấn đề trong Plan V1 | Sửa trong Plan V2 |
|---|---|---|
| 1 | Thiếu `Accounts/Security/` (Argon2PasswordHasher, Argon2Options) | Thêm Task 1.7 — Move Identity/Security |
| 2 | Thiếu `Authorization/` folder (PermissionAuthorizationHandler, SameUserAuthorizationHandler, PermissionPolicyProvider, ConfigureJwtBearerOptions) | Thêm Task 2.6 — Move AccessControl/Authorization |
| 3 | Thiếu `Permissions/AuthZ/` (PermissionClaimsTransformation) | Gộp vào Task 2.6 → AccessControl/Authorization/Permissions |
| 4 | Thiếu `TokenGeneratorBase.cs` trong Tokens/Generate/ | Thêm vào Task 3.4 |
| 5 | AS-IS không liệt kê `SecurityExtensions.cs` trong DependencyInjection | Sửa AS-IS tree + thêm Task 6.5 — Update SecurityExtensions |
| 6 | Plan cũ có `AuthExtensions.cs` (không tồn tại thực tế) | Đã xóa khỏi DI list |
| 7 | CONSTRAINT-G2 không list đầy đủ files không touch | Mở rộng danh sách |
| 8 | Task 7.3 UNEXPECTED list thiếu `Authorization` folder | Đã thêm |

## CHANGELOG SO VỚI PLAN V3 (V4 Corrections — sau audit lần 4)

| # | Vấn đề trong Plan V3 | Sửa trong Plan V4 |
|---|---|---|
| 1 | AS-IS tree liệt kê `EmailIdentifierValidator`, `UsernameIdentifierValidator`, `PhoneIdentifierValidator`, `DynamicIdentifierValidator` trong `Accounts/Validators/` Infrastructure — SAI. Thực tế chúng nằm ở `ControlHub.Domain/Identity/Identifiers/Rules/` và `Services/` với namespace `ControlHub.Domain.*` | Xóa 4 files này khỏi AS-IS Infrastructure tree, thêm chú thích rõ ràng |
| 2 | TO-BE tree có folder `Identity/Validators/` chứa 4 files trên — folder này không nên tồn tại | Xóa `Identity/Validators/` khỏi TO-BE tree hoàn toàn |
| 3 | Task 1.1 tạo folder `Identity\Validators\` — không cần | Xóa khỏi Task 1.1 |
| 4 | Task 1.5 move 5 files trong đó có 4 Domain files — SAI | Sửa thành chỉ move `AccountValidator.cs`, giữ nguyên 4 Domain validators |
| 5 | Task 6.1 thêm `using ControlHub.Infrastructure.Identity.Validators;` — namespace không tồn tại | Xóa dòng này; giữ nguyên `using ControlHub.Domain.Identity.Identifiers.Rules;` |
| 6 | Namespace mapping table có dòng `Accounts.Validators → Identity.Validators` — misleading | Sửa thành `Accounts.Validators → Identity.Persistence.Repositories` (AccountValidator only) |
| 7 | File count V3: ~57 — sai do count 3 Domain files | Sửa thành ~51 files |
| 8 | `ConfigureJwtBearerOptions.cs` được ghi là ở `Authorization/Permissions/` — SAI. Thực tế ở `Tokens/` root (namespace `ControlHub.Infrastructure.Tokens`) | Sửa AS-IS source path, Task 2.6, Task 3.5, Task 6.5, namespace mapping |
| 9 | `RolePermissionChangedHandler.cs` hoàn toàn vắng mặt (nằm ở `Authorization/Handlers/`) | Thêm vào AS-IS, TO-BE, Task 2.6 |

| # | Vấn đề trong Plan V2 | Sửa trong Plan V3 |
|---|---|---|
| 1 | AS-IS `Permissions/` thiếu 2 files: `PermissionConfig.cs` và `PermissionValidator.cs` nằm ở root level | Đã thêm vào AS-IS tree |
| 2 | Task 2.2 source path sai: `PermissionConfig.cs` được ghi là ở `Persistence/Configurations/`, thực tế ở `Permissions/` | Sửa source path Task 2.2 |
| 3 | `RolePermissions/` thiếu `Repositories/` subfolder với 2 files: `RolePermissionRepository.cs`, `RolePermissionQuery.cs` | Đã thêm vào AS-IS + sửa Task 2.5 |
| 4 | `PermissionValidator.cs` hoàn toàn vắng mặt khỏi plan | Thêm vào TO-BE + Task 2.4 |
| 5 | Namespace mapping table thiếu: `ControlHub.Infrastructure.Permissions` (root) và `ControlHub.Infrastructure.RolePermissions.Repositories` | Thêm 2 dòng vào mapping table |
