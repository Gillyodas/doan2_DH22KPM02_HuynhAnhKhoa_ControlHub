using System.Text.Json.Serialization;

namespace ControlHub.SharedKernel.Common.DTOs
{
    // DTO dùng chung cho tất cả các API phân trang
    public class PagedResult<T>
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<T> Items { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageIndex")]
        public int PageIndex { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        // Tính toán số trang (Optional - có thể tính ở FE hoặc BE)
        [JsonPropertyName("totalPages")]
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public PagedResult(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}
