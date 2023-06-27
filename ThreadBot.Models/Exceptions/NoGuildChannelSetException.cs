namespace ThreadBot.Models.Exceptions;

public class NoGuildChannelSetException : Exception
{
    public NoGuildChannelSetException(string guildId) :
        base($"No thread channel is set up for guild with id {guildId}")
    { }
}