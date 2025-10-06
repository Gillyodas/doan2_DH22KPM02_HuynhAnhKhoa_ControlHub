using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.Outboxs;

namespace ControlHub.Application.OutBoxs
{
    public interface IOutboxHandler
    {
        OutboxMessageType Type { get; }
        Task HandleAsync(string payload, CancellationToken ct);
    }
}
