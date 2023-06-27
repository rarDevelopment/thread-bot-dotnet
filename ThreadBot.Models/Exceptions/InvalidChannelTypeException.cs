namespace ThreadBot.Models.Exceptions;

public class InvalidChannelTypeException : Exception
{
    public InvalidChannelTypeException(string channelId) : base($"Channel with id {channelId} is not a valid channel for use in this context.") { }
}