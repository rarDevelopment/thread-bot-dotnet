using System.Runtime.InteropServices.Marshalling;
using Discord;
using ThreadBot.BusinessLayer;
using ThreadBot.Models;
using ThreadBot.Models.Exceptions;

namespace ThreadBot;

public class ThreadListUpdateHelper(IThreadBotBusinessLayer threadBotBusinessLayer, ILogger<DiscordBot> logger)
{
    private const string ActiveThreadsTitle = "Active Threads";
    private const string NoActiveThreadsTitle = "No Active Threads";
    private const string FooterText = "Regards, Theodore";
    private const int MaxChannelGroupsPerPage = 25;

    public async Task<IUserMessage?> UpdateThreadListAndGetMessage(SocketGuild guild, int newPageIndex = 0)
    {
        try
        {
            var threadsByChannelAndTotalPages = GetThreadsByChannelPaginated(guild.ThreadChannels);

            // Build the embed for the current page
            var threadEmbed = BuildThreadEmbed(threadsByChannelAndTotalPages.threadsByChannel, newPageIndex);

            // Update the thread list message
            var message = await UpdateThreadList(guild, threadEmbed, newPageIndex, threadsByChannelAndTotalPages.totalPages);
            return message;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public (Dictionary<string, List<ThreadChannelPartial>> threadsByChannel, int totalPages)
        GetThreadsByChannelPaginated(IReadOnlyCollection<SocketThreadChannel> threadChannels)
    {
        var threads = (IReadOnlyList<ThreadChannelPartial>)threadChannels.Where(t => t is { IsArchived: false, IsLocked: false })
            .Select(t => new ThreadChannelPartial(t.Name,
                t.Mention,
                t.ParentChannel.Name,
                (t.ParentChannel as SocketTextChannel)?.Mention ?? "")).ToList();

        var threadsByChannel = threads
            .GroupBy(t => t.ChannelName)
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalPages = (int)Math.Ceiling((double)threadsByChannel.Count / MaxChannelGroupsPerPage);

        return (threadsByChannel, totalPages);
    }

    private static EmbedBuilder BuildThreadEmbed(Dictionary<string, List<ThreadChannelPartial>> threadsByChannel, int pageIndex)
    {
        var paginatedChannelGroups = threadsByChannel
            .Skip(pageIndex * MaxChannelGroupsPerPage)
            .Take(MaxChannelGroupsPerPage)
            .ToList();

        var embedFields = new List<EmbedFieldBuilder>();
        foreach (var channelGroup in paginatedChannelGroups)
        {
            var threadMentions = channelGroup.Value
                .OrderBy(t => t.ThreadName)
                .Select(t => t.ThreadMention);

            embedFields.Add(new EmbedFieldBuilder
            {
                Name = $"#{channelGroup.Key}",
                Value = string.Join("\n", threadMentions),
                IsInline = false
            });
        }

        var embed = new EmbedBuilder
        {
            Title = embedFields.Count > 0 ? ActiveThreadsTitle : NoActiveThreadsTitle,
            Fields = embedFields.Count > 0 ? embedFields :
            [
                new EmbedFieldBuilder
                {
                    Name = NoActiveThreadsTitle,
                    Value = "No threads are currently active on the server.",
                    IsInline = false
                }
            ],
            Footer = new EmbedFooterBuilder { Text = $"{FooterText} | Page {pageIndex + 1}" }
        };

        return embed;
    }

    private async Task<IUserMessage> UpdateThreadList(SocketGuild guild, EmbedBuilder threadEmbed, int newPageIndex, int totalPages)
    {
        var guildId = guild.Id.ToString();
        var threadListMessage = await threadBotBusinessLayer.GetThreadListMessage(guildId);
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

        var buttonBuilder = new ComponentBuilder();
        if (newPageIndex >= 1)
        {
            buttonBuilder.WithButton("Previous", $"currentIndexPrev_{newPageIndex - 1}", emote: new Emoji("⬅️"));
        }
        if (newPageIndex < totalPages && totalPages > 1)
        {
            buttonBuilder.WithButton("Next", $"currentIndexNext_{newPageIndex + 1}", emote: new Emoji("➡️"));
        }

        var message = threadListMessage.ListMessageId != null
            ? await textChannel.GetMessageAsync(Convert.ToUInt64(threadListMessage.ListMessageId))
            : null;

        if (message == null)
        {
            return await textChannel.SendMessageAsync(embed: threadEmbed.Build(), components: buttonBuilder.Build());
        }

        return await textChannel.ModifyMessageAsync(Convert.ToUInt64(threadListMessage.ListMessageId), properties =>
        {
            properties.Embed = threadEmbed.Build();
            properties.Components = buttonBuilder.Build();
        });

    }
}