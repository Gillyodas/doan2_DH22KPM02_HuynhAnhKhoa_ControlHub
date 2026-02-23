# ADR 002: Triển khai Domain Events trong ControlHub

## Trạng thái
**Đã phê duyệt (Approved)**

## Bối cảnh (Context)
Để hỗ trợ các tính năng như Dynamic Authorization (ADR 001) và đảm bảo tính nhất quán dữ liệu (Data Consistency) giữa các Aggregate khác nhau, chúng ta cần một cơ chế để phát đi các sự kiện nghiệp vụ (Domain Events) ngay từ tầng Domain.

Các thách thức cần giải quyết:
1. **Coupling**: Tầng Domain nên "pure" nhất có thể, tránh phụ thuộc vào các thư viện bên ngoài.
2. **Timing**: Khi nào thì nên phát tán các sự kiện (Dispatching) để đảm bảo tính an toàn dữ liệu.
3. **Reliability**: Đảm bảo rằng nếu một sự kiện làm thay đổi trạng thái quan trọng (như xóa Cache), nó phải được thực hiện đồng bộ với việc lưu dữ liệu vào Database.

## Quyết định (Decision)
Chúng ta quyết định triển khai cơ chế Domain Events như sau:

1. **Sử dụng MediatR làm Dispatcher**:
    *   Interface `IDomainEvent` sẽ kế thừa `MediatR.INotification`.
    *   **Lý do**: Đây là marker interface ổn định, giúp tận dụng tối đa sức mạnh của DI container và MediatR mà không cần viết custom dispatcher phức tạp. Chấp nhận sự phụ thuộc nhẹ (coupling) này để đổi lấy tính tiện dụng và giảm boilerplate code.
2. **Kế thừa AggregateRoot**:
    *   Tất cả các Aggregate Root sẽ kế thừa base class `AggregateRoot` có chứa danh sách `IDomainEvent`.
    *   Dùng phương thức `RaiseDomainEvent` để đăng ký sự kiện.
3. **Dispatch tập trung tại UnitOfWork**:
    *   Logic phát tán sự kiện (`DispatchDomainEventsAsync`) sẽ được tích hợp trực tiếp vào phương thức `CommitAsync` của `UnitOfWork`.
    *   Sử dụng EF Core `ChangeTracker` để quét toàn bộ các Aggregate đang được theo dõi và lấy ra các sự kiện chưa được xử lý.
4. **Chiến lược Dispatch: "Before Save Changes" (Fail-safe)**:
    *   Các Domain Events sẽ được dispatch **TRƯỚC KHI** gọi lệnh `SaveChangesAsync()` của EF Core.
    *   **Lý do**: Đảm bảo rằng các side-effects (như xóa cache) được thực thi trong phạm vi transaction của database. Nếu một handler (ví dụ: Cache Invalidation) thất bại và ném ra exception, toàn bộ transaction sẽ bị rollback, đảm bảo dữ liệu trong DB không bị thay đổi trong khi cache đã bị xóa (ổn định trạng thái).

## Hệ quả (Consequences)

### Ưu điểm
*   **Decoupling nghiệp vụ**: UnitOfWork không cần biết handler nào đang lắng nghe sự kiện, giúp tuân thủ nguyên tắc Open-Closed.
*   **Tính nhất quán cao**: Các tác vụ quan trọng như xóa cache được liên kết chặt chẽ với lifecycle của transaction.
*   **Giảm Boilerplate**: Tận dụng MediatR giúp việc thêm sự kiện mới rất nhanh chóng (chỉ cần tạo Event và Handler).

### Nhược điểm & Thách thức
*   **Coupling với MediatR**: Tầng Domain phụ thuộc vào thư viện bên ngoài (tuy nhiên rủi ro thấp do MediatR rất ổn định).
*   **Khả năng mở rộng**: Hiện tại đang dispatch đồng bộ (In-Process). Nếu số lượng handler lớn và tốn thời gian, có thể ảnh hưởng đến performance của request chính. Trong tương lai có thể cần chuyển những tác vụ không critical sang nền (Background) hoặc Outbox Pattern.
