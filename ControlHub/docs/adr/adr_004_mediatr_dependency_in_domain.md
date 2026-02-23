# ADR 004: Sử dụng thư viện MediatR trong tầng Domain

## Trạng thái
**Đã phê duyệt (Approved)**

## Bối cảnh (Context)
Theo nguyên tắc Domain-Driven Design (DDD) thuần túy, tầng **Domain** nên là "Pure Code" (POCO - Plain Old CLR Objects), tức là không phụ thuộc vào bất kỳ thư viện hoặc framework bên ngoài nào.

Tuy nhiên, để cơ chế Domain Events hoạt động hiệu quả, chúng ta cần một `Dispatcher`. Trong ControlHub, chúng ta đang sử dụng `MediatR` và để `IDomainEvent` kế thừa `MediatR.INotification`.

## Quyết định (Decision)
ControlHub chấp nhận **phụ thuộc nhẹ (Pragmatic Coupling)** vào MediatR ngay tại tầng Domain.

Cụ thể:
- `ControlHub.Domain` sẽ cài đặt NuGet package `MediatR`.
- `IDomainEvent` kế thừa `INotification`.

## Lý do (Rationale)
1.  **Giảm Boilerplate**: Tận dụng `INotification` giúp chúng ta không phải tự viết hệ thống Dispatcher/Observer pattern phức tạp và dễ phát sinh lỗi.
2.  **Tính ổn định cao**: MediatR là một thư viện chuẩn công nghiệp (De facto standard) trong cộng đồng .NET, rủi ro thư viện này bị khai tử hoặc thay đổi đột ngột là rất thấp.
3.  **Tích hợp tốt**: MediatR tích hợp sâu với .NET Dependency Injection, giúp việc xử lý các Scoped services (như DbContext, IMemoryCache) trong EventHandlers trở nên cực kỳ đơn giản.

## Trade-off (Đổi chác)
*   **Vi phạm Purity**: Tầng Domain không còn 100% độc lập. Nếu sau này muốn chuyển sang một thư viện khác không dùng MediatR, chúng ta buộc phải sửa code trong tầng Domain.

## Cách tiếp cận nếu muốn khắc phục (Purist Approach)
Nếu trong tương lai yêu cầu tính "Pure" tuyệt đối, có thể sửa lại theo hướng:
1.  Định nghĩa `IDomainEvent` không có interface cha.
2.  Tại tầng **Infrastructure**, viết một `MediatRDecorator` hoặc `Adapter` để bọc Domain Event lại thành một `INotification` trước khi dispatch.

Tuy nhiên, với quy mô hiện tại của dự án, sự phức tạp thêm vào của giải pháp "Purist" là không cần thiết (Over-engineering).
