namespace ThreadBot;

public class DiscordSettings
{
    public DiscordSettings(string botToken)
    {
        BotToken = botToken;
    }

    public string BotToken { get; set; }
}