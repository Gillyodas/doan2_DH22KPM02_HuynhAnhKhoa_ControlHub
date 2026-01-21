namespace ControlHub.SharedKernel
{
    internal class ToDoNote
    {
        // TODO: AI Audit Log Analysis (Phân tích log bằng AI): Sử dụng LLM để phân tích file Log. Thay vì admin phải đọc hàng ngàn dòng log, chỉ cần hỏi: "Hệ thống có hành vi nào bất thường trong 24h qua không?" AI sẽ chỉ ra các IP cố gắng brute-force hoặc các quyền được cấp phát sai quy trình.
        // TODO: Tìm hiểu về Token Refresh tự động (useTokenRefresh)
        // TODO: Document
        // TODO: Implementation Document cho dev
        // TODO: Chuyển tính năng đăng ký tài khoản admin vào sau khi đăng nhập vì cần permission tạo admin
        // TODO: Thêm hướng dẫn sử dụng APIs trên FE
        // TODO: Real-time Dashboard: Sử dụng SignalR (vì bạn làm .NET) để hiển thị biểu đồ active users, login attempts theo thời gian thực.
        // TODO: Thống kê kết quả từ AI và user active ra FE
        // TODO: Thêm cache cho những dữ liệu ít thay đổi và test hiệu năng trên tập dữ liệu lớn
        // TODO: Tìm hiểu về các công nghệ và kỹ thuật cache (mediatR hay Redis)
        // TODO: Sử dụng Domain Events: Khi một Role được cập nhật, phát ra một Event. Một Handler sẽ bắt Event đó và gọi _memoryCache.Remove(key). Cách này "sạch" nhất vì Decorator không cần quan tâm đến logic nghiệp vụ.
        // TODO: Thêm các tool observability để theo dõi tỉ lệ truy cập cache 
    }
}
