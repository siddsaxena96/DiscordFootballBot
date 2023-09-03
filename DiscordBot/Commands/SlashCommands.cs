using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text.RegularExpressions;
using System;
using System.Runtime.InteropServices;

namespace DiscordBot
{
    public class SlashCommands : ApplicationCommandModule
    {
        List<string> responseStrings = new(5);

        [SlashCommand("Subscribe_To_Team", "Subscribe to fixture reminders of a team")]
        public async Task SubscribeToTeam(InteractionContext interactionContext, [Option("League", "Select League")] LeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))]
            [Option("Team", "SelectTeam", true)] string teamId)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string response;
            if (teamId == "-1")
            {
                response = "Sorry Unable to Fetch team data at this time";
            }
            else
            {
                response = await BotCommandLogic.SubscribeTo(selectedLeague.ToString(), teamId);
            }

            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Team_Schedule", "Shows all scheduled fixtures for the selected team")]
        public async Task ShowTeamSchedule(InteractionContext interactionContext, [Option("League", "Select League")] LeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))]
            [Option("Team", "SelectTeam", true)] string teamId)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string response;
            if (teamId == "-1")
            {
                response = "Sorry Unable to Fetch teams at this time";
            }
            else
            {
                responseStrings.Clear();
                response = await BotCommandLogic.GetTeamFixtures(teamId, responseStrings);
                foreach (var responseString in responseStrings)
                {
                    await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
                }
            }

            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Upcoming", "Show next scheduled fixtures for a subscribed team")]
        public async Task ShowUpcoming(InteractionContext interactionContext,
            [Autocomplete(typeof(FetchSubscribedTeamsAutoComplete))]
            [Option("Team", "Select Team", true)] string teamId,
            [Option("NumMatches", "Optional, no value will show next match")] long numMatches = 0)
        {            
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (teamId == "-1")
            {
                response = "Sorry, it seems you have no subscriptions";
            }
            else
            {
                responseStrings.Clear();
                response = await BotCommandLogic.GetUpcomingFixtureForTeam(teamId, numMatches, responseStrings);
                foreach (var responseString in responseStrings)
                {
                    await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
                }
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Standings", "Show current standings of the selected league")]
        public async Task ShowStandings(InteractionContext interactionContext, [Option("League", "Select League")] LeagueOptions selectedLeague)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            responseStrings.Clear();
            string response = await BotCommandLogic.GetStandingsForCompetition(selectedLeague.ToString(), responseStrings);
            Console.WriteLine(response);
            foreach (var responseString in responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Top_Scorer", "Shows current top goalscorers of the selected league")]
        public async Task ShowTopScorers(InteractionContext interactionContext, [Option("League", "Select League")] LeagueOptions selectedLeague)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            responseStrings.Clear();
            string response = await BotCommandLogic.GetTopScorersForCompetition(selectedLeague.ToString(), responseStrings);
            foreach (var responseString in responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Player_Stats", "Shows individual stats of the selected player")]
        public async Task ShowPlayerStats(InteractionContext interactionContext, [Option("League", "Select League")] FDataLeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))][Option("Team", "Select Team", true)] long teamId,
            [Autocomplete(typeof(FetchPlayerNamesAutoComplete))][Option("Player", "Select Player", true)] long playerIndex)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            if (teamId == -1 || playerIndex == -1)
            {
                await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, unable to load options at this time"));
            }
            else
            {
                responseStrings.Clear();
                var response = await BotCommandLogic.GetPlayerStats(Convert.ToInt32(playerIndex), responseStrings);
                foreach (var responseString in responseStrings)
                {
                    await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
                }
                await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
            }
        }

        [SlashCommand($"Clear_Player_Stats_Cache", "Clears the cached player stats ( requires Admin User)")]
        public async Task ClearPlayerStatsCache(InteractionContext interactionContext)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (BotController.Configuration.AdminUsers.Contains(interactionContext.User.Id))
            {
                response = "Player Stats Cache has been reset";
                BotCommandLogic.ClearTeamStatsCache();
            }
            else
            {
                response = "Sorry, only admins can use this command";
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));

        }

        [SlashCommand($"Reset_League_Teams_Cache", "Reloads team data for all leagues ( requires Admin User)")]
        public async Task ResetLeagueTeamDataCache(InteractionContext interactionContext)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (BotController.Configuration.AdminUsers.Contains(interactionContext.User.Id))
            {
                await BotCommandLogic.RefreshTeamsCache();
                response = "Player Stats Cache has been reset";
            }
            else
            {
                response = "Sorry, only admins can use this command";
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }
    }

    public class FetchSubscribedTeamsAutoComplete : IAutocompleteProvider
    {
        private static List<SubscriptionDetails> subscriptions = new(5);
        private static List<DiscordAutoCompleteChoice> choices = new(5);

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            subscriptions.Clear();
            await BotCommandLogic.GetSubscriptions(subscriptions);
            choices.Clear();
            if (subscriptions.Count == 0)
            {
                choices.Add(new DiscordAutoCompleteChoice("No Subscriptions", "-1"));
            }
            else
            {
                foreach (var sub in subscriptions)
                {
                    choices.Add(new DiscordAutoCompleteChoice(sub.Team.teamName, sub.Team.teamId));
                }
            }
            return choices;
        }
    }

    public class FetchCompetitionTeamsAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var competitionTeams = await BotCommandLogic.GetTeamsFromCompetition(ctx.Options[0].Value.ToString());
            if (competitionTeams == null)
            {
                return new List<DiscordAutoCompleteChoice>() { new DiscordAutoCompleteChoice("Sorry, Unable to Fetch Teams", -1) };
            }
            else
            {
                List<DiscordAutoCompleteChoice> choices = new List<DiscordAutoCompleteChoice>(competitionTeams.Count);
                foreach (var team in competitionTeams)
                {
                    choices.Add(new DiscordAutoCompleteChoice(team.teamName, team.teamId));
                }
                return choices;
            }
        }

    }

    public class FetchPlayerNamesAutoComplete : IAutocompleteProvider
    {
        List<(int playerIndex, string playerName)> playerNames = new(25);
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            playerNames.Clear();
            Console.WriteLine($"GETTING {(long)ctx.Options[1].Value} ");
            await BotCommandLogic.GetPlayerNamesFromTeam((long)ctx.Options[1].Value, playerNames);

            if (playerNames.Count == 0)
            {
                return new List<DiscordAutoCompleteChoice>() { new DiscordAutoCompleteChoice("Sorry, Unable to Fetch Players", -1) };
            }
            else
            {
                List<DiscordAutoCompleteChoice> choices = new List<DiscordAutoCompleteChoice>(playerNames.Count);
                foreach (var playerName in playerNames)
                {
                    choices.Add(new DiscordAutoCompleteChoice(playerName.playerName, playerName.playerIndex));
                    if (choices.Count >= 25)
                        break;
                }
                return choices;
            }
        }
    }
}
