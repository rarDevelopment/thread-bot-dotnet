using ThreadBot.DataLayer.SchemaModels;
using ThreadBot.Models;
using ThreadBot.Models.Exceptions;

namespace ThreadBot.Tests;

public class ModelTests
{
    #region ThreadChannelPartial

    [Fact]
    public void ThreadChannelPartial_PropertiesSetCorrectly()
    {
        var partial = new ThreadChannelPartial("my-thread", "<#12345>", "general", "#general");

        Assert.Equal("my-thread", partial.ThreadName);
        Assert.Equal("<#12345>", partial.ThreadMention);
        Assert.Equal("general", partial.ChannelName);
        Assert.Equal("#general", partial.ChannelMention);
    }

    #endregion

    #region ThreadListMessage

    [Fact]
    public void ThreadListMessage_PropertiesSetCorrectly()
    {
        var message = new ThreadListMessage("guild-1", "channel-2", "msg-3");

        Assert.Equal("guild-1", message.GuildId);
        Assert.Equal("channel-2", message.ChannelId);
        Assert.Equal("msg-3", message.ListMessageId);
    }

    [Fact]
    public void ThreadListMessage_ListMessageIdCanBeNull()
    {
        var message = new ThreadListMessage("guild-1", "channel-2", null);

        Assert.Null(message.ListMessageId);
    }

    [Fact]
    public void ThreadListMessage_PropertiesAreMutable()
    {
        var message = new ThreadListMessage("g1", "c1", "m1");
        message.GuildId = "g2";
        message.ChannelId = "c2";
        message.ListMessageId = "m2";

        Assert.Equal("g2", message.GuildId);
        Assert.Equal("c2", message.ChannelId);
        Assert.Equal("m2", message.ListMessageId);
    }

    #endregion

    #region DatabaseSettings

    [Fact]
    public void DatabaseSettings_PropertiesSetCorrectly()
    {
        var settings = new DatabaseSettings("cluster0", "admin", "pass123", "threadbot-db");

        Assert.Equal("cluster0", settings.Cluster);
        Assert.Equal("admin", settings.User);
        Assert.Equal("pass123", settings.Password);
        Assert.Equal("threadbot-db", settings.Name);
    }

    #endregion

    #region DiscordSettings

    [Fact]
    public void DiscordSettings_BotTokenSetCorrectly()
    {
        var settings = new DiscordSettings("test-token-abc");

        Assert.Equal("test-token-abc", settings.BotToken);
    }

    #endregion

    #region VersionSettings

    [Fact]
    public void VersionSettings_VersionNumberSetCorrectly()
    {
        var settings = new VersionSettings("1.2.3");

        Assert.Equal("1.2.3", settings.VersionNumber);
    }

    #endregion

    #region ThreadListMessageEntity

    [Fact]
    public void ThreadListMessageEntity_ToDomain_MapsCorrectly()
    {
        var entity = new ThreadListMessageEntity
        {
            Id = "abc123",
            GuildId = "guild-1",
            ChannelId = "channel-2",
            ListMessageId = "msg-3"
        };

        var domain = entity.ToDomain();

        Assert.Equal("guild-1", domain.GuildId);
        Assert.Equal("channel-2", domain.ChannelId);
        Assert.Equal("msg-3", domain.ListMessageId);
    }

    #endregion

    #region Exceptions

    [Fact]
    public void ChannelNotFoundException_MessageContainsChannelId()
    {
        var exception = new ChannelNotFoundException("ch-999");

        Assert.Contains("ch-999", exception.Message);
    }

    [Fact]
    public void InvalidChannelTypeException_MessageContainsChannelId()
    {
        var exception = new InvalidChannelTypeException("ch-456");

        Assert.Contains("ch-456", exception.Message);
    }

    [Fact]
    public void NoGuildChannelSetException_MessageContainsGuildId()
    {
        var exception = new NoGuildChannelSetException("guild-789");

        Assert.Contains("guild-789", exception.Message);
    }

    [Fact]
    public void AllCustomExceptions_AreExceptions()
    {
        Assert.IsAssignableFrom<Exception>(new ChannelNotFoundException("1"));
        Assert.IsAssignableFrom<Exception>(new InvalidChannelTypeException("1"));
        Assert.IsAssignableFrom<Exception>(new NoGuildChannelSetException("1"));
    }

    #endregion
}
