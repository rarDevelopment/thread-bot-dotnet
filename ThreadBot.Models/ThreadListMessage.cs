namespace ThreadBot.Models;

public class ThreadListMessage
{
    public ThreadListMessage(string guildId, string channelId, string? listMessageId)
    {
        GuildId = guildId;
        ChannelId = channelId;
        ListMessageId = listMessageId;
    }

    public string GuildId { get; set; }
    public string ChannelId { get; set; }
    public string? ListMessageId { get; set; }
}