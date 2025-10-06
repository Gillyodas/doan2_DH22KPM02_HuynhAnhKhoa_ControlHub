namespace ControlHub.Domain.Outboxs
{
    public enum OutboxMessageType
    {
        Email = 1,
        Sms = 2,
        PushNotification = 3,
        Webhook = 4
    }
}