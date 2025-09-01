namespace ThreadBot.Notifications;

public class ThreadDeletedNotification(Cacheable<SocketThreadChannel, ulong> deletedThread)
{
    public Cacheable<SocketThreadChannel, ulong> DeletedThread { get; set; } = deletedThread;
}