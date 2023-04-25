using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Commands : BaseCommandModule
    {
        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Skynet is Real");
        }

        [Command("add")]
        public async Task AddCommand(CommandContext ctx, int a, int b)
        {
            await (ctx.Channel.SendMessageAsync((a + b).ToString()));
        }

        [Command("SubscribeToTeam")]
        public async Task SubscribeToTeam(CommandContext ctx, string teamName, string competitionCode)
        {
            string response = await BotCommandLogic.SubscribeTo(teamName, competitionCode);
            if (!string.IsNullOrEmpty(response))
            {
                var msg = await new DiscordMessageBuilder()
                .WithContent(response)
                .SendAsync(ctx.Channel);
            }
        }

        [Command("ShowScheduled")]
        public async Task ShowScheduledMatchesForSubscribedTeams(CommandContext ctx, string teamName = null)
        {
            string response = await BotCommandLogic.ShowScheduledMatchesForSubscribedTeams(teamName);
            if (string.IsNullOrEmpty(response))
            {
                response = $"Sorry, It seems you are not subscribed to {teamName}";
            }
            var msg = await new DiscordMessageBuilder()
                .WithContent(response)
                .SendAsync(ctx.Channel);
        }
    }
}
