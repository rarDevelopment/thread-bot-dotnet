using DiscordDotNetUtilities.Interfaces;

namespace ThreadBot.Commands;

public class UpdateThreadListSlashCommand(ThreadListUpdateHelper threadListUpdateHelper, IDiscordFormatter discordFormatter)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("update-threads", "Manual thread update.")]
    public async Task UpdateThreadList()
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

        var message = await threadListUpdateHelper.UpdateThreadListAndGetMessage(Context.Guild);

        var isSuccess = message != null;
        if (isSuccess)
        {
            await FollowupAsync(embed: discordFormatter.BuildRegularEmbedWithUserFooter("Thread List Updated",
                $"Manual update of the thread list is complete. Your thread list should now be up to date.",
                Context.User));
        }
        else
        {
            await FollowupAsync(embed: discordFormatter.BuildErrorEmbedWithUserFooter("Thread List Was Not Updated",
                "The command failed. Please try again later, or there might be an issue with your request.",
                Context.User));
        }
    }
}