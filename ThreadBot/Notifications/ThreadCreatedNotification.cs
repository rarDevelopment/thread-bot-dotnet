using MediatR;

namespace ThreadBot.Notifications;

public class ThreadCreatedNotification : INotification
{
    public SocketThreadChannel Channel { get; }

    public ThreadCreatedNotification(SocketThreadChannel channel)
    {
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }
}