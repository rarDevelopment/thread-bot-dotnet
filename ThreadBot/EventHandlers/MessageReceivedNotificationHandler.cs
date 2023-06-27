using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers
{
    public class MessageReceivedNotificationHandler : INotificationHandler<MessageReceivedNotification>
    {
        public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        }
    }
}