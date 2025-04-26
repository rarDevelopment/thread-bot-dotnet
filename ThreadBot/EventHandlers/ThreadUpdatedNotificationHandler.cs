using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadUpdatedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    : INotificationHandler<ThreadUpdatedNotification>
{
    public Task Handle(ThreadUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.NewThread.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}