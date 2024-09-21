using DSharpPlus;
using Newtonsoft.Json;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using BaichungBotia;

namespace BaichungBotia
{
    public static class BotController
    {
        private static DiscordClient _client;
        public static DiscordClient Client => _client;

        private static Configuration _configuration;
        public static Configuration Configuration => _configuration;

        public static async Task SetupBotController()
        {
            var json = string.Empty;
            using var fs = File.OpenRead("config.json");
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            json = await sr.ReadToEndAsync();
            Console.Write(json);

            _configuration = JsonConvert.DeserializeObject<Configuration>(json);

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(_configuration.Token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents);
            
            builder.UseCommands(
                // we register our commands here      
                extension =>
                {
                    extension.AddCommands([typeof(PingCommand), typeof(AdminCommands), typeof(SquadCommands), typeof(StatsCommands), typeof(SubscriptionCommands), typeof(TeamScheduleCommands)]);
                },
                new CommandsConfiguration()
                {
                    DebugGuildId = _configuration.ServerId,
                    // The default value, however it's shown here for clarity
                    RegisterDefaultCommandProcessors = true
                }
            );

            _client = builder.Build();
            DiscordActivity status = new("The Footy", DiscordActivityType.Playing);
            await _client.ConnectAsync(status, DiscordUserStatus.Online);
        }
    }
}
public class PingCommand
{
    [Command("ping")]
    public static ValueTask ExecuteAsync(CommandContext context) => context.RespondAsync($"SIUUUUU! Latency is - {context.Client.GetConnectionLatency(BotController.Configuration.ServerId)}ms.");
}