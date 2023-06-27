namespace ThreadBot.Models;

public class ThreadChannelPartial
{
    public string ThreadName { get; }
    public string ThreadMention { get; }
    public string ChannelName { get; }
    public string ChannelMention { get; }

    public ThreadChannelPartial(string threadName, string threadMention, string channelName, string channelMention)
    {
        ThreadName = threadName;
        ThreadMention = threadMention;
        ChannelName = channelName;
        ChannelMention = channelMention;
    }
}