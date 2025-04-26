namespace ThreadBot.Models.Exceptions;

public class InvalidChannelTypeException(string channelId)
    : Exception($"Channel with id {channelId} is not a valid channel for use in this context.");