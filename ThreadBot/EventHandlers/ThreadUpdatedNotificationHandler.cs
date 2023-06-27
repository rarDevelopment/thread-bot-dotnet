using MediatR;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadUpdatedNotificationHandler : INotificationHandler<ThreadUpdatedNotification>
{
    private readonly ThreadListUpdateHelper _threadListUpdateHelper;

    public ThreadUpdatedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    {
        _threadListUpdateHelper = threadListUpdateHelper;
    }
    public Task Handle(ThreadUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await _threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.NewThread.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}