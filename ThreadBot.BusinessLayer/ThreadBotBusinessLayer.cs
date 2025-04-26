using ThreadBot.DataLayer;
using ThreadBot.Models;

namespace ThreadBot.BusinessLayer;

public class ThreadBotBusinessLayer(IThreadBotDataLayer threadBotDataLayer) : IThreadBotBusinessLayer
{
    public Task<ThreadListMessage?> GetThreadListMessage(string guildId)
    {
        return threadBotDataLayer.GetThreadListMessage(guildId);
    }

    public Task<bool> SetThreadListMessage(string guildId, string channelId, string messageId)
    {
        return threadBotDataLayer.SetThreadListMessage(guildId, channelId, messageId);
    }
}