using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace DiscordBot
{
    [Serializable]
    public class Configuration
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("baseURL")]
        public string baseURL { get; private set; }

        [JsonProperty("leagueTableURL")]
        public string leagueTableURL { get; private set; }
        
        [JsonProperty("fixturesURL")]
        public string fixturesURL { get; private set; }
        
        [JsonProperty("leagueStatsURL")]
        public string leagueStatsURL { get; private set; }

        [JsonProperty("prefix")]
        public string Prefix { get; private set; }        

        [JsonProperty("serverid")]
        public ulong ServerId { get; private set; }

        [JsonProperty("footychannelid")]
        public ulong FootyChannelId { get; private set; }

        [JsonProperty("adminusers")]
        public List<ulong> AdminUsers { get; private set; }
    }
    [Serializable]
    public class SubscriptionDetails
    {
        private Team team;
        public Team Team => team;
        public SubscriptionDetails(Team team)
        {
            this.team = team;
        }
    }
    public enum LeagueOptions
    {
        [ChoiceName("English Premier Leage")]
        ENG,
        [ChoiceName("La Liga")]
        ESP,
        [ChoiceName("Bundesliga")]
        GER,
        [ChoiceName("Serie A")]
        ITA
    }
    
    [Serializable]
    public class Team
    {
        public string teamId;
        public string teamName;

        public Team(string teamName, string teamId)
        {
            this.teamName = teamName;
            this.teamId = teamId;
        }
    }
}