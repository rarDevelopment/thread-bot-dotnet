using DiscordDotNetUtilities.Interfaces;
using ThreadBot.BusinessLayer;

namespace ThreadBot.Commands;

public class SetThreadChannelSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IThreadBotBusinessLayer _threadBotBusinessLayer;
    private readonly ThreadListUpdateHelper _threadListUpdateHelper;
    private readonly IDiscordFormatter _discordFormatter;

    public SetThreadChannelSlashCommand(IThreadBotBusinessLayer threadBotBusinessLayer,
        ThreadListUpdateHelper threadListUpdateHelper,
        IDiscordFormatter discordFormatter)
    {
        _threadBotBusinessLayer = threadBotBusinessLayer;
        _threadListUpdateHelper = threadListUpdateHelper;
        _discordFormatter = discordFormatter;
    }

    [SlashCommand("set-thread-channel", "Set the channel for the thread list to appear in.")]
    public async Task SetThreadChannel(
        [Summary("channel", "The channel where the thread list will appear")] ISocketMessageChannel? channelToSet = null)
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbedWithUserFooter("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.ManageThreads)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    "Sorry, you do not have permission to set the thread list channel.",
                    Context.User));
            return;
        }

        channelToSet ??= Context.Channel;

        var placeholderMessage = await channelToSet.SendMessageAsync(
            embed: _discordFormatter.BuildRegularEmbed("Thread List Placeholder",
                "Threads will appear here once the process is finished.", new EmbedFooterBuilder { Text = "Placeholder" }));

        var isSuccess = await _threadBotBusinessLayer.SetThreadListMessage(Context.Guild.Id.ToString(), channelToSet.Id.ToString(), placeholderMessage.Id.ToString());

        var message = await _threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild);

        if (isSuccess && message != null)
        {
            await FollowupAsync(embed: _discordFormatter.BuildRegularEmbedWithUserFooter("Thread Channel Set",
                $"The list of threads in this server will now appear in {(channelToSet as SocketTextChannel)!.Mention}",
                Context.User));
        }
        else
        {
            await FollowupAsync(embed: _discordFormatter.BuildErrorEmbedWithUserFooter("Thread Channel Was Not Set",
                "The command failed. Please try again later, or there might be an issue with your request.",
                Context.User));
        }
    }
}