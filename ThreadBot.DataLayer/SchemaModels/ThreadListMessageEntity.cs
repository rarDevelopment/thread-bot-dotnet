using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThreadBot.Models;

namespace ThreadBot.DataLayer.SchemaModels;

public class ThreadListMessageEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("guildId")]
    public string GuildId { get; set; }
    [BsonElement("channelId")]
    public string ChannelId { get; set; }
    [BsonElement("listMessageId")]
    public string ListMessageId { get; set; }

    public ThreadListMessage ToDomain()
    {
        return new ThreadListMessage(GuildId, ChannelId, ListMessageId);
    }
}