using DiscordDotNetUtilities.Interfaces;
using ThreadBot.BusinessLayer;
using ThreadBot.Models;
using ThreadBot.Models.Exceptions;

namespace ThreadBot;

public class ThreadListUpdateHelper(IThreadBotBusinessLayer threadBotBusinessLayer,
    IDiscordFormatter discordFormatter,
    ILogger<DiscordBot> logger)
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

    public (EmbedBuilder threadEmbed, ComponentBuilder buttonBuilder)? GetThreadListForChannel(SocketGuild guild, ISocketMessageChannel channel, int newPageIndex = 0)
    {
        try
        {
            var threadsInSpecifiedChannel = guild.ThreadChannels.Where(t => t.ParentChannel.Id == channel.Id).ToList();

            var threadsByChannelAndTotalPages = GetThreadsByChannelPaginated(threadsInSpecifiedChannel);

            // Build the embed for the current page
            var threadEmbedBuilder = BuildThreadEmbed(threadsByChannelAndTotalPages.threadsByChannel, newPageIndex);
            var threadListMessage = BuildThreadListMessage(threadEmbedBuilder, newPageIndex, threadsByChannelAndTotalPages.totalPages);
            return threadListMessage;
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

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)threadsByChannel.Count / MaxChannelGroupsPerPage));

        return (threadsByChannel, totalPages);
    }

    private static EmbedBuilder BuildThreadEmbed(Dictionary<string, List<ThreadChannelPartial>> threadsByChannel, int pageIndex)
    {
        var paginatedChannelGroups = threadsByChannel
            .OrderBy(c => c.Key)
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

    private async Task<ThreadListMessage?> GetThreadList(SocketGuild guild, EmbedBuilder threadEmbed, int newPageIndex, int totalPages)
    {
        var guildId = guild.Id.ToString();
        var threadListMessage = await threadBotBusinessLayer.GetThreadListMessage(guildId);
        return threadListMessage;
    }

    private async Task<IUserMessage> UpdateThreadList(SocketGuild guild, EmbedBuilder threadEmbed, int newPageIndex, int totalPages)
    {
        var guildId = guild.Id.ToString();
        var threadListMessage = await threadBotBusinessLayer.GetThreadListMessage(guildId);

        if (threadListMessage == null)
        {
            throw new NoGuildChannelSetException(guildId);
        }

        return await UpdateThreadListForMessage(guild, threadEmbed, newPageIndex, totalPages, threadListMessage);
    }

    private (EmbedBuilder threadEmbed, ComponentBuilder buttonBuilder) BuildThreadListMessage(EmbedBuilder threadEmbed, int newPageIndex, int totalPages)
    {
        var buttonBuilder = new ComponentBuilder();
        if (newPageIndex >= 1)
        {
            buttonBuilder.WithButton("Previous", $"currentIndexPrev_{newPageIndex}", emote: new Emoji("⬅️"));
        }
        if (newPageIndex < (totalPages - 1) && totalPages > 1)
        {
            buttonBuilder.WithButton("Next", $"currentIndexNext_{newPageIndex}", emote: new Emoji("➡️"));
        }

        return (threadEmbed, buttonBuilder);
    }

    private async Task<IUserMessage> UpdateThreadListForMessage(SocketGuild guild, EmbedBuilder threadEmbed, int newPageIndex,
        int totalPages, ThreadListMessage threadListMessage)
    {
        var threadListChannel = guild.GetChannel(Convert.ToUInt64(threadListMessage.ChannelId));
        if (threadListChannel == null)
        {
            throw new ChannelNotFoundException(threadListMessage.ChannelId);
        }

        if (threadListChannel is not SocketTextChannel textChannel)
        {
            throw new InvalidChannelTypeException(threadListChannel.Id.ToString());
        }

        var (embed, components) = BuildThreadListMessage(threadEmbed, newPageIndex, totalPages);

        var message = threadListMessage.ListMessageId != null
            ? await textChannel.GetMessageAsync(Convert.ToUInt64(threadListMessage.ListMessageId))
            : null;

        var listMessageId = threadListMessage.ListMessageId;

        if (message != null)
        {
            return await textChannel.ModifyMessageAsync(Convert.ToUInt64(listMessageId), properties =>
            {
                properties.Embed = embed.Build();
                properties.Components = components.Build();
            });
        }

        var placeholderMessage = await textChannel.SendMessageAsync(
            embed: discordFormatter.BuildRegularEmbed("Thread List Placeholder",
                "Threads will appear here once the process is finished.",
                new EmbedFooterBuilder { Text = "Placeholder" }));

        var isSuccess = await threadBotBusinessLayer.SetThreadListMessage(guild.Id.ToString(),
            textChannel.Id.ToString(), placeholderMessage.Id.ToString());
        if (isSuccess)
        {
            listMessageId = placeholderMessage.Id.ToString();
        }

        return await textChannel.ModifyMessageAsync(Convert.ToUInt64(listMessageId), properties =>
        {
            properties.Embed = embed.Build();
            properties.Components = components.Build();
        });
    }
}