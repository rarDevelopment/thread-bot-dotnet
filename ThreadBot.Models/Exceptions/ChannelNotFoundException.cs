namespace ThreadBot.Models.Exceptions;

public class ChannelNotFoundException(string channelId) : Exception($"No channel found with id {channelId}");