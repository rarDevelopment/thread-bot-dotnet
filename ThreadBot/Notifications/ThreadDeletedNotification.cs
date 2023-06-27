using MediatR;

namespace ThreadBot.Notifications;

public class ThreadDeletedNotification : INotification
{
    public ThreadDeletedNotification(Cacheable<SocketThreadChannel, ulong> deletedThread)
    {
        DeletedThread = deletedThread;
    }

    public Cacheable<SocketThreadChannel, ulong> DeletedThread { get; set; }
}