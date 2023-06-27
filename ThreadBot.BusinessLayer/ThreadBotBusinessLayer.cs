using ThreadBot.DataLayer;
using ThreadBot.Models;

namespace ThreadBot.BusinessLayer;

public class ThreadBotBusinessLayer : IThreadBotBusinessLayer
{
    private readonly IThreadBotDataLayer _threadBotDataLayer;
    public ThreadBotBusinessLayer(IThreadBotDataLayer threadBotDataLayer)
    {
        _threadBotDataLayer = threadBotDataLayer;
    }

    public Task<ThreadListMessage?> GetThreadListMessage(string guildId)
    {
        return _threadBotDataLayer.GetThreadListMessage(guildId);
    }

    public Task<bool> SetThreadListMessage(string guildId, string channelId, string messageId)
    {
        return _threadBotDataLayer.SetThreadListMessage(guildId, channelId, messageId);
    }
}