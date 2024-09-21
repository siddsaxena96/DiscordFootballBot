using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace BaichungBotia
{
    public class StatsCommands
    {
        private List<string> _responseStrings = new(5);

        [Command("ShowStandings")]
        [Description("Show current standings of the selected league")]
        public async Task ShowStandings(SlashCommandContext interactionContext,
            [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague)
        {
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetStandingsForCompetition(selectedLeague.ToString(), _responseStrings);
            await interactionContext.RespondAsync(response);
            foreach (var responseString in _responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
        }

        [Command("ShowLeagueStats")]
        [Description("Shows current top goalscorers and assisters of the selected league")]
        public async Task ShowLeagueStats(SlashCommandContext interactionContext,
            [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague)
        {
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetLeagueStatsForCompetition(selectedLeague.ToString(), _responseStrings, 0);
            response = await BotCommandLogic.GetLeagueStatsForCompetition(selectedLeague.ToString(), _responseStrings, 1);
            await interactionContext.RespondAsync(response);
            foreach (var responseString in _responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
        }

        [Command("ShowPastResults")]
        [Description("Shows past results of the selected team this season")]
        public async Task ShowPastResults(SlashCommandContext interactionContext,
                [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague,
                [SlashAutoCompleteProvider<FetchCompetitionTeamsAutoComplete>][Parameter("TeamName")][Description("Select Team")] string teamId)
        {
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetTeamPastResults(teamId, _responseStrings);
            await interactionContext.RespondAsync(response);
            foreach (var responseString in _responseStrings)
            {
                await interactionContext.Channel.SendMessageAsync($"```\n{responseString}```");
            }
        }
    }
}