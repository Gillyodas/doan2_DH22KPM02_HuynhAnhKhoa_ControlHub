# ADR 003: Chiến lược Dispatch Domain Events - After SaveChanges

## Trạng thái
**Đã phê duyệt (Approved)**  
**Cập nhật (Revised):** 2026-02-24

## Bối cảnh (Context)
Khi tích hợp Domain Events vào `UnitOfWork`, có một quyết định quan trọng về thời điểm (Timing) để phát tán sự kiện qua Mediator/EventDispatcher. 

Phiên bản trước chọn **Before SaveChangesAsync**, nhưng phân tích thêm cho thấy có **Race Condition Window** giữa lúc cache bị invalidate và lúc transaction chưa commit. Điều này có thể dẫn đến **stale cache data**.

## Quyết định (Decision)
ControlHub **cập nhật chiến lược** để dispatch Domain Events **AFTER SaveChangesAsync và AFTER Transaction Commit** trong `UnitOfWork.CommitAsync`:

```csharp
public async Task<int> CommitAsync(CancellationToken ct = default)
{
    if (_currentTransaction != null)
    {
        return await SaveChangesAsync(ct);
    }
    
    await using var transaction = await _dbContext.Database
        .BeginTransactionAsync(ct);
    try
    {
        _logger.LogInformation("Implicit transaction started");
        
        // Step 1: SaveChanges trước
        var changes = await SaveChangesAsync(ct);
        
        // Step 2: Commit transaction
        await transaction.CommitAsync(ct);
        
        // Step 3: Dispatch events AFTER (NEW!)
        await DispatchDomainEventsAsync(ct);
        
        _logger.LogInformation(
            "Transaction committed successfully with {Changes} changes.",
            changes);
        return changes;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Transaction failed. Rolling back...");
        await SafeRollbackAsync(transaction, ct);
        _dbContext.ChangeTracker.Clear();
        throw MapException(ex);
    }
}
```

## Lý do (Rationale)

### Giải quyết Race Condition Window
**Vấn đề cũ:** Khi dispatch trước commit, tồn tại khoảng thời gian giữa lúc cache bị xóa (T1) và lúc transaction commit (T5):

```
Timeline:
T1: Cache invalidate (RoleCreatedEventHandler)
    Cache "Role:1" trống ❌

T2-T4: Request B khác đến
    → Check cache: miss
    → Query DB: dữ liệu vẫn CŨ (SaveChanges chưa xong)
    → Cache lại dữ liệu CŨ 😱

T5: SaveChanges() + Transaction.Commit()
    Database = {version: NEW}

Result: Cache stale data đến 30 phút (TTL) 🐛
```

**Giải pháp mới:** Với dispatch **AFTER commit**:
- SaveChanges xong → Database update thành công
- Transaction.Commit() xong → Dữ liệu 100% đã lưu
- THEN dispatch events → Cache invalidate
- ✅ Không có race condition window

### Tính Atomicity
Mặc dù event handler chạy ngoài transaction, nhưng điều này là **acceptable** vì:

1. **Business data đã safe trong DB** - SaveChanges + Commit đã thành công
2. **Cache handlers rất nhanh** - Cache invalidation chỉ mất vài milliseconds
3. **Self-healing mechanism** - Nếu handler fail, cache vẫn có TTL để tự expire

Tradeoff này **hợp lý hơn** việc chấp nhận race condition.

### Event Handler Isolation
Event handlers chạy **ngoài transaction scope**, có lợi ích:
- ✅ Không kéo dài database lock nếu handler slow
- ✅ Handler failure không rollback business data
- ✅ Có thể scale handlers độc lập (future: Outbox Pattern)

## Trade-off & Rủi ro

### 1. Event Handler Failure
Nếu một `DomainEventHandler` quăng exception:
```csharp
public class RoleCreatedEventHandler : INotificationHandler<RoleCreatedEvent>
{
    public async Task Handle(RoleCreatedEvent notification, CancellationToken ct)
    {
        // Nếu exception ở đây, transaction đã commit rồi
        // → Data safe nhưng cache không được invalidate
        await _cacheService.InvalidateRoleCache(...);
    }
}
```

**Giải pháp:**
- ✅ Implement retry logic trong handler
- ✅ Logging + monitoring cho handler failures
- ✅ Future: Outbox Pattern với persistent event queue + background processor

### 2. Handler Timeout
Nếu handler chạy rất chậm (network I/O, external API call):
```csharp
// Scenario:
await _cacheService.InvalidateRoleCache(...);  // 5 giây
await _externalService.NotifyAsync(...);       // 10 giây
// → User phải chờ 15s response (chỉ còn 5s là cache issue)
```

**Giải pháp:**
- ✅ Set timeout cho `DispatchDomainEventsAsync` (e.g., 5 seconds)
- ✅ Critical handlers chạy synchronous, non-critical chạy fire-and-forget
- ✅ Future: Outbox Pattern cho non-critical operations

**Current Implementation Best Practice:**
```csharp
// Chỉ handle quick cache operations ở đây
// Slow/non-critical operations → Future Outbox Pattern
public async Task DispatchDomainEventsAsync(CancellationToken ct)
{
    var quickTimeout = TimeSpan.FromSeconds(5);
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(quickTimeout);
    
    try
    {
        foreach (var @event in _domainEvents)
        {
            await _mediator.Publish(@event, cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Domain event dispatch timeout");
        // Log but don't throw - data is safe
    }
}
```

## Sơ đồ Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│ Request đến UnitOfWork.CommitAsync()                       │
└─────────────────────────────────────────┬───────────────────┘
                                          │
                                          ▼
                         ┌─────────────────────────────────┐
                         │ Check _currentTransaction       │
                         │ (nested transaction check)      │
                         └──────────┬──────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
         (explicit tx exists)            (no explicit tx)
              Return changes                 ▼
                                   ┌──────────────────────────┐
                                   │ Begin Transaction        │
                                   └──────────────┬───────────┘
                                                  │
                                                  ▼
                         ┌────────────────────────────────────┐
                         │ Collect Domain Events from         │
                         │ ChangeTracker (AggregateRoot)      │
                         └──────────────┬─────────────────────┘
                                        │
                                        ▼
                         ┌────────────────────────────────────┐
                         │ SaveChangesAsync()                 │
                         │ → Insert/Update/Delete to DB       │
                         └──────────────┬─────────────────────┘
                                        │
                                        ▼
                         ┌────────────────────────────────────┐
                         │ Transaction.CommitAsync()          │
                         │ → ACID commit to database          │
                         │ → Data 100% persisted ✅           │
                         └──────────────┬─────────────────────┘
                                        │
                                        ▼
                         ┌────────────────────────────────────┐
                         │ DispatchDomainEventsAsync()  (NEW) │
                         │ → Cache invalidation               │
                         │ → Event notifications              │
                         │ (outside transaction scope)        │
                         └──────────────┬─────────────────────┘
                                        │
                                        ▼
                         ┌────────────────────────────────────┐
                         │ Return changes count               │
                         │ (Success response)                 │
                         └────────────────────────────────────┘
```

## Khi nào dùng Pattern này

### ✅ Phù hợp cho:
1. **Cache Invalidation** (hiện tại)
   - Handler nhanh (< 100ms)
   - Không critical nếu delay vài ms
   
2. **Real-time notifications** (một số trường hợp)
   - In-process event handlers
   - Không yêu cầu guaranteed delivery

3. **Audit logging** (log to memory)
   - Fire-and-forget
   - Loss acceptable

### ❌ Không phù hợp cho:
1. **Critical notifications** (Email, SMS)
   - Cần guarantee delivery
   - **→ Use Outbox Pattern**

2. **External API calls** (long-running)
   - Block response time
   - **→ Use Outbox + Background Job**

3. **Distributed systems** (multiple instances)
   - IMemoryCache local only
   - **→ Use Redis + Outbox**

## Migration Path (Tương lai)

Khi hệ thống mở rộng, có thể nâng cấp sang **Outbox Pattern**:

```
Phase 1 (Hiện tại):
  Domain Events → Immediate Dispatch in UnitOfWork
  (Best for cache invalidation)

Phase 2 (Future):
  Domain Events → Outbox Table
  Background Processor → Dispatch từ Outbox
  (Best for critical operations + guaranteed delivery)

Phase 3 (Enterprise):
  Domain Events → Message Broker (RabbitMQ/Kafka)
  Multiple subscribers → Different handlers
  (Best for distributed systems)
```

## Summary

| Aspect | Chiến lược cũ | Chiến lược mới |
|--------|---|---|
| Timing | **Before** SaveChanges | **After** Commit ✅ |
| Race Condition | ❌ Tồn tại window | ✅ Eliminated |
| Atomicity | ✅ Strict | ~ Flexible |
| Handler Isolation | ❌ In-transaction | ✅ Out-of-transaction |
| DB Lock Duration | ❌ Kéo dài | ✅ Minimal |
| Implementation | Simple | Simple ✅ |
| Suited for | Non-cache ops | **Cache invalidation** |

---

**Effective Date:** 2026-02-24  
**Updated By:** [Khoa]  
**Reviewed By:** [Reviewer]