using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace BaichungBotia
{
    public class SquadCommands
    {
        private List<string> _responseStrings = new(5);

        [Command("ShowTransfers")]
        [Description("Shows transfer data of the selected team from this season")]
        public async Task ShowTransfers(SlashCommandContext interactionContext,
                [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague,
                [SlashAutoCompleteProvider<FetchCompetitionTeamsAutoComplete>][Parameter("TeamName")][Description("Select Team")] string teamId)
        {
            await interactionContext.DeferResponseAsync();
            _responseStrings.Clear();
            string response = await BotCommandLogic.GetTeamTransferData(teamId, _responseStrings);
            await HelperFunctions.HandleResponse(interactionContext, response, _responseStrings);
        }
    }
}