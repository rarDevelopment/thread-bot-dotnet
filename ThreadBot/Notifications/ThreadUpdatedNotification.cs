using MediatR;

namespace ThreadBot.Notifications;

public class ThreadUpdatedNotification(Cacheable<SocketThreadChannel, ulong> oldThread, SocketThreadChannel newThread)
    : INotification
{
    public Cacheable<SocketThreadChannel, ulong> OldThread { get; } = oldThread;
    public SocketThreadChannel NewThread { get; } = newThread;
}