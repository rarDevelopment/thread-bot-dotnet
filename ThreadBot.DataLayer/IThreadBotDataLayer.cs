using ThreadBot.Models;

namespace ThreadBot.DataLayer;

public interface IThreadBotDataLayer
{
    Task<ThreadListMessage?> GetThreadListMessage(string guildId);
    Task<bool> SetThreadListMessage(string guildId, string channelId, string listMessageId);
}