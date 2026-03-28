using NSubstitute;
using ThreadBot.BusinessLayer;
using ThreadBot.DataLayer;
using ThreadBot.Models;

namespace ThreadBot.Tests;

public class ThreadBotBusinessLayerTests
{
    private readonly IThreadBotDataLayer _dataLayer;
    private readonly ThreadBotBusinessLayer _businessLayer;

    public ThreadBotBusinessLayerTests()
    {
        _dataLayer = Substitute.For<IThreadBotDataLayer>();
        _businessLayer = new ThreadBotBusinessLayer(_dataLayer);
    }

    #region GetThreadListMessage

    [Fact]
    public async Task GetThreadListMessage_ReturnsMessageFromDataLayer()
    {
        var expected = new ThreadListMessage("guild-123", "channel-456", "msg-789");
        _dataLayer.GetThreadListMessage("guild-123").Returns(expected);

        var result = await _businessLayer.GetThreadListMessage("guild-123");

        Assert.NotNull(result);
        Assert.Equal("guild-123", result.GuildId);
        Assert.Equal("channel-456", result.ChannelId);
        Assert.Equal("msg-789", result.ListMessageId);
    }

    [Fact]
    public async Task GetThreadListMessage_ReturnsNull_WhenDataLayerReturnsNull()
    {
        _dataLayer.GetThreadListMessage("guild-unknown").Returns((ThreadListMessage?)null);

        var result = await _businessLayer.GetThreadListMessage("guild-unknown");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetThreadListMessage_PassesGuildIdToDataLayer()
    {
        await _businessLayer.GetThreadListMessage("guild-abc");

        await _dataLayer.Received(1).GetThreadListMessage("guild-abc");
    }

    #endregion

    #region SetThreadListMessage

    [Fact]
    public async Task SetThreadListMessage_ReturnsTrue_WhenDataLayerSucceeds()
    {
        _dataLayer.SetThreadListMessage("g1", "c1", "m1").Returns(true);

        var result = await _businessLayer.SetThreadListMessage("g1", "c1", "m1");

        Assert.True(result);
    }

    [Fact]
    public async Task SetThreadListMessage_ReturnsFalse_WhenDataLayerFails()
    {
        _dataLayer.SetThreadListMessage("g1", "c1", "m1").Returns(false);

        var result = await _businessLayer.SetThreadListMessage("g1", "c1", "m1");

        Assert.False(result);
    }

    [Fact]
    public async Task SetThreadListMessage_PassesAllParametersToDataLayer()
    {
        await _businessLayer.SetThreadListMessage("guild-1", "channel-2", "message-3");

        await _dataLayer.Received(1).SetThreadListMessage("guild-1", "channel-2", "message-3");
    }

    #endregion
}
