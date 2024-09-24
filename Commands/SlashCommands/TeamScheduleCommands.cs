using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace BaichungBotia
{
    public class TeamScheduleCommands
    {
        private List<string> _responseStrings = new(5);

        [Command("ShowTeamSchedule")]
        [Description("Shows all scheduled fixtures for the selected team")]
        public async Task ShowTeamSchedule(SlashCommandContext interactionContext,
                [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague,
                [SlashAutoCompleteProvider<FetchCompetitionTeamsAutoComplete>][Parameter("TeamName")][Description("Select Team")] string teamId,
                [Parameter("NumMatches")][Description("Optional, no value will show full schedule")] long numMatches = -1)
        {
            await interactionContext.DeferResponseAsync();
            string response = await GetFixturesResponseForTeam(teamId, numMatches, _responseStrings);
            await HelperFunctions.HandleResponse(interactionContext, response, _responseStrings);
        }

        [Command("ShowUpcoming")]
        [Description("Show next scheduled fixtures for a subscribed team")]
        public async Task ShowUpcoming(SlashCommandContext interactionContext,
            [SlashAutoCompleteProvider<FetchSubscribedTeamsAutoComplete>][Parameter("TeamName")][Description("Select Team")] string teamId,
            [Parameter("NumMatches")][Description("Optional, no value will show next match")] long numMatches = 1)
        {
            await interactionContext.DeferResponseAsync();
            string response = await GetFixturesResponseForTeam(teamId, numMatches, _responseStrings, true);
            await HelperFunctions.HandleResponse(interactionContext, response, _responseStrings);
        }
        
        private async Task<string> GetFixturesResponseForTeam(string teamId, long numMatches, List<string> responseStrings, bool fromSubscriptions = false)
        {
            if (teamId == "-1")
            {
                return fromSubscriptions ? "Sorry, it seems you have no subscriptions" : "Sorry, I could not fetch teams from the competition :(";
            }
            else
            {
                responseStrings.Clear();
                numMatches = numMatches != -1 ? Math.Max(numMatches, 1) : -1;
                string response = await BotCommandLogic.GetTeamFixtures(teamId, responseStrings, (int)numMatches);
                return response;
            }
        }
    }
}