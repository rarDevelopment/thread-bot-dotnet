namespace ThreadBot.Modules
{
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("test", "Just a test command")]
        public async Task TestCommand()
            => await RespondAsync("Hello There");

    }
}