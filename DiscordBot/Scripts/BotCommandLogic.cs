using DiscordBot;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DiscordBot
{
    public static class BotCommandLogic
    {
        private static Dictionary<int, Team> _teamsCache = new(20);
        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<List<string>> _tableData = new(5);
        private static string subscriptionFileLocation = "./subscription.json";

        public async static Task<string> SubscribeTo(long teamId)
        {
            if (!_teamsCache.TryGetValue((int)teamId, out Team team))
            {
                return "Sorry, unable to subscibe to the team";
            }
            bool result = await UpdateSubscriptionList(team.id, team.name);
            if (result)
            {
                return $"Congratulations ! You are now subscribed to {team.name}";
            }
            else
            {
                return $"You are already subscribed to {team.name}";
            }
        }
        public async static Task RoutineCheckUpcomingMatches(List<DiscordEmbed> matchReminders)
        {
            await GetSubscriptions(_subscriptions);
            if (_subscriptions.Count == 0)
                return;
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            // Get the next date by adding one day to the current date
            DateTime nextDate = DateTime.Now.AddDays(1);
            string nextDateString = nextDate.ToString("yyyy-MM-dd");
            foreach (var sub in _subscriptions)
            {
                await CheckForUpcomingMatch(sub, currentDate, nextDateString, matchReminders);
            }
        }
        private async static Task CheckForUpcomingMatch(SubscriptionDetails sub, string currentDate, string nextDateString, List<DiscordEmbed> matchReminders)
        {
            string url = $"http://api.football-data.org//v4/teams/{sub.teamId}/matches?status=SCHEDULED&dateFrom={currentDate}&dateTo={nextDateString}";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);

            if (response == "FAIL") return;

            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                foreach (var match in fixtureList.matches)
                {
                    bool remind = false;
                    TimeSpan timeDifference = match.utcDate - DateTime.UtcNow;
                    Console.WriteLine($"{match.homeTeam.name} vs {match.awayTeam.name} - {timeDifference.TotalHours}");

                    remind = timeDifference.TotalHours is < 24 and >= 23.5
                        || timeDifference.TotalHours is < 12 and >= 11.5
                        || timeDifference.TotalHours is < 1 and > 0;

                    if (!remind) continue;

                    DiscordEmbedBuilder embedMessage = new()
                    {
                        Title = $"{match.homeTeam.name} VS {match.awayTeam.name}",
                        Description = $"{match.competition.name}\n\t{FormatTimeToIST(match.utcDate)}\n"
                    };
                    matchReminders.Add(embedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in trying to get Upcoming Match {ex.Message}");
            }
        }

        public async static Task<CompetitionTeamsResponse> GetTeamsFromCompetition(string competitionCode)
        {
            string url = $"http://api.football-data.org/v4/competitions/{competitionCode}/teams";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);

            if (response == "FAIL") return null;

            try
            {
                var competitionTeams = JsonConvert.DeserializeObject<CompetitionTeamsResponse>(response);
                foreach (var team in competitionTeams.teams)
                {
                    if (!_teamsCache.ContainsKey(team.id))
                    {
                        _teamsCache.Add(team.id, team);
                    }
                }
                return competitionTeams;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Trying to get competition teams {ex.Message}");
                return null;
            }
        }
        public async static Task<string> GetStandingsForCompetition(string competitionCode, List<string> stringsToSendBack)
        {
            string url = $"http://api.football-data.org/v4/competitions/{competitionCode}/standings";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);

            if (response == "FAIL") return "Sorry, Unable to fetch standings";
            
            try
            {
                var competitionStandings = JsonConvert.DeserializeObject<CompetitionStandingsResponse>(response);
                string message = string.Empty;

                _tableData.Clear();
                if (competitionCode != "SA")
                    _tableData.Add(new() { "Pos", "Team", "MP", "W", "D", "L", "GF", "GA", "GD", "Pts", "Last 5" });
                else
                    _tableData.Add(new() { "Pos", "Team", "MP", "W", "D", "L", "GF", "GA", "GD", "Pts" });
                stringsToSendBack.Clear();

                var competitionTable = competitionStandings.standings[0].table;

                foreach (var entry in competitionTable)
                {
                    if (competitionCode != "SA")
                        _tableData.Add(new() { entry.position, entry.team.name, entry.playedGames, entry.won, entry.draw, entry.lost, entry.goalsFor, entry.goalsAgainst, entry.goalDifference, entry.points, entry.form });
                    else
                        _tableData.Add(new() { entry.position, entry.team.name, entry.playedGames, entry.won, entry.draw, entry.lost, entry.goalsFor, entry.goalsAgainst, entry.goalDifference, entry.points });
                }
                var numColumns = _tableData[0].Count;
                var columnWidths = new int[numColumns];

                // Determine the number of rows and columns in the table data
                int numRows = _tableData.Count;
                int numCols = _tableData[0].Count;

                // Determine the maximum width of each column
                int[] colWidths = new int[numCols];
                for (int col = 0; col < numCols; col++)
                {
                    int maxColWidth = 0;
                    for (int row = 0; row < numRows; row++)
                    {
                        int cellWidth = _tableData[row][col].Length;
                        if (cellWidth > maxColWidth)
                        {
                            maxColWidth = cellWidth;
                        }
                    }
                    colWidths[col] = maxColWidth;
                }

                // Set up table header
                message += "+";
                for (int col = 0; col < numCols; col++)
                {
                    message += (new string('-', colWidths[col]) + "+");
                }
                message += "\n";
                int rowCounter = 0;
                // Set up table body
                for (int row = 0; row < numRows; row++)
                {
                    message += ("|");
                    for (int col = 0; col < numCols; col++)
                    {
                        string cellValue = _tableData[row][col];
                        message += cellValue;
                        message += new string('\u0020', colWidths[col] - cellValue.Length) + "|";
                        //message += ("\u0020" + cellValue.PadRight(colWidths[col]) + "\u0020|");
                    }
                    message += "\n";

                    // Set up separator between rows
                    message += ("+");
                    for (int col = 0; col < numCols; col++)
                    {
                        message += (new string('-', colWidths[col]) + "+");
                    }
                    message += "\n";
                    rowCounter++;
                    if (message.Length >= 1500)
                    {
                        stringsToSendBack.Add(message);
                        message = "";
                    }
                }

                stringsToSendBack.Add(message);

                return $"{competitionStandings.competition.name}\t|Start Date : {FormatStringToIST(competitionStandings.season.startDate)}|\t|End Date : {FormatStringToIST(competitionStandings.season.endDate)}|\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in printing table {ex.Message}");
                return $"Sorry, Unable to print standings right now";
            }
        }
        public async static Task GetSubscriptions(List<SubscriptionDetails> subscriptions)
        {
            subscriptions.Clear();
            using (FileStream fs = new FileStream(subscriptionFileLocation, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            {
                string jsonString = await sr.ReadToEndAsync();
                if (!string.IsNullOrEmpty(jsonString))
                {
                    subscriptions.AddRange(JsonConvert.DeserializeObject<List<SubscriptionDetails>>(jsonString));
                }
                sr.Close();
                fs.Close();
            }
        }
        public async static Task<string> GetAllScheduledFixturesForTeam(long teamId)
        {
            string message = "";
            string url = $"http://api.football-data.org//v4/teams/{teamId}/matches?status=SCHEDULED";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);

            if (response == "FAIL") return "Sorry, Unable to fetch next fixture";        

            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                if (fixtureList.matches.Count == 0)
                    return "Team has no upcoming matches";
                foreach (var match in fixtureList.matches)
                {
                    message += $"**{match.homeTeam.name} VS {match.awayTeam.name}**\n\t{match.competition.name}\n\t{FormatTimeToIST(match.utcDate)}\n\n";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Getting Scheduled Match {ex.Message}");
                return "Sorry, Unable to fetch next fixture";
            }
            return message;
        }
        public async static Task<string> GetUpcomingFixtureForTeam(long teamId)
        {
            string message = "";
            string url = $"http://api.football-data.org//v4/teams/{teamId}/matches?status=SCHEDULED";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);
            
            if (response == "FAIL") return "Sorry, Unable to fetch next fixture";        

            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                if (fixtureList.matches.Count == 0)
                    return "Team has no upcoming matches";
                var match = fixtureList.matches[0];
                message = $"**{match.homeTeam.name} VS {match.awayTeam.name}**\n\t{match.competition.name}\n\t{FormatTimeToIST(match.utcDate)}\n\n";
                return message;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Getting Scheduled Match {ex.Message}");
                return "Sorry, Unable to fetch next fixture";
            }            
        }
        public static string FormatTimeToIST(DateTime utcDate)
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, istTimeZone);
            return istDateTime.ToString("dd/MM/yyyy \n\tHH:mm") + " Hrs";
        }
        private static string FormatStringToIST(string yyyymmdd)
        {
            DateTime date = DateTime.ParseExact(yyyymmdd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return date.ToString("dd-MM-yyyy");
        }
        private async static Task<bool> UpdateSubscriptionList(int teamId, string teamName)
        {
            await GetSubscriptions(_subscriptions);

            foreach (var sub in _subscriptions)
            {
                if (sub.teamId == teamId)
                {
                    return false;
                }
            }

            _subscriptions.Add(new SubscriptionDetails(teamId, teamName));

            string output = JsonConvert.SerializeObject(_subscriptions);
            using StreamWriter writer = new StreamWriter(subscriptionFileLocation, false);
            await writer.WriteAsync(output);
            return true;
        }
    }
}
