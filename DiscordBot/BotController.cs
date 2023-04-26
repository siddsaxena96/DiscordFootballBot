using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class BotController
    {
        public struct Configuration
        {
            [JsonProperty("token")]
            public string Token { get; private set; }

            [JsonProperty("prefix")]
            public string Prefix { get; private set; }
            
            [JsonProperty("apitoken")]
            public string APIToken { get; private set; }
            
            [JsonProperty("footychannelid")]
            public ulong footyChannelId { get;private set; }  
        }
     
        public DiscordClient _client { get; private set; }
        public InteractivityExtension _interactivity { get; private set; }
        public CommandsNextExtension _commands { get; private set; }
        public static Configuration configuration;
        public async Task RunAsync()
        {
            var json = string.Empty;
            using var fs = File.OpenRead("config.json");
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            json = await sr.ReadToEndAsync();

            configuration = JsonConvert.DeserializeObject<Configuration>(json);

            var config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = configuration.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            _client = new DiscordClient(config);
            _client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { configuration.Prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            _commands = _client.UseCommandsNext(commandsConfig);
            _commands.RegisterCommands<Commands>();
            
            var slashCommandsConfig = _client.UseSlashCommands();
            slashCommandsConfig.RegisterCommands<SlashCommands>(700563290153156608);            
            slashCommandsConfig.AutocompleteErrored += AutoCompleteErr;
            slashCommandsConfig.SlashCommandErrored += SlashErr;
            await _client.ConnectAsync();            
        }

        private Task SlashErr(SlashCommandsExtension sender, SlashCommandErrorEventArgs args)
        {
            Console.WriteLine(args.Context);
            Console.WriteLine(args.Exception);

            throw new Exception();
        }

        private Task AutoCompleteErr(SlashCommandsExtension sender, AutocompleteErrorEventArgs args)
        {           
            Console.WriteLine(args.Exception);
            Console.WriteLine(args.Context.OptionValue);
            throw new Exception();
        }

        public async Task RoutineCheckUpcomingMatches()
        {
            List<DiscordEmbed> matchReminders = new List<DiscordEmbed>();
            await BotCommandLogic.RoutineCheckUpcomingMatches(matchReminders);
            if (matchReminders.Count > 0)
            {
                DiscordChannel channel = await _client.GetChannelAsync(configuration.footyChannelId);
                foreach (var embedMessage in matchReminders)
                {
                    await channel.SendMessageAsync(embed: embedMessage);
                }
            }
        }

        private Task OnClientReady(ReadyEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }


    }
}
