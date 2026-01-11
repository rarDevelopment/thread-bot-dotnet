namespace ThreadBot;

public static class DiscordExtensions
{
    public static bool HasPermissionToSendMessagesInChannel(this ISocketMessageChannel channel, ulong userId)
    {
        if (channel is not IGuildChannel guildChannel)
        {
            return false;
        }

        if (guildChannel.Guild is not SocketGuild guild)
        {
            return false;
        }

        var user = guild.Users.FirstOrDefault(u => u.Id == userId);

        var permissions = user?.GetPermissions(guildChannel);

        return permissions is { ViewChannel: true, SendMessages: true };
    }
}