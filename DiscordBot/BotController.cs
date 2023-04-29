using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Newtonsoft.Json;
using System.Text;

namespace DiscordBot
{
    public class BotController
    {        
        public DiscordClient _client { get; private set; }        
        public CommandsNextExtension _commands { get; private set; }        
        
        private static ConfigurationStruct _configuration;
        public static ConfigurationStruct Configuration => _configuration;

        public async Task RunAsync()
        {
            var json = string.Empty;
            using var fs = File.OpenRead("config.json");
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            json = await sr.ReadToEndAsync();
            _configuration = JsonConvert.DeserializeObject<ConfigurationStruct>(json);

            var config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = _configuration.Token,
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
                StringPrefixes = new string[] { _configuration.Prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            _commands = _client.UseCommandsNext(commandsConfig);
            _commands.RegisterCommands<Commands>();
            
            var slashCommandsConfig = _client.UseSlashCommands();
            slashCommandsConfig.RegisterCommands<SlashCommands>(_configuration.ServerId);            
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
                DiscordChannel channel = await _client.GetChannelAsync(Configuration.FootyChannelId);
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
