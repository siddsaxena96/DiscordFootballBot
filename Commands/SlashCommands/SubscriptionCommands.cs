using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace BaichungBotia
{
    public class SubscriptionCommands
    {
        [Command("SubscribeToTeam")]
        [Description("Subscribe to fixture reminders of a team")]
        public async Task SubscribeToTeam(SlashCommandContext interactionContext,
            [SlashChoiceProvider<LeagueOptionsProvider>][Parameter("LeagueName")][Description("Select League")] string selectedLeague,
            [SlashAutoCompleteProvider<FetchCompetitionTeamsAutoComplete>][Parameter("TeamName")][Description("Select Team")] string teamId)
        {
            await interactionContext.DeferResponseAsync();
            string response;
            if (teamId == "-1")
            {
                response = "Sorry Unable to Fetch team data at this time";
            }
            else
            {
                response = await BotCommandLogic.SubscribeTo(selectedLeague.ToString(), teamId);
            }

            await interactionContext.RespondAsync(response);
        }
    }
}