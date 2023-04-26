using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("Subscribe_To_Team", "Subscribe to fixture reminders of a team")]
        public async Task SubscribeToTeam(InteractionContext interactionContext,
            [Option("League", "Select League")] LeagueOptions selectedLeague, [Option("TeamName", "Team Name")] string teamName)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var response = await BotCommandLogic.SubscribeTo(teamName, selectedLeague.ToString());

            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }

        [SlashCommand("Show_Scheduled", "Show all scheduled fixtures for a subscribed team")]
        public async Task ShowScheduled(InteractionContext interactionContext,
            [Autocomplete(typeof(AutoCompleteProvider))]
            [Option("Team", "SelectTeam", true)] long s)
        {
            await interactionContext.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string response = "";
            if (s == -1)
            {
                response = "Sorry, it seems you have no subscriptions";
            }
            await interactionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }
        [Serializable]
        public class ReminderAutoCompletion : IAutocompleteProvider
        {
            public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
            {
                //var factory = ctx.Services.GetRequiredService<IDbContextFactory<MadsContext>>();
                //await using var db = await factory.CreateDbContextAsync();
                //return db.Reminders.Where(x => x.UserId == ctx.User.Id).Select(x => new DiscordAutoCompleteChoice(x.Id.ToString(), x.Id.ToString()));
                var test = new List<DiscordAutoCompleteChoice>();
                test.Add(new DiscordAutoCompleteChoice("test", 1));
                test.Add(new DiscordAutoCompleteChoice("test2", 2));
                test.Add(new DiscordAutoCompleteChoice("test3", 3));
                return test;
            }
        }
        [SlashCommand("delete", "delete a reminder based on its id")]
        public async Task DeleteById
        (
            InteractionContext ctx,
            [Autocomplete(typeof(ReminderAutoCompletion))]
            [Option("id", "id of the given reminder which should be deleted", true)]
            long id
        )
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Success!"));
        }
    }

    public class AutoCompleteProvider : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            Console.WriteLine("SGSGSGS");
            List<SubscriptionDetails> subscriptions = new(5);
            await BotCommandLogic.GetSubscriptions(subscriptions);
            Console.WriteLine($"GSGS{subscriptions.Count}");
            if (subscriptions.Count == 0)
            {
                return new List<DiscordAutoCompleteChoice>() { new DiscordAutoCompleteChoice("No Subscriptions", "-1") };
            }
            List<DiscordAutoCompleteChoice> choices = new(subscriptions.Count);
            foreach (var sub in subscriptions)
            {
                choices.Add(new DiscordAutoCompleteChoice(sub.teamName, sub.teamId.ToString()));
            }
            return choices;
        }
    }    
}
