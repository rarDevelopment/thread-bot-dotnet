﻿using DiscordDotNetUtilities.Interfaces;
using ThreadBot.BusinessLayer;

namespace ThreadBot.Commands;

public class SetThreadChannelSlashCommand(
    IThreadBotBusinessLayer threadBotBusinessLayer,
    ThreadListUpdateHelper threadListUpdateHelper,
    IDiscordFormatter discordFormatter) : InteractionModuleBase<SocketInteractionContext>
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

    [ComponentInteraction("currentIndexNext_*")]
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