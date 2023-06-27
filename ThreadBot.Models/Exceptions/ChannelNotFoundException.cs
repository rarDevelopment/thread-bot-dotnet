namespace ThreadBot.Models.Exceptions;

public class ChannelNotFoundException : Exception
{
    public ChannelNotFoundException(string channelId) : base($"No channel found with id {channelId}") { }
}