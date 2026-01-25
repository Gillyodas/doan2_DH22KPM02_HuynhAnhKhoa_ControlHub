# Layer Violation Report - ControlHub.Application

Báo cáo các vấn đề vi phạm phân lớp (Domain Layer và Infrastructure Layer) phát hiện trong các Command và Query handlers của `ControlHub.Application`.

---

## Tổng quan

| Metric | Value |
|--------|-------|
| Command Handlers Reviewed | 18 |
| Query Handlers Reviewed | 4 |
| Domain Layer Violations | 6 |
| Infrastructure Layer Violations | 4 |
| Design Concerns | 3 |

---

## 🔴 Domain Layer Violations

### 1. `ForgotPasswordCommandHandler.cs`
**Location**: [ForgotPasswordCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs#L125-L131)

**Issue**: Business logic cho việc tạo reset link và email payload nằm trong Application layer.

```csharp
// Line 125-131
var resetLink = $"{devBaseUrl}/control-hub/reset-password?token={domainToken.Value}";
var payload = new
{
    To = request.Value,
    Subject = "Reset your password",
    Body = $"Click this link to reset your password: <a href='{resetLink}'>Reset Password</a>"
};
```

**Suggestion**: Di chuyển logic tạo email payload vào một Domain Service hoặc Email Template Service.

---

### 2. `SignOutCommandHandler.cs`
**Location**: [SignOutCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/SignOut/SignOutCommandHandler.cs#L64-L77)

**Issue**: Logic parsing JWT Claims trực tiếp trong handler - đây là Infrastructure concern.

```csharp
// Line 64-66
var accIdString = claim.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? claim.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
```

**Suggestion**: Tạo một `IClaimParser` abstraction trong Application layer và implement trong Infrastructure layer.

---

### 3. `UpdateIdentifierConfigCommandHandler.cs`
**Location**: [UpdateIdentifierConfigCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/UpdateIdentifierConfig/UpdateIdentifierConfigCommandHandler.cs#L64-L66)

**Issue**: Tạo Domain object trực tiếp từ DTO mà không handle failure case.

```csharp
// Line 64-66 - Không kiểm tra result failure
var validationRules = request.Rules.Select(r => 
    ValidationRule.Create(r.Type, r.Parameters, r.ErrorMessage, r.Order).Value
).ToList();
```

**Suggestion**: Cần kiểm tra `IsFailure` cho mỗi `ValidationRule.Create()` call và return error thích hợp.

---

### 4. `RefreshAccessTokenCommandHandler.cs`
**Location**: [RefreshAccessTokenCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/RefreshAccessToken/RefreshAccessTokenCommandHandler.cs#L78)

**Issue**: Sử dụng `DateTime.UtcNow` trực tiếp - hard dependency on system clock.

```csharp
// Line 78
if (refreshToken.ExpiredAt <= DateTime.UtcNow || refreshToken.IsUsed)
```

**Suggestion**: Inject `ISystemClock` hoặc `ITimeProvider` để dễ test và maintain.

---

### 5. `ChangePasswordCommandHandler.cs`
**Location**: [ChangePasswordCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/ChangePassword/ChangePasswordCommandHandler.cs#L105-L116)

**Issue**: Revoke token logic được lặp lại trong handler thay vì được đóng gói trong Domain.

```csharp
// Line 105-116
var tokens = await _tokenRepository.GetTokensByAccountIdAsync(acc.Id, cancellationToken);
if (tokens.Any())
{
    foreach (var token in tokens)
    {
        if (token.IsValid())
        {
            token.Revoke();
        }
    }
}
```

**Suggestion**: Tạo Domain Service `TokenRevocationService` hoặc thêm method `RevokeAllTokens()` vào Account aggregate.

---

### 6. `GetActiveIdentifierConfigsQueryHandler.cs`
**Location**: [GetActiveIdentifierConfigsQueryHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Queries/GetActiveIdentifierConfigs/GetActiveIdentifierConfigsQueryHandler.cs#L66)

**Issue**: Sử dụng `System.Text.Json.JsonSerializer.Deserialize` trực tiếp trong handler.

```csharp
// Line 66
System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(r.ParametersJson) ?? new Dictionary<string, object>()
```

**Suggestion**: Logic JSON deserialization nên được đóng gói trong DTO mapping hoặc Mediator Pipeline.

---

## 🟠 Infrastructure Layer Violations

### 1. `ForgotPasswordCommandHandler.cs` - Configuration Dependency
**Location**: [ForgotPasswordCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs#L117-L123)

**Issue**: Access trực tiếp `IConfiguration` trong Command handler.

```csharp
// Line 117-123
var devBaseUrl = _configuration["BaseUrl:DevBaseUrl"];
if (string.IsNullOrEmpty(devBaseUrl))
{
    _logger.LogError("{@LogCode} | Key: {Key}", CommonLogs.System_ConfigMissing, "BaseUrl:DevBaseUrl");
    return Result.Failure(CommonErrors.SystemConfigurationError);
}
```

**Suggestion**: Tạo strongly-typed options class như `UrlSettings` và inject vào handler thay vì raw `IConfiguration`.

---

### 2. `RegisterAdminCommandHandler.cs` / `RegisterUserCommandHandler.cs` / `RegisterSupperAdminCommandHandler.cs`
**Location**: 
- [RegisterAdminCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/RegisterAdmin/RegisterAdminCommandHandler.cs#L56-L61)
- [RegisterUserCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/RegisterUser/RegisterUserCommandHandler.cs#L57-L62)
- [RegisterSupperAdminCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/RegisterSupperAdmin/RegisterSupperAdminCommandHandler.cs#L47-L54)

**Issue**: Access trực tiếp `IConfiguration` để lấy RoleId.

```csharp
var roleIdString = _config["RoleSettings:AdminRoleId"];
if (!Guid.TryParse(roleIdString, out var userRoleId))
{
    _logger.LogError("{@LogCode} | Value: {Value}", CommonLogs.System_InvalidConfiguration, roleIdString);
    return Result<Guid>.Failure(CommonErrors.SystemConfigurationError);
}
```

**Suggestion**: Tạo `RoleSettings` options class với validation và inject vào handlers.

---

### 3. `SignOutCommandHandler.cs` - JWT Dependencies
**Location**: [SignOutCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/SignOut/SignOutCommandHandler.cs#L1)

**Issue**: Import `System.IdentityModel.Tokens.Jwt` trực tiếp trong Application layer.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
```

**Suggestion**: Các JWT claim types nên được abstract thông qua Application interfaces, không import trực tiếp từ JWT library.

---

### 4. `CreatePermissionsCommandHandler.cs` - Debug Log
**Location**: [CreatePermissionsCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Permissions/Commands/CreatePermissions/CreatePermissionsCommandHandler.cs#L32)

**Issue**: Debug log line nên được remove trong production code.

```csharp
// Line 32
_logger.LogInformation("--- DEBUG: CreatePermissionsCommandHandler.Handle HIT ---");
```

**Suggestion**: Remove debug log hoặc đổi sang `LogDebug` level.

---

## 🟡 Design Concerns

### 1. Inconsistent Error Handling Pattern
**Files affected**:
- `UpdateIdentifierConfigCommandHandler.cs` - Không check failure khi tạo ValidationRule
- `GetActiveIdentifierConfigsQueryHandler.cs` - Không handle exception từ JSON deserialization

**Suggestion**: Implement consistent error handling pattern across all handlers.

---

### 2. Missing Null Checks
**Location**: [SignInCommandHandler.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/SignIn/SignInCommandHandler.cs#L87-L94)

```csharp
if (account.Identifiers == null || !account.Identifiers.Any())
```

**Issue**: Null check cho collection suggest rằng Domain invariant không được enforce properly.

**Suggestion**: Account aggregate nên đảm bảo Identifiers không bao giờ null (có thể empty collection).

---

### 3. Empty File
**Location**: [SignInCommandHandler_Simple.cs](file:///E:/Project/ControlHub/src/ControlHub.Application/Accounts/Commands/SignIn/SignInCommandHandler_Simple.cs)

**Issue**: File rỗng, có thể là leftover code.

**Suggestion**: Remove file nếu không cần thiết.

---

## ✅ Handlers Without Issues

Các handlers sau đây tuân thủ tốt Clean Architecture principles:

| Handler | Status |
|---------|--------|
| `AddIdentifierCommandHandler.cs` | ✅ Clean |
| `CreateIdentifierConfigCommandHandler.cs` | ✅ Clean |
| `ResetPasswordCommandHandler.cs` | ✅ Clean |
| `ToggleIdentifierActiveCommandHandler.cs` | ✅ Clean |
| `AddPermissonsForRoleCommandHandler.cs` | ✅ Clean |
| `CreateRolesCommandHandler.cs` | ✅ Clean |
| `UpdateUsernameCommandHandler.cs` | ✅ Clean |
| `GetIdentifierConfigsQueryHandler.cs` | ✅ Clean |
| `SearchPermissionsQueryHandler.cs` | ✅ Clean |
| `SearchRolesQueryHandler.cs` | ✅ Clean |

---

## Recommended Refactoring Priority

| Priority | Issue | Effort |
|----------|-------|--------|
| 🔴 High | Extract `IConfiguration` usage to Options classes | Medium |
| 🔴 High | Create `IClaimParser` abstraction | Low |
| 🟠 Medium | Create `ISystemClock` abstraction | Low |
| 🟠 Medium | Create `TokenRevocationService` | Medium |
| 🟡 Low | Fix null check pattern for collections | Low |
| 🟡 Low | Remove empty/debug files | Low |
