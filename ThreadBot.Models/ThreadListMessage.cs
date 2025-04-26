namespace ThreadBot.Models;

public class ThreadListMessage(string guildId, string channelId, string? listMessageId)
{
    public string GuildId { get; set; } = guildId;
    public string ChannelId { get; set; } = channelId;
    public string? ListMessageId { get; set; } = listMessageId;
}