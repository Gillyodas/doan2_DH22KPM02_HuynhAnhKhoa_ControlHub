# ADR 005: Sử dụng Enum để phân loại IdentifierType trong tầng Domain

## Trạng thái
**Nháp (Draft)** — Chưa hoàn thiện, sẽ được cập nhật sau.

## Bối cảnh (Context)

Trong ControlHub, một `Account` có thể đăng nhập bằng nhiều loại định danh khác nhau: email, số điện thoại, username, hoặc một trường tùy chỉnh do người dùng tự định nghĩa (custom identifier).

Hệ thống cần một cách để **phân biệt** các loại định danh này, đặc biệt trong các trường hợp:
- Xác định validator nào sẽ xử lý một identifier (chiến lược Strategy Pattern).
- Lưu trữ và truy vấn identifier theo loại trong database.
- Kiểm soát logic nghiệp vụ riêng theo từng loại (ví dụ: chuẩn hóa email về lowercase, chuẩn hóa số điện thoại theo E.164).

## Quyết định (Decision)

Sử dụng một `enum` tên `IdentifierType` trong tầng Domain để phân loại identifier:

```csharp
public enum IdentifierType
{
    Email    = 0,
    Phone    = 1,
    Username = 2,
    Custom   = 99,
}
```

Giá trị `Custom = 99` được đặt cách biệt với các giá trị mặc định để dễ dàng mở rộng dải số cho các loại built-in trong tương lai mà không gây xung đột thứ tự.

## Lý do (Rationale)

1. **Phân biệt rõ built-in vs custom**: Ba loại `Email`, `Phone`, `Username` là các identifier có sẵn với logic validate cứng (hard-coded regex, chuẩn hóa). `Custom` là loại mở, được điều khiển bởi `IdentifierConfig` được lưu trong database.

2. **Tích hợp tự nhiên với Strategy Pattern**: `IIdentifierValidator` dùng thuộc tính `Type` để DI container resolve đúng validator. Enum cho phép ánh xạ `1-1` giữa loại và implementation mà không cần reflection hay magic string.

3. **Đơn giản cho persistence**: EF Core lưu enum dưới dạng `int`, không cần conversion phức tạp. Truy vấn theo loại identifier trở thành một phép so sánh số nguyên.

4. **Tầng Domain thuần túy**: Enum là primitive của C#, không kéo theo dependency ngoài nào, phù hợp với nguyên tắc Domain thuần.

## Vấn đề còn tồn tại (Known Issues)

> Phần này chưa hoàn thiện và sẽ được cập nhật.

- **Không extensible tại runtime**: Enum là compile-time constant. Không thể thêm loại identifier mới mà không rebuild. Nếu sau này muốn hỗ trợ loại built-in mới (ví dụ: `OAuth`, `Passkey`), phải thay đổi code.

- **`Custom = 99` là một hack**: Tất cả identifier do người dùng tự cấu hình đều dùng chung giá trị `Custom`. Điều này có nghĩa là không thể phân biệt hai custom identifier khác nhau chỉ bằng `IdentifierType` — cần dựa thêm vào `IdentifierConfig.Id` hoặc tên.

- **Ranh giới trách nhiệm chưa rõ**: Chưa rõ `IdentifierType` nên quyết định toàn bộ validation flow hay chỉ là gợi ý để chọn validator. Trường hợp `Custom`, validator thực sự là `DynamicIdentifierValidator` được điều khiển bởi `IdentifierConfig` — mối quan hệ này chưa được mô hình hóa tường minh trong Domain.

## Trade-off (Đổi chác)

| Lợi ích | Hạn chế |
|---|---|
| Đơn giản, dễ đọc | Không extensible không cần rebuild |
| Không có dependency ngoài | Tất cả custom type đều "nhìn giống nhau" |
| Tích hợp tốt với EF Core và Strategy Pattern | Logic phân biệt custom identifier bị rò rỉ sang Infrastructure |

## Hướng có thể cải thiện (Future Consideration)

> Chưa quyết định — cần nghiên cứu thêm.

- Thay enum bằng một **Value Object** `IdentifierType` có thể mang thêm metadata (tên, loại validation strategy).
- Sử dụng **discriminated union** (thông qua abstract record hoặc OneOf) để mô hình hóa tường minh sự khác biệt giữa built-in và custom.
- Tách `BuiltInIdentifierType` và `CustomIdentifierType` thành hai type riêng, sau đó dùng union type để biểu diễn `IdentifierType`.
