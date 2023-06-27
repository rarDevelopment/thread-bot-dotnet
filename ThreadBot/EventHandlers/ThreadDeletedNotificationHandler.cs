using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadDeletedNotificationHandler : INotificationHandler<ThreadDeletedNotification>
{
    private readonly ThreadListUpdateHelper _threadListUpdateHelper;

    public ThreadDeletedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    {
        _threadListUpdateHelper = threadListUpdateHelper;
    }
    public Task Handle(ThreadDeletedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            if (!notification.DeletedThread.HasValue)
            {
                return Task.CompletedTask;
            }

            await _threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.DeletedThread.Value.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}