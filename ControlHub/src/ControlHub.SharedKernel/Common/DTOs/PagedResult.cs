using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Common.DTOs
{
    // DTO dùng chung cho tất cả các API phân trang
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        // Tính toán số trang (Optional - có thể tính ở FE hoặc BE)
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
