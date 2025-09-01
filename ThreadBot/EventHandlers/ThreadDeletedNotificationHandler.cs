using DiscordDotNetUtilities.Interfaces;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadDeletedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    : IEventHandler<ThreadDeletedNotification>
{
    public Task HandleAsync(ThreadDeletedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            if (!notification.DeletedThread.HasValue)
            {
                return Task.CompletedTask;
            }

            await threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.DeletedThread.Value.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}