using System.Text.Json;

namespace ThreadBot;

public static class DiscordExtensions
{
    extension(ISocketMessageChannel channel)
    {
        public bool HasPermissionToSendMessagesInChannel(ulong userId, out string? missingPermissions)
        {
            missingPermissions = null;
        
            if (channel is not IGuildChannel guildChannel)
            {
                missingPermissions = "Channel is not a guild channel.";
                return false;
            }
        
            if (guildChannel.Guild is not SocketGuild guild)
            {
                missingPermissions = "Guild is not accessible.";
                return false;
            }

            var user = guild.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                missingPermissions = "User not found in guild.";
                return false;
            }

            var permissions = user.GetPermissions(guildChannel);
        
            var permissionsJson = JsonSerializer.Serialize(new
            {
                ChannelId = guildChannel.Id,
                UserId = userId,
                permissions.ViewChannel,
                permissions.SendMessages,
                permissions.EmbedLinks,
                permissions.ReadMessageHistory,
                Result = permissions is { ViewChannel: true, SendMessages: true, EmbedLinks: true, ReadMessageHistory: true }
            }, new JsonSerializerOptions { WriteIndented = true });
        
            Console.WriteLine($"[Permission Check] {permissionsJson}");

            var missing = new List<string>();
            if (!permissions.ViewChannel)
            {
                missing.Add("View Channel");
            }

            if (!permissions.SendMessages)
            {
                missing.Add("Send Messages");
            }

            if (!permissions.EmbedLinks)
            {
                missing.Add("Embed Links");
            }

            if (!permissions.ReadMessageHistory)
            {
                missing.Add("Read Message History");
            }

            if (missing.Count <= 0)
            {
                return true;
            }

            missingPermissions = string.Join("\n", missing);
            return false;

        }
    }
}