using MediatR;

namespace ThreadBot.Notifications;

public class ThreadDeletedNotification(Cacheable<SocketThreadChannel, ulong> deletedThread) : INotification
{
    public Cacheable<SocketThreadChannel, ulong> DeletedThread { get; set; } = deletedThread;
}