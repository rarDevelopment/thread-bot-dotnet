namespace ThreadBot.Models.Exceptions;

public class NoGuildChannelSetException(string guildId)
    : Exception($"No thread channel is set up for guild with id {guildId}");