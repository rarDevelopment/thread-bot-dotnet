namespace ThreadBot.Notifications;

public class ThreadCreatedNotification(SocketThreadChannel channel)
{
    public SocketThreadChannel Channel { get; } = channel ?? throw new ArgumentNullException(nameof(channel));
}