using MongoDB.Driver;
using ThreadBot.DataLayer.SchemaModels;
using ThreadBot.Models;

namespace ThreadBot.DataLayer;

public class ThreadBotDataLayer : IThreadBotDataLayer
{
    private readonly IMongoCollection<ThreadListMessageEntity> _threadListMessagesCollection;

    public ThreadBotDataLayer(DatabaseSettings databaseSettings)
    {
        var connectionString = $"mongodb+srv://{databaseSettings.User}:{databaseSettings.Password}@{databaseSettings.Cluster}.mongodb.net/{databaseSettings.Name}?w=majority";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseSettings.Name);
        _threadListMessagesCollection = database.GetCollection<ThreadListMessageEntity>("threadlistmessages");
    }

    public async Task<ThreadListMessage?> GetThreadListMessage(string guildId)
    {
        return (await GetThreadListMessageEntity(guildId))?.ToDomain();
    }

    private async Task<ThreadListMessageEntity?> GetThreadListMessageEntity(string guildId)
    {
        try
        {

            var filter = Builders<ThreadListMessageEntity>.Filter.Eq(t => t.GuildId, guildId);
            var threadListMessage = await _threadListMessagesCollection.Find(filter).FirstOrDefaultAsync();
            return threadListMessage;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task<bool> SetThreadListMessage(string guildId, string channelId, string listMessageId)
    {
        var threadListMessage = await GetThreadListMessageEntity(guildId);
        if (threadListMessage != null)
        {
            var filter = Builders<ThreadListMessageEntity>.Filter.Eq(t => t.GuildId, guildId);
            var update = Builders<ThreadListMessageEntity>.Update
                .Set(t => t.ChannelId, channelId)
                .Set(t => t.ListMessageId, listMessageId);
            var updateResult = await _threadListMessagesCollection.UpdateOneAsync(filter, update);
            return updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        threadListMessage = new ThreadListMessageEntity
        {
            GuildId = guildId,
            ChannelId = channelId,
            ListMessageId = listMessageId
        };

        try
        {
            await _threadListMessagesCollection.InsertOneAsync(threadListMessage);
        }
        catch (Exception ex)
        {
            // TODO: log?
            return false;
        }

        return true;
    }
}