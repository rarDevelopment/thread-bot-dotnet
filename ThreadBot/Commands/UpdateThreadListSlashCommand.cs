using DiscordDotNetUtilities.Interfaces;

namespace ThreadBot.Commands;

public class UpdateThreadListSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ThreadListUpdateHelper _threadListUpdateHelper;
    private readonly IDiscordFormatter _discordFormatter;

    public UpdateThreadListSlashCommand(ThreadListUpdateHelper threadListUpdateHelper, IDiscordFormatter discordFormatter)
    {
        _threadListUpdateHelper = threadListUpdateHelper;
        _discordFormatter = discordFormatter;
    }

    [SlashCommand("update-threads", "Manual thread update (rarely needed).")]
    public async Task UpdateThreadList()
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

        var isSuccess = message != null;
        if (isSuccess)
        {
            await FollowupAsync(embed: _discordFormatter.BuildRegularEmbed("Thread List Updated",
                $"Manual update of the thread list is complete. Your thread list should now be up to date.",
                Context.User));
        }
        else
        {
            await FollowupAsync(embed: _discordFormatter.BuildErrorEmbed("Thread List Was Not Updated",
                "The command failed. Please try again later, or there might be an issue with your request.",
                Context.User));
        }
    }
}