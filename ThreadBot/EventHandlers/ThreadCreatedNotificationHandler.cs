using DiscordDotNetUtilities.Interfaces;
using ThreadBot.Notifications;

namespace ThreadBot.EventHandlers;

public class ThreadCreatedNotificationHandler(ThreadListUpdateHelper threadListUpdateHelper)
    : IEventHandler<ThreadCreatedNotification>
{
    public Task HandleAsync(ThreadCreatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await threadListUpdateHelper.UpdateThreadListAndGetMessage(notification.Channel.Guild);
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}