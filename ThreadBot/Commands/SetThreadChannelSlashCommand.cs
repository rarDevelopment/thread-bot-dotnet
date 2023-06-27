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
    public async Task SetThreadChannel([Summary("channel", "The channel where the thread list will appear")] ITextChannel channelToSet)
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.ManageThreads)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Insufficient Permissions",
                    "Sorry, you do not have permission to set the thread list channel.",
                    Context.User));
            return;
        }

        var message = await _threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild);

        var isSuccess = message != null && await _threadBotBusinessLayer.SetThreadListMessage(Context.Guild.Id.ToString(), channelToSet.Id.ToString(), message.Id.ToString());
        if (isSuccess)
        {
            await FollowupAsync(embed: _discordFormatter.BuildRegularEmbed("Thread Channel Set",
                $"The list of threads in this server will now appear in {channelToSet.Mention}",
                Context.User));
        }
        else
        {
            await FollowupAsync(embed: _discordFormatter.BuildErrorEmbed("Thread Channel Was Not Set",
                "The command failed. Please try again later, or there might be an issue with your request.",
                Context.User));
        }
    }
}