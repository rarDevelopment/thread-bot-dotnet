using MediatR;

namespace ThreadBot.Notifications;

public class ThreadUpdatedNotification : INotification
{
    public Cacheable<SocketThreadChannel, ulong> OldThread { get; }
    public SocketThreadChannel NewThread { get; }

    public ThreadUpdatedNotification(Cacheable<SocketThreadChannel, ulong> oldThread, SocketThreadChannel newThread)
    {
        OldThread = oldThread;
        NewThread = newThread;
    }
}