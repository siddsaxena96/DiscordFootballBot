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
        private List<string> _responseStrings = new(5);

        [SlashCommand("Subscribe_To_Team", "Subscribe to fixture reminders of a team")]
        public async Task SubscribeToTeam(InteractionContext interactionContext,
            [Option("League", "Select League")] LeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))] [Option("Team", "SelectTeam", true)] string teamId)
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
        public async Task ShowTeamSchedule(InteractionContext interactionContext, 
            [Option("League", "Select League")] LeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))] [Option("Team", "SelectTeam", true)] string teamId,
            [Option("NumMatches", "Optional, no value will show entire calendar")] long numMatches = -1)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string response;
            if (teamId == "-1")
            {
                response = "Sorry Unable to Fetch teams at this time";
            }
            else
            {
                _responseStrings.Clear();
                response = await BotCommandLogic.GetUpcomingFixtureForTeam(teamId, numMatches, _responseStrings);
                foreach (var responseString in _responseStrings)
                {
                    await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
                }
            }

            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Upcoming", "Show next scheduled fixtures for a subscribed team")]
        public async Task ShowUpcoming(InteractionContext interactionContext,
            [Autocomplete(typeof(FetchSubscribedTeamsAutoComplete))] [Option("Team", "Select Team", true)] string teamId,
            [Option("NumMatches", "Optional, no value will show next match")] long numMatches = 1)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (teamId == "-1")
            {
                response = "Sorry, it seems you have no subscriptions";
            }
            else
            {
                _responseStrings.Clear();
                response = await BotCommandLogic.GetUpcomingFixtureForTeam(teamId, numMatches, _responseStrings);
                foreach (var responseString in _responseStrings)
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
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetStandingsForCompetition(selectedLeague.ToString(), _responseStrings);
            Console.WriteLine(response);
            foreach (var responseString in _responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_League_Stats", "Shows current top goalscorers and assisters of the selected league")]
        public async Task ShowLeagueStats(InteractionContext interactionContext, [Option("League", "Select League")] LeagueOptions selectedLeague)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetLeagueStatsForCompetition(selectedLeague.ToString(), _responseStrings, 0);
            response = await BotCommandLogic.GetLeagueStatsForCompetition(selectedLeague.ToString(), _responseStrings, 1);
            foreach (var responseString in _responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Clear_Player_Stats_Cache", "Clears the cached player stats ( requires Admin User)")]
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

        [SlashCommand("Reset_League_Teams_Cache", "Reloads team data for all leagues ( requires Admin User)")]
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

        [SlashCommand("Force_Update_MatchReminder", "Force botia to refresh match reminder ( requires Admin User)")]
        public async Task ForceUpdateMatchReminder(InteractionContext interactionContext)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (BotController.Configuration.AdminUsers.Contains(interactionContext.User.Id))
            {
                response = ":(";
                await BotController.RoutineCheckUpcomingMatches();
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
        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<DiscordAutoCompleteChoice> _choices = new(5);

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            _subscriptions.Clear();
            await BotCommandLogic.GetSubscriptions(_subscriptions);
            _choices.Clear();
            if (_subscriptions.Count == 0)
            {
                _choices.Add(new DiscordAutoCompleteChoice("No Subscriptions", "-1"));
            }
            else
            {
                foreach (var sub in _subscriptions)
                {
                    _choices.Add(new DiscordAutoCompleteChoice(sub.Team.teamName, sub.Team.teamId));
                }
            }
            return _choices;
        }
    }

    public class FetchCompetitionTeamsAutoComplete : IAutocompleteProvider
    {
        private static List<DiscordAutoCompleteChoice> _choices = new(5);

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var competitionTeams = await BotCommandLogic.GetTeamsFromCompetition(ctx.Options[0].Value.ToString());
            _choices.Clear();
            if (competitionTeams == null)
            {
                _choices.Add(new DiscordAutoCompleteChoice("Sorry, Unable to Fetch Teams", -1));
            }
            else
            {
                foreach (var team in competitionTeams)
                {
                    _choices.Add(new DiscordAutoCompleteChoice(team.teamName, team.teamId));
                }
            }
            return _choices;
        }
    }
}