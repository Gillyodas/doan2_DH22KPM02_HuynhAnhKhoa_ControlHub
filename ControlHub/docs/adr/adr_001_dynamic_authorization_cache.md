# ADR 001: Sử dụng IMemoryCache và Domain Events cho Dynamic Authorization với JWT

## Trạng thái
**Đã phê duyệt (Approved)**

## Bối cảnh (Context)
Trong hệ thống ControlHub, chúng ta cần triển khai tính năng **Dynamic Authorization** (Phân quyền động). Điều này cho phép Administrator thay đổi Permission của Role hoặc gán Role cho User mà không cần restart ứng dụng hoặc yêu cầu User login lại.

Tuy nhiên, việc sử dụng **JWT (JSON Web Token)** đặt ra một thách thức:
1. **JWT là Stateless**: Các thông tin về quyền (claims) thường được đóng gói cứng vào token khi login. Nếu quyền thay đổi dưới Database, token cũ vẫn còn hiệu lực với quyền cũ cho đến khi hết hạn (TTL).
2. **Performance**: Nếu mỗi request đều truy vấn Database để kiểm tra quyền mới nhất, hệ thống sẽ gặp bottleneck về I/O.
3. **Consistency**: Cần đảm bảo quyền được kiểm tra là chính xác và phản ánh đúng trạng thái thực tế của hệ thống.

## Quyết định (Decision)
Để cân bằng giữa tính **Stateless** của JWT và sự linh hoạt của **Dynamic Authorization**, chúng ta quyết định:

1.  **Không lưu Permission cụ thể trong JWT Claims**: JWT chỉ lưu các thông tin định danh cơ bản (Sub, Email, Jti) và có thể là RoleId.
2.  **Sử dụng IMemoryCache của Microsoft**:
    *   Sử dụng `IMemoryCache` để lưu trữ danh sách Permission của từng User/Role ngay tại bộ nhớ của Application Server.
    *   Khi một request đến, `AuthorizationHandler` sẽ kiểm tra trong Cache trước. Nếu không có (Cache Miss), mới truy vấn Database và nạp vào Cache.
3.  **Áp dụng Domain Events để đảm bảo Reliability**:
    *   Mỗi khi có sự thay đổi quyền trong Domain (ví dụ: `Role.AddPermission`, `User.AssignRole`), một **Domain Event** tương ứng sẽ được raise.
    *   Một `DomainEventHandler` sẽ lắng nghe các event này và thực hiện **Cache Invalidation** (xóa cache cũ) hoặc **Cache Update**.
    *   Việc này đảm bảo tính nhất quán dữ liệu (Data Consistency) giữa Database và Cache mà không cần đợi Cache TTL hết hạn.

## Hệ quả (Consequences)

### Ưu điểm
*   **Hiệu năng cao**: Truy xuất quyền từ memory cực nhanh, giảm tải cho Database.
*   **Linh hoạt**: Thay đổi quyền có hiệu lực ngay lập tức (Near real-time) nhờ cơ chế Domain Events.
*   **Bảo mật**: Giảm rủi ro từ việc sử dụng token chứa quyền cũ đã bị thu hồi.

### Nhược điểm & Thách thức
*   **Memory Usage**: Cần quản lý kích thước Cache để tránh tràn bộ nhớ nếu số lượng User/Role quá lớn (có thể sử dụng `SizeLimit` hoặc `SlidingExpiration`).
*   **Distributed Environment**: Trong môi trường cân bằng tải (Load Balancing) với nhiều instance, `IMemoryCache` chỉ có tác dụng cục bộ. Nếu cần mở rộng scale-out, chúng ta sẽ cần chuyển sang **Distributed Cache** như **Redis** (mô hình này vẫn giữ nguyên logic Domain Events nhưng thay đổi provider).
