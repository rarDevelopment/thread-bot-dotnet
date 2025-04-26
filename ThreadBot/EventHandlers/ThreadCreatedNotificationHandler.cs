using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadCreatedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    : INotificationHandler<ThreadCreatedNotification>
{
    public Task Handle(ThreadCreatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.Channel.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}