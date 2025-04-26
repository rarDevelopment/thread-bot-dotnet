using MediatR;

namespace ThreadBot.Notifications;

public class ThreadCreatedNotification(SocketThreadChannel channel) : INotification
{
    public SocketThreadChannel Channel { get; } = channel ?? throw new ArgumentNullException(nameof(channel));
}