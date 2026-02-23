# ADR 003: Chiến lược Dispatch Domain Events: Before vs After SaveChanges

## Trạng thái
**Đã phê duyệt (Approved)**

## Bối cảnh (Context)
Khi tích hợp Domain Events vào `UnitOfWork`, có một quyết định quan trọng về thời điểm (Timing) để phát tán sự kiện qua Mediator. Có hai hướng chính:
1.  **Before `SaveChangesAsync()`**: Dispatch sự kiện trước khi dữ liệu được ghi xuống DB.
2.  **After `SaveChangesAsync()`**: Dispatch sự kiện sau khi DB đã persist thành công.

Cả hai đều có trade-offs liên quan đến tính nhất quán và hiệu năng.

## Quyết định (Decision)
ControlHub chọn chiến lược **Before `SaveChangesAsync()`** (Dispatch trước khi lưu).

Cụ thể trong `UnitOfWork.CommitAsync`:
```csharp
await DispatchDomainEventsAsync(ct); // <-- Dispatch tại đây
var changes = await SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
```

## Lý do (Rationale)
1.  **Tính Fail-safe (Atomicity)**: Nếu một EventHandler (ví dụ: Xóa Cache) gặp lỗi và quăng Exception, toàn bộ quá trình `CommitAsync` sẽ dừng lại. Dữ liệu trong Database sẽ không bị thay đổi (Rollback). Điều này đảm bảo trạng thái hệ thống luôn nhất quán: "Dữ liệu không đổi <=> Cache không đổi".
2.  **Tránh "Stale Data" (Lỗi dữ liệu cũ)**: Nếu dispatch sau (sau khi DB đã lưu), có rủi ro là DB lưu xong nhưng app bị crash/lỗi trước khi kịp xóa Cache. Khi đó người dùng sẽ tiếp tục thấy dữ liệu cũ trong khi DB đã có dữ liệu mới.

## Trade-off & Rủi ro
*   **Race Condition Window**: Tồn tại một khoảng thời gian cực ngắn giữa lúc Cache bị xóa và lúc Transaction được Commit. Nếu có một Request khác đọc dữ liệu đúng lúc này, nó sẽ thấy Cache trống -> Query DB -> Lấy được data CŨ -> Cache lại data CŨ đó. Tuy nhiên, Cache có TTL (Time To Live) nên lỗi này sẽ tự được khắc phục (Self-healing).
*   **Hiệu năng**: Các Handler chạy trong cùng transaction của Request chính. Nếu Handler quá chậm, thời gian giữ Lock trên database sẽ kéo dài.

## Giải pháp Tương lai
Nếu hệ thống mở rộng, các tác vụ không critical hoặc tốn thời gian (như gửi email, push notification) nên được chuyển sang **Outbox Pattern** hoặc Background Job để tránh kéo dài transaction.
