using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace DiscordBot
{
    [Serializable]
    public class ConfigurationStruct
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("baseURL")]
        public string baseURL { get; private set; }

        [JsonProperty("leagueTableURL")]
        public string leagueTableURL { get; private set; }
        [JsonProperty("fixturesURL")]
        public string fixturesURL { get; private set; }

        [JsonProperty("prefix")]
        public string Prefix { get; private set; }

        [JsonProperty("fdataapiendpoint")]
        public string FDataAPIEndpoint { get; private set; }

        [JsonProperty("fdataapitoken")]
        public string FDataAPIToken { get; private set; }

        [JsonProperty("apifootballendpoint")]
        public string APIFootbalAPIEndPoint { get; private set; }

        [JsonProperty("apifootballtoken")]
        public string APIFootballToken { get; private set; }

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
        public Team team;
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
 
    public enum APIChoice
    {
        FootbalDataOrg,
        APIFootball
    }
    public enum FDataLeagueOptions
    {
        [ChoiceName("English Premier Leage")]
        PL,
        [ChoiceName("La Liga")]
        PD,
        [ChoiceName("Bundesliga")]
        BL1,
        [ChoiceName("Serie A")]
        SA,
        [ChoiceName("Ligue 1")]
        FL1
    }
    public enum APIFootballLeagueOptions
    {
        PL = 39,
        PD = 140,
        BL1 = 78,
        SA = 135,
        FL1 = 61
    }

    #region Responses 
    [Serializable]
    public class CompetitionTeamsResponse
    {
        public int count { get; set; }
        public FiltersTeamResponse filters { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public List<Team_Old> teams { get; set; }
    }
    [Serializable]
    public class TeamFixturesResponse
    {
        public FiltersMatchResponse filters { get; set; }
        public ResultSet resultSet { get; set; }
        public List<Match> matches { get; set; }
    }
    [Serializable]
    public class CompetitionStandingsResponse
    {
        public FiltersTeamResponse filters { get; set; }
        public Area area { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public List<Standing> standings { get; set; }
    }
    [Serializable]
    public class CompetitionTopScorerResponse
    {
        public int count { get; set; }
        public Filters filters { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public List<Scorer> scorers { get; set; }
    }
    [Serializable]
    public class CompetitionStandingsResponseAPIFootball
    {
        public string get { get; set; }
        public ParametersAPIFootball parameters { get; set; }
        public List<object> errors { get; set; }
        public int results { get; set; }
        public Paging paging { get; set; }
        public List<ResponseAPIFootball> response { get; set; }
    }
    [Serializable]
    public class TeamPlayersResponseAPIFootball
    {
        public string get { get; set; }
        public ParametersPlayerStats parameters { get; set; }
        public List<object> errors { get; set; }
        public int results { get; set; }
        public Paging paging { get; set; }
        public List<ResponsePlayerStats> response { get; set; }
    }
    #endregion

    #region Football-Data-Org Models

    public class Area
    {
        public int id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string flag { get; set; }
    }

    public class Coach
    {
        public int? id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string name { get; set; }
        public string dateOfBirth { get; set; }
        public string nationality { get; set; }
        public Contract contract { get; set; }
    }

    public class Competition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string type { get; set; }
        public string emblem { get; set; }
    }

    public class Contract
    {
        public object start { get; set; }
        public object until { get; set; }
    }

    public class FiltersTeamResponse
    {
        public string season { get; set; }
    }

    public class RunningCompetition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string type { get; set; }
        public string emblem { get; set; }
    }

    public class Season
    {
        public int id { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int currentMatchday { get; set; }
        public object winner { get; set; }
    }
    public class Standing
    {
        public string stage { get; set; }
        public string type { get; set; }
        public object group { get; set; }
        public List<Table> table { get; set; }
    }

    public class Table
    {
        public string position { get; set; }
        public TeamStanding team { get; set; }
        public string playedGames { get; set; }
        public string form { get; set; }
        public string won { get; set; }
        public string draw { get; set; }
        public string lost { get; set; }
        public string points { get; set; }
        public string goalsFor { get; set; }
        public string goalsAgainst { get; set; }
        public string goalDifference { get; set; }
    }
    public class Squad
    {
        public int id { get; set; }
        public string name { get; set; }
        public string position { get; set; }
        public string dateOfBirth { get; set; }
        public string nationality { get; set; }
    }
    public class TeamStanding
    {
        public int id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string tla { get; set; }
        public string crest { get; set; }
    }
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

    public class Team_Old
    {
        public Area area { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string tla { get; set; }
        public string crest { get; set; }
        public string address { get; set; }
        public string website { get; set; }
        public int founded { get; set; }
        public string clubColors { get; set; }
        public string venue { get; set; }
        public List<RunningCompetition> runningCompetitions { get; set; }
        public Coach coach { get; set; }
        public List<Squad> squad { get; set; }
        public List<object> staff { get; set; }
        public DateTime lastUpdated { get; set; }
    }
    public class AwayTeam
    {
        public int id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string tla { get; set; }
        public string crest { get; set; }
    }

    public class FiltersMatchResponse
    {
        public string dateFrom { get; set; }
        public string dateTo { get; set; }
        public string permission { get; set; }
        public List<string> status { get; set; }
        public int limit { get; set; }
    }

    public class FullTime
    {
        public object home { get; set; }
        public object away { get; set; }
    }

    public class HalfTime
    {
        public object home { get; set; }
        public object away { get; set; }
    }

    public class HomeTeam
    {
        public int id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string tla { get; set; }
        public string crest { get; set; }
    }

    public class Match
    {
        public Area area { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public int id { get; set; }
        public DateTime utcDate { get; set; }
        public string status { get; set; }
        public string matchday { get; set; }
        public string stage { get; set; }
        public object group { get; set; }
        public DateTime lastUpdated { get; set; }
        public HomeTeam homeTeam { get; set; }
        public AwayTeam awayTeam { get; set; }
        public Score score { get; set; }
        public Odds odds { get; set; }
        public List<Referee> referees { get; set; }
    }

    public class Odds
    {
        public string msg { get; set; }
    }

    public class Referee
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string nationality { get; set; }
    }

    public class ResultSet
    {
        public int count { get; set; }
        public string competitions { get; set; }
        public string first { get; set; }
        public string last { get; set; }
        public int played { get; set; }
        public int wins { get; set; }
        public int draws { get; set; }
        public int losses { get; set; }
    }

    public class Score
    {
        public object winner { get; set; }
        public string duration { get; set; }
        public FullTime fullTime { get; set; }
        public HalfTime halfTime { get; set; }
    }

    public class Filters
    {
        public string season { get; set; }
        public int limit { get; set; }
    }

    public class Player
    {
        public int id { get; set; }
        public string name { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string dateOfBirth { get; set; }
        public string nationality { get; set; }
        public string position { get; set; }
        public object shirtNumber { get; set; }
        public DateTime lastUpdated { get; set; }
    }

    public class Scorer
    {
        public Player player { get; set; }
        public TeamTopScorer team { get; set; }
        public string playedMatches { get; set; }
        public string goals { get; set; }
        public string assists { get; set; }
        public string penalties { get; set; }
    }

    public class TeamTopScorer
    {
        public int id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string tla { get; set; }
        public string crest { get; set; }
        public string address { get; set; }
        public string website { get; set; }
        public int founded { get; set; }
        public string clubColors { get; set; }
        public string venue { get; set; }
        public DateTime lastUpdated { get; set; }
    }
    #endregion

    #region API-Football Models
    public class LeagueAPIFootball
    {
        public int id { get; set; }
        public string name { get; set; }
        public string country { get; set; }
        public string logo { get; set; }
        public string flag { get; set; }
        public string season { get; set; }
        public List<List<StandingAPIFootball>> standings { get; set; }
    }
    public class All
    {
        public string played { get; set; }
        public string win { get; set; }
        public string draw { get; set; }
        public string lose { get; set; }
        public Goals goals { get; set; }
    }

    public class Away
    {
        public string played { get; set; }
        public string win { get; set; }
        public string draw { get; set; }
        public string lose { get; set; }
        public Goals goals { get; set; }
    }

    public class Goals
    {
        public string @for { get; set; }
        public string against { get; set; }
    }

    public class Home
    {
        public string played { get; set; }
        public string win { get; set; }
        public string draw { get; set; }
        public string lose { get; set; }
        public Goals goals { get; set; }
    }

    public class StandingAPIFootball
    {
        public int rank { get; set; }
        public TeamAPIFootball team { get; set; }
        public string points { get; set; }
        public string goalsDiff { get; set; }
        public string group { get; set; }
        public string form { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public All all { get; set; }
        public Home home { get; set; }
        public Away away { get; set; }
        public DateTime update { get; set; }
    }
    public class Paging
    {
        public int current { get; set; }
        public int total { get; set; }
    }

    public class ParametersAPIFootball
    {
        public string league { get; set; }
        public string season { get; set; }
    }

    public class ResponseAPIFootball
    {
        public LeagueAPIFootball league { get; set; }
    }
    public class TeamAPIFootball
    {
        public int id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
    }
    // Player Stats stuff -
    public class Birth
    {
        public string date { get; set; }
        public string place { get; set; }
        public string country { get; set; }
    }

    public class Cards
    {
        public string yellow { get; set; }
        public string yellowred { get; set; }
        public string red { get; set; }
    }

    public class Dribbles
    {
        public string attempts { get; set; }
        public string success { get; set; }
        public object past { get; set; }
    }

    public class Duels
    {
        public string total { get; set; }
        public string won { get; set; }
    }

    public class Fouls
    {
        public string drawn { get; set; }
        public string committed { get; set; }
    }

    public class Games
    {
        public string appearences { get; set; }
        public string lineups { get; set; }
        public string minutes { get; set; }
        public object number { get; set; }
        public string position { get; set; }
        public string rating { get; set; }
        public bool captain { get; set; }
    }

    public class GoalsPlayerStats
    {
        public string total { get; set; }
        public string conceded { get; set; }
        public string assists { get; set; }
        public string saves { get; set; }
    }

    public class LeaguePlayerStats
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string country { get; set; }
        public string logo { get; set; }
        public string flag { get; set; }
        public object season { get; set; }
    }

    public class ParametersPlayerStats
    {
        public string season { get; set; }
        public string team { get; set; }
    }

    public class Passes
    {
        public string total { get; set; }
        public string key { get; set; }
        public string accuracy { get; set; }
    }

    public class Penalty
    {
        public object won { get; set; }
        public object commited { get; set; }
        public string scored { get; set; }
        public string missed { get; set; }
        public string saved { get; set; }
    }

    public class PlayerPlayerStats
    {
        public int id { get; set; }
        public string name { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public int age { get; set; }
        public Birth birth { get; set; }
        public string nationality { get; set; }
        public string height { get; set; }
        public string weight { get; set; }
        public bool injured { get; set; }
        public string photo { get; set; }
    }

    public class ResponsePlayerStats
    {
        public PlayerPlayerStats player { get; set; }
        public List<Statistic> statistics { get; set; }
    }

    public class Shots
    {
        public string total { get; set; }
        public string on { get; set; }
    }

    public class Statistic
    {
        public TeamAPIFootball team { get; set; }
        public LeaguePlayerStats league { get; set; }
        public Games games { get; set; }
        public Substitutes substitutes { get; set; }
        public Shots shots { get; set; }
        public GoalsPlayerStats goals { get; set; }
        public Passes passes { get; set; }
        public Tackles tackles { get; set; }
        public Duels duels { get; set; }
        public Dribbles dribbles { get; set; }
        public Fouls fouls { get; set; }
        public Cards cards { get; set; }
        public Penalty penalty { get; set; }
    }

    public class Substitutes
    {
        public string @in { get; set; }
        public string @out { get; set; }
        public string bench { get; set; }
    }

    public class Tackles
    {
        public string total { get; set; }
        public string blocks { get; set; }
        public string interceptions { get; set; }
    }

    #endregion
}