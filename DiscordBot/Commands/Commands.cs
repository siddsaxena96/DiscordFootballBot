using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DiscordBot
{
    public class Commands : BaseCommandModule
    {
        [Command("ping")]
        public async Task PingCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Skynet is Real");
        }              
    }
}
