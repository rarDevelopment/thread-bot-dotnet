using DiscordDotNetUtilities.Interfaces;
using JetBrains.Annotations;

namespace ThreadBot.Commands;

[UsedImplicitly]
public class ViewThreadsInChannelSlashCommand(
    ThreadListUpdateHelper threadListUpdateHelper,
    IDiscordFormatter discordFormatter,
    ILogger<DiscordBot> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("view-channel-threads", "See the threads in this channel.")]
    public async Task ViewThreadsInChannel()
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.ManageThreads)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    "Sorry, you do not have permission to set the thread list channel.",
                    Context.User));
            return;
        }

        var channelToView = Context.Channel;

        try
        {
            var threadsInChannel = Context.Guild.ThreadChannels
            .Where(t => t.ParentChannel.Id == (channelToView as IGuildChannel)?.Id && t is { IsArchived: false, IsLocked: false })
            .ToList();

            if (threadsInChannel.Count == 0)
            {
                await FollowupAsync(embed: discordFormatter.BuildRegularEmbedWithUserFooter("No Threads Found",
                    """No threads were found. If there are threads in this channel, I am not able to see them and you should check if I have "View Channel" permissions for this channel!""",
                    Context.User));
                return;
            }

            var message = threadListUpdateHelper.GetThreadListForChannel(Context.Guild, Context.Channel);

            if (message != null)
            {
                var channelMention = (channelToView as SocketTextChannel)!.Mention;
                await FollowupAsync(embed: discordFormatter.BuildRegularEmbedWithUserFooter($"Threads for {channelMention}",
                    "",
                    Context.User, message.Value.threadEmbed.Fields));
            }
            else
            {
                await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Could not get the list of Threads",
                    "The command failed. Please try again later, or there might be an issue with your request.",
                    Context.User));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"ThreadBot Error: {ex.Message}");
            await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Could not get the list of Threads",
                "The command failed. Please try again later, or there might be an issue with your request.",
                Context.User));
        }
    }

    [ComponentInteraction("currentIndexNext_*")]
    [UsedImplicitly]
    public async Task NextButton(int currentIndex)
    {
        await DeferAsync();

        var newIndex = currentIndex + 1;
        var threadsByChannelAndPageCount = threadListUpdateHelper.GetThreadsByChannelPaginated(Context.Guild.ThreadChannels);
        if (newIndex < threadsByChannelAndPageCount.totalPages)
        {
            await threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild, newIndex);
        }

        await FollowupAsync();
    }
    [ComponentInteraction("currentIndexPrev_*")]
    [UsedImplicitly]
    public async Task PreviousButton(int currentIndex)
    {
        await DeferAsync();

        var newIndex = currentIndex - 1;

        if (newIndex >= 0)
        {
            await threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild, newIndex);
        }

        await FollowupAsync();
    }
}