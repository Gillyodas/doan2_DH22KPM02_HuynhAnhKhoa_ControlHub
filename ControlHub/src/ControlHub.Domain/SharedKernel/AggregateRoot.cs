using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Domain.SharedKernel
{
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Danh sách các Domain Events chưa được dispatch.
        /// UnitOfWork sẽ đọc list này khi CommitAsync().
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        /// <summary>
        /// Gọi từ Domain behavior methods (VD: Role.AddPermission()).
        /// Thêm event vào hàng đợi — CHƯA dispatch ngay.
        /// </summary>
        protected void RaiseDomainEvent(IDomainEvent domainEvent)
            => _domainEvents.Add(domainEvent);

        /// <summary>
        /// Gọi bởi UnitOfWork SAU KHI đã dispatch xong.
        /// Xóa events đã xử lý để tránh dispatch lặp.
        /// </summary>
        public void ClearDomainEvents()
            => _domainEvents.Clear();
    }
}
