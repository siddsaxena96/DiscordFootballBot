using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    [Serializable]
    public class SubscriptionDetails
    {
        public int teamId { get; set; }
        public string teamName { get; set; }
        public SubscriptionDetails(int teamId, string teamName)
        {
            this.teamId = teamId;
            this.teamName = teamName;
        }
    }
    public enum LeagueOptions
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
    #region Responses 
    public class CompetitionTeamsResponse
    {
        public int count { get; set; }
        public FiltersTeamResponse filters { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public List<Team> teams { get; set; }
    }
    public class TeamFixturesResponse
    {
        public FiltersMatchResponse filters { get; set; }
        public ResultSet resultSet { get; set; }
        public List<Match> matches { get; set; }
    }

    public class CompetitionStandingsResponse
    {
        public FiltersTeamResponse filters { get; set; }
        public Area area { get; set; }
        public Competition competition { get; set; }
        public Season season { get; set; }
        public List<Standing> standings { get; set; }
    }
    #endregion
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
}