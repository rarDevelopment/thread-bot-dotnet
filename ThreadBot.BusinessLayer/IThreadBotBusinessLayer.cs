using ThreadBot.Models;

namespace ThreadBot.BusinessLayer;

public interface IThreadBotBusinessLayer
{
    Task<ThreadListMessage?> GetThreadListMessage(string guildId);
    Task<bool> SetThreadListMessage(string guildId, string channelId, string messageId);
}