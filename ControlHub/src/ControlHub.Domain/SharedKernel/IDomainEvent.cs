using MediatR;

namespace ControlHub.Domain.SharedKernel
{
    public interface IDomainEvent : INotification
    { 
        DateTime OccurredOn { get; }
    }
}
