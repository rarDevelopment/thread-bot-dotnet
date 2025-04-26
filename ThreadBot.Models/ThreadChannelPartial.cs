namespace ThreadBot.Models;

public class ThreadChannelPartial(string threadName, string threadMention, string channelName, string channelMention)
{
    public string ThreadName { get; } = threadName;
    public string ThreadMention { get; } = threadMention;
    public string ChannelName { get; } = channelName;
    public string ChannelMention { get; } = channelMention;
}