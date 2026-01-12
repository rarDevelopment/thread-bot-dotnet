using DiscordDotNetUtilities.Interfaces;
using JetBrains.Annotations;
using ThreadBot.BusinessLayer;

namespace ThreadBot.Commands;

[UsedImplicitly]
public class SetThreadChannelSlashCommand(
    IThreadBotBusinessLayer threadBotBusinessLayer,
    ThreadListUpdateHelper threadListUpdateHelper,
    IDiscordFormatter discordFormatter,
    ILogger<DiscordBot> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set-thread-channel", "Set the channel for the thread list to appear in.")]
    public async Task SetThreadChannel(
        [Summary("channel", "The channel that shows the thread list")]
        ISocketMessageChannel? channelToSet = null)
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

        channelToSet ??= Context.Channel;

        try
        {
            var botHasPermission = channelToSet.HasPermissionToSendMessagesInChannel(Context.Client.CurrentUser.Id, out var botMissingPermissions);
            logger.LogInformation($"Bot permission check for channel {channelToSet.Id}: {botHasPermission}");

            if (!botHasPermission)
            {
                await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    $"Sorry, I do not have permission to send messages in that channel.\n\n**Missing Permissions:** {botMissingPermissions}",
                    Context.User));
                return;
            }

            var userHasPermission = channelToSet.HasPermissionToSendMessagesInChannel(Context.User.Id, out var userMissingPermissions);
            logger.LogInformation($"User permission check for channel {channelToSet.Id}: {userHasPermission}");

            if (!userHasPermission)
            {
                await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    $"Sorry, you do not have permission to send messages in that channel.\n\n**Missing Permissions:**\n{userMissingPermissions}",
                    Context.User));
                return;
            }

            var placeholderMessage = await channelToSet.SendMessageAsync(
                embed: discordFormatter.BuildRegularEmbed("Thread List Placeholder",
                    "Threads will appear here once the process is finished.",
                    new EmbedFooterBuilder { Text = "Placeholder" }));

            var isSuccess = await threadBotBusinessLayer.SetThreadListMessage(Context.Guild.Id.ToString(),
                channelToSet.Id.ToString(), placeholderMessage.Id.ToString());

            var message = await threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild);

            if (isSuccess && message != null)
            {
                await FollowupAsync(embed: discordFormatter.BuildRegularEmbedWithUserFooter("Thread Channel Set",
                    $"The list of threads in this server will now appear in {(channelToSet as SocketTextChannel)!.Mention}",
                    Context.User));
            }
            else
            {
                await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Thread Channel Was Not Set",
                    "The command failed. Please try again later, or there might be an issue with your request.",
                    Context.User));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"ThreadBot Error: {ex.Message}");
            await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Thread Channel Was Not Set",
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