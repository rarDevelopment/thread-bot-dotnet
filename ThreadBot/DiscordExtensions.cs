using System.Text.Json;

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
        if (user == null)
        {
            return false;
        }

        var permissions = user.GetPermissions(guildChannel);
        
        var permissionsJson = JsonSerializer.Serialize(new
        {
            ChannelId = guildChannel.Id,
            UserId = userId,
            permissions.ViewChannel,
            permissions.SendMessages,
            Result = permissions is { ViewChannel: true, SendMessages: true }
        }, new JsonSerializerOptions { WriteIndented = true });
        
        Console.WriteLine($"[Permission Check] {permissionsJson}");

        return permissions is { ViewChannel: true, SendMessages: true };
    }
}