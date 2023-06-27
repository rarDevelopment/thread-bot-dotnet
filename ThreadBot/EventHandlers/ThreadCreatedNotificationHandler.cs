using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadCreatedNotificationHandler : INotificationHandler<ThreadCreatedNotification>
{
    private readonly ThreadListUpdateHelper _threadListUpdateHelper;

    public ThreadCreatedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    {
        _threadListUpdateHelper = threadListUpdateHelper;
    }
    public Task Handle(ThreadCreatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await _threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.Channel.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}