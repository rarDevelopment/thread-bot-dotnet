using ThreadBot.BusinessLayer;
using ThreadBot.Models;
using ThreadBot.Models.Exceptions;

namespace ThreadBot;

public class ThreadListUpdateHelper
{
    private readonly IThreadBotBusinessLayer _threadBotBusinessLayer;
    private readonly ILogger<DiscordBot> _logger;
    private const string ActiveThreadsTitle = "Active Threads";
    private const string NoActiveThreadsTitle = "No Active Threads";
    private const string FooterText = "Regards, Theodore";

    public ThreadListUpdateHelper(IThreadBotBusinessLayer threadBotBusinessLayer, ILogger<DiscordBot> logger)
    {
        _threadBotBusinessLayer = threadBotBusinessLayer;
        _logger = logger;
    }

    public async Task<IUserMessage?> UpdateThreadListAndGetMessage(SocketGuild guild)
    {
        try
        {
            var threads = GetThreadsToShow(guild.ThreadChannels);
            var threadEmbed = BuildThreadEmbed(threads);
            var message = await UpdateThreadList(guild, threadEmbed);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    private static IReadOnlyList<ThreadChannelPartial> GetThreadsToShow(IReadOnlyCollection<SocketThreadChannel> socketThreadChannels)
    {
        return socketThreadChannels.Where(t => t is { IsArchived: false, IsLocked: false })
            .Select(t => new ThreadChannelPartial(t.Name,
                t.Mention,
                t.ParentChannel.Name,
                (t.ParentChannel as SocketTextChannel)?.Mention ?? "")).ToList();
    }

    private static EmbedBuilder BuildThreadEmbed(IEnumerable<ThreadChannelPartial> threads)
    {
        var threadsByChannel = new Dictionary<string, List<ThreadChannelPartial>>();
        foreach (var thread in threads)
        {
            if (threadsByChannel.TryGetValue(thread.ChannelName, out var channelGroup))
            {
                channelGroup.Add(thread);
            }
            else
            {
                threadsByChannel.Add(thread.ChannelName, new List<ThreadChannelPartial> { thread });
            }
        }

        var embedFields = new List<EmbedFieldBuilder>();
        foreach (var thread in threadsByChannel)
        {
            var threadMentions = thread.Value.OrderBy(t => t.ThreadName).Select(t => t.ThreadMention);
            embedFields.Add(new EmbedFieldBuilder
            {
                Name = $"#{thread.Key}",
                Value = string.Join("\n", threadMentions),
                IsInline = false
            });
        }

        var embed = new EmbedBuilder
        {
            Title = embedFields.Count > 0 ? ActiveThreadsTitle : NoActiveThreadsTitle,
            Fields = embedFields.Count > 0 ? embedFields : new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = NoActiveThreadsTitle,
                    Value = "No threads are currently active on the server.",
                    IsInline = false
                }
            },
            Footer = new EmbedFooterBuilder { Text = FooterText }
        };

        return embed;
    }

    private async Task<IUserMessage> UpdateThreadList(SocketGuild guild, EmbedBuilder threadEmbed)
    {
        var guildId = guild.Id.ToString();
        var threadListMessage = await _threadBotBusinessLayer.GetThreadListMessage(guildId);
        if (threadListMessage == null)
        {
            throw new NoGuildChannelSetException(guildId);
        }

        var threadListChannel = guild.GetChannel(Convert.ToUInt64(threadListMessage.ChannelId));
        if (threadListChannel == null)
        {
            throw new ChannelNotFoundException(threadListMessage.ChannelId);
        }

        if (threadListChannel is not SocketTextChannel textChannel)
        {
            throw new InvalidChannelTypeException(threadListChannel.Id.ToString());
        }

        var message = threadListMessage.ListMessageId != null
            ? await textChannel.GetMessageAsync(Convert.ToUInt64(threadListMessage.ListMessageId))
            : null;

        if (message == null)
        {
            return await textChannel.SendMessageAsync(embed: threadEmbed.Build());
        }

        return await textChannel.ModifyMessageAsync(Convert.ToUInt64(threadListMessage.ListMessageId), msg =>
        {
            msg.Embed = threadEmbed.Build();
        });

    }
}