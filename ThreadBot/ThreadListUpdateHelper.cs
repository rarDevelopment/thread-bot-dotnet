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
    private const int MaxFieldsPerPage = 25;
    private const int MaxEmbedContentLength = 5800; // Discord's 6000 char limit minus buffer for title/footer

    public async Task<IUserMessage?> UpdateThreadListAndGetMessage(SocketGuild guild, int newPageIndex = 0)
    {
        try
        {
            var paginatedFields = GetPaginatedEmbedFields(guild.ThreadChannels);

            // Build the embed for the current page
            var threadEmbed = BuildThreadEmbed(paginatedFields.pages.ElementAtOrDefault(newPageIndex) ?? [], newPageIndex);

            // Update the thread list message
            var message = await UpdateThreadList(guild, threadEmbed, newPageIndex, paginatedFields.totalPages);
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

            var paginatedFields = GetPaginatedEmbedFields(threadsInSpecifiedChannel);

            // Build the embed for the current page
            var threadEmbedBuilder = BuildThreadEmbed(paginatedFields.pages.ElementAtOrDefault(newPageIndex) ?? [], newPageIndex);
            var threadListMessage = BuildThreadListMessage(threadEmbedBuilder, newPageIndex, paginatedFields.totalPages);
            return threadListMessage;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    public (List<List<EmbedFieldBuilder>> pages, int totalPages)
        GetPaginatedEmbedFields(IReadOnlyCollection<SocketThreadChannel> threadChannels)
    {
        var threads = threadChannels.Where(t => t is { IsArchived: false, IsLocked: false })
            .Select(t => new ThreadChannelPartial(t.Name,
                t.Mention,
                t.ParentChannel.Name,
                (t.ParentChannel as SocketTextChannel)?.Mention ?? "")).ToList();

        var threadsByChannel = threads
            .GroupBy(t => t.ChannelName)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build all fields with 1024-char splitting per field
        var allFields = new List<EmbedFieldBuilder>();
        foreach (var channelGroup in threadsByChannel)
        {
            allFields.AddRange(BuildFieldsForChannelGroup(channelGroup.Key, channelGroup.Value));
        }

        // Paginate fields respecting both 25-field limit and total embed char limit
        var pages = new List<List<EmbedFieldBuilder>>();
        var currentPage = new List<EmbedFieldBuilder>();
        var currentPageCharCount = 0;

        foreach (var field in allFields)
        {
            var fieldCharCount = field.Name.Length + ((string)field.Value).Length;

            if (currentPage.Count > 0 &&
                (currentPage.Count >= MaxFieldsPerPage || currentPageCharCount + fieldCharCount > MaxEmbedContentLength))
            {
                pages.Add(currentPage);
                currentPage = new List<EmbedFieldBuilder>();
                currentPageCharCount = 0;
            }

            currentPage.Add(field);
            currentPageCharCount += fieldCharCount;
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        if (pages.Count == 0)
        {
            pages.Add([]);
        }

        return (pages, pages.Count);
    }

    private const int MaxEmbedFieldValueLength = 1024;

    private static List<EmbedFieldBuilder> BuildFieldsForChannelGroup(string channelName, List<ThreadChannelPartial> threads)
    {
        var orderedMentions = threads.OrderBy(t => t.ThreadName).Select(t => t.ThreadMention).ToList();
        var fields = new List<EmbedFieldBuilder>();
        var currentLines = new List<string>();
        var currentLength = 0;

        foreach (var mention in orderedMentions)
        {
            var addedLength = (currentLines.Count > 0 ? 1 : 0) + mention.Length; // +1 for \n separator
            if (currentLength + addedLength > MaxEmbedFieldValueLength && currentLines.Count > 0)
            {
                fields.Add(new EmbedFieldBuilder
                {
                    Name = fields.Count == 0 ? $"#{channelName}" : $"#{channelName} (cont.)",
                    Value = string.Join("\n", currentLines),
                    IsInline = false
                });
                currentLines.Clear();
                currentLength = 0;
            }
            currentLines.Add(mention);
            currentLength += addedLength;
        }

        if (currentLines.Count > 0)
        {
            fields.Add(new EmbedFieldBuilder
            {
                Name = fields.Count == 0 ? $"#{channelName}" : $"#{channelName} (cont.)",
                Value = string.Join("\n", currentLines),
                IsInline = false
            });
        }

        return fields;
    }

    private static EmbedBuilder BuildThreadEmbed(List<EmbedFieldBuilder> pageFields, int pageIndex)
    {
        var embed = new EmbedBuilder
        {
            Title = pageFields.Count > 0 ? ActiveThreadsTitle : NoActiveThreadsTitle,
            Fields = pageFields.Count > 0 ? pageFields :
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
