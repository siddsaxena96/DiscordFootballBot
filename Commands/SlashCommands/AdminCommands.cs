using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;

namespace BaichungBotia
{
    public class AdminCommands
    {
        [Obsolete]
        [Command("ClearPlayerStatsCache")]
        [Description("!!OBSOLETE!!! - Clears the cached player stats ( requires Admin User)")]
        public async Task ClearPlayerStatsCache(SlashCommandContext interactionContext)
        {
            await interactionContext.DeferResponseAsync();
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
            await interactionContext.EditResponseAsync(response);

        }

        [Command("ResetLeagueTeamsCache")]
        [Description("Reloads team data for all leagues ( requires Admin User)")]
        public async Task ResetLeagueTeamDataCache(SlashCommandContext interactionContext)
        {
            await interactionContext.DeferResponseAsync();
            string response = "";
            if (BotController.Configuration.AdminUsers.Contains(interactionContext.User.Id))
            {
                await BotCommandLogic.RefreshTeamsCache();
                response = "Teams Cache has been reset";
            }
            else
            {
                response = "Sorry, only admins can use this command";
            }
            await interactionContext.EditResponseAsync(response);
        }

        [Command("ForceUpdateMatchReminder")]
        [Description("Force botia to refresh match reminder ( requires Admin User)")]
        public async Task ForceUpdateMatchReminder(SlashCommandContext interactionContext)
        {
            await interactionContext.DeferResponseAsync();
            string response = "";
            if (BotController.Configuration.AdminUsers.Contains(interactionContext.User.Id))
            {
                response = ":(";
                await HelperFunctions.RoutineCheckUpcomingMatches();
            }
            else
            {
                response = "Sorry, only admins can use this command";
            }
            await interactionContext.EditResponseAsync(response);
        }
    }
}