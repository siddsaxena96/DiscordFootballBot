using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace BaichungBotia
{
    public class LeagueOptionsProvider : IChoiceProvider
    {
        private static readonly IReadOnlyDictionary<string, object> _leagueOptions = new Dictionary<string, object>
        {
            ["English Premier League"] = LeagueOptions.ENG.ToString(),
            ["La Liga"] = LeagueOptions.ESP.ToString(),
            ["Serie A"] = LeagueOptions.ITA.ToString(),
            ["Bundesliga"] = LeagueOptions.GER.ToString(),
        };

        public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_leagueOptions);
    }

    public class FetchSubscribedTeamsAutoComplete : IAutoCompleteProvider
    {
        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static Dictionary<string, object> _choices = new(5);
        public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
        {
            _subscriptions.Clear();
            _choices.Clear();
            await BotCommandLogic.GetSubscriptions(_subscriptions);
            if (_subscriptions.Count == 0)
            {
                _choices.Add("No Subscriptions", "-1");
            }
            else
            {
                foreach (var sub in _subscriptions)
                {
                    _choices.Add(sub.Team.teamName, sub.Team.teamId);
                }
            }
            return _choices;
        }
    }

    public class FetchCompetitionTeamsAutoComplete : IAutoCompleteProvider
    {
        private static Dictionary<string, object> _choices = new(5);

        public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var leagueOption = context.Arguments.First();
            var competitionTeams = await BotCommandLogic.GetTeamsFromCompetition(leagueOption.Value.ToString());
            _choices.Clear();
            if (competitionTeams == null || competitionTeams.Count == 0)
            {
                _choices.Add("Sorry, Unable to Fetch Teams", "-1");
            }
            else
            {
                foreach (var team in competitionTeams)
                {
                    _choices.Add(team.teamName, team.teamId);
                }
            }

            return _choices;
        }
    }
}