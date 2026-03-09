namespace ControlHub.SharedKernel.Common.DTOs
{
    // DTO d�ng chung cho t?t c? c�c API ph�n trang
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        // T�nh to�n s? trang (Optional - c� th? t�nh ? FE ho?c BE)
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
