using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DiscordBot
{
    public class SlashCommands : ApplicationCommandModule
    {
        List<string> responseStrings = new(5);

        [SlashCommand("Subscribe_To_Team", "Subscribe to fixture reminders of a team")]
        public async Task SubscribeToTeam(InteractionContext interactionContext, [Option("League", "Select League")] FDataLeagueOptions selectedLeague,
            [Autocomplete(typeof(FetchCompetitionTeamsAutoComplete))]
            [Option("Team", "SelectTeam", true)] long teamId)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string response;
            if (teamId == -1)
            {
                response = "Sorry Unable to Fetch teams at this time";
            }
            else
            {
                response = await BotCommandLogic.SubscribeTo(selectedLeague.ToString(), teamId);
            }

            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Scheduled", "Show all scheduled fixtures for a subscribed team")]
        public async Task ShowScheduled(InteractionContext interactionContext,
            [Autocomplete(typeof(FetchSubscribedTeamsAutoComplete))]
            [Option("Team", "SelectTeam", true)] long teamId)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (teamId == -1)
            {
                response = "Sorry, it seems you have no subscriptions";
            }
            else
            {
                response = await BotCommandLogic.GetAllScheduledFixturesForTeam(teamId);
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Upcoming", "Show next scheduled fixtures for a subscribed team")]
        public async Task ShowUpcoming(InteractionContext interactionContext,
            [Autocomplete(typeof(FetchSubscribedTeamsAutoComplete))]
            [Option("Team", "SelectTeam", true)] long teamId)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string response = "";
            if (teamId == -1)
            {
                response = "Sorry, it seems you have no subscriptions";
            }
            else
            {
                response = await BotCommandLogic.GetUpcomingFixtureForTeam(teamId);
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Standings", "Show current standings of the selected league")]
        public async Task ShowStandings(InteractionContext interactionContext, [Option("League", "Select League")] FDataLeagueOptions selectedLeague)
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
        public async Task ShowTopScorers(InteractionContext interactionContext, [Option("League", "Select League")] FDataLeagueOptions selectedLeague)
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
    }

    public class FetchSubscribedTeamsAutoComplete : IAutocompleteProvider
    {
        List<SubscriptionDetails> subscriptions = new(5);
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            Console.WriteLine("Providing");
            subscriptions.Clear();
            await BotCommandLogic.GetSubscriptions(subscriptions);
            Console.WriteLine($"Sub count = {subscriptions.Count}");
            if (subscriptions.Count == 0)
            {
                return new List<DiscordAutoCompleteChoice>() { new DiscordAutoCompleteChoice("No Subscriptions", -1) };
            }
            List<DiscordAutoCompleteChoice> choices = new(subscriptions.Count);
            foreach (var sub in subscriptions)
            {
                choices.Add(new DiscordAutoCompleteChoice(sub.team.name, sub.team.id));
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
                    choices.Add(new DiscordAutoCompleteChoice(team.name, team.id));
                }
                return choices;
            }
        }
    }
}
