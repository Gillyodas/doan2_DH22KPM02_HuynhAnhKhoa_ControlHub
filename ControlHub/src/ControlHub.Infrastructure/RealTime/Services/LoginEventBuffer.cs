using System.Threading.Channels;
using ControlHub.Application.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Services
{
    public class LoginEventBuffer : BackgroundService
    {
        private readonly Channel<AccountSignedInEvent> _channel;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<LoginEventBuffer> _logger;
        private readonly TimeSpan _flushInterval = TimeSpan.FromMilliseconds(1500);

        public LoginEventBuffer(IHubContext<DashboardHub> hubContext, ILogger<LoginEventBuffer> logger)
        {
            _channel = Channel.CreateUnbounded<AccountSignedInEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// �u?c g?i t? DashboardNotificationHandler d? th�m event v�o buffer.
        /// </summary>
        /// 
        public void Enqueue(AccountSignedInEvent evt)
        {
            _channel.Writer.TryWrite(evt);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var batch = new List<AccountSignedInEvent>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_flushInterval);

                    try
                    {
                        while (await _channel.Reader.WaitToReadAsync(cts.Token))
                        {
                            while (_channel.Reader.TryRead(out var evt))
                            {
                                batch.Add(evt);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //TODO: Timeout - flush batch
                    }

                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch, cancellationToken);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LoginEventBuffer"); //TODO: Format log
                }
            }
        }

        private async Task FlushBatchAsync(List<AccountSignedInEvent> batch, CancellationToken cancellationToken)
        {
            var payload = batch.Select(e => new
            {
                e.Timestamp,
                e.IsSuccess,
                e.IdentifierType,
                e.MaskedIdentifier,
                e.FailureReason
            }).ToArray();

            await _hubContext.Clients.All.SendAsync("LoginAttemptsBatch", payload, cancellationToken);
            _logger.LogDebug("Flushed {Count} login events to Dashboard", batch.Count);
        }
    }
}
