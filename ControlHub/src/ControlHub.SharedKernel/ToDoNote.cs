namespace ControlHub.SharedKernel
{
    internal class ToDoNote
    {
        // TODO: Them cac tool observability de theo doi ti le truy cap cache
        // TODO: Xoa bot 1 method du thua cua UserRepository
        // TODO: Tim hieu ve Token Refresh tu dong (useTokenRefresh)

        // =====================================================================
        // TODO: [Fire-and-Forget trong SignInCommandHandler]
        // 
        // PROBLEM:
        //   O SignInCommandHandler, chung ta dung '_ = PublishLoginEvent(...)' 
        //   de fire-and-forget event len Dashboard (MediatR -> SignalR).
        //   Neu publisher throw exception, exception bi nuot im lang (silently swallowed)
        //   -> khong co log, khong ai biet event that bai.
        //
        // TAI SAO PATTERN HIEN TAI HAY (can tim hieu sau):
        //   1. Fire-and-Forget ('_ = Task'): Khong block response tra ve client.
        //      Login response luon nhanh vi khong doi SignalR broadcast xong.
        //   2. Channel<T> Buffer (LoginEventBuffer): Thay vi moi login event 
        //      goi SignalR ngay (N requests = N broadcasts), dung Channel<T> 
        //      batch lai -> flush moi 2 giay. Giam tai SignalR hub, giam network traffic.
        //      Channel<T> la thread-safe, high-performance producer-consumer.
        //
        // SOLUTION CAN LAM:
        //   - Wrap fire-and-forget trong try-catch de log exception neu co
        //   - Hoac chuyen sang BackgroundService / IHostedService de xu ly reliable hon
        //   - Xem xet dung Outbox Pattern (da co san trong project) neu can guarantee delivery
        // =====================================================================

        // =====================================================================
        // TODO: [Cache Invalidation - Domain Events cho Dynamic Authorization]
        //
        // PROBLEM: 
        //   CachedRoleRepository cache Role + Permissions toi da 30 phut.
        //   Neu Admin thay doi permission, user van giu quyen cu toi da 30 phut.
        //   -> Lo hong bao mat trong Authorization system.
        //
        // SOLUTION:
        //   Trien khai Domain Events trong Role Aggregate Root.
        //   Khi AddPermission/ClearPermissions -> raise RolePermissionChangedEvent
        //   -> Handler invalidate IMemoryCache -> next request load fresh tu DB
        // =====================================================================
    }
}
