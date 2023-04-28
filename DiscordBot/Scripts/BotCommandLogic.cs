using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Globalization;

namespace DiscordBot
{
    public static class BotCommandLogic
    {
        private static Dictionary<string, List<Team>> _footballDataOrgTeamsCache = new(5);
        private static Dictionary<int, int> _footballDataOrgTeamIdToAPIFootballTeamId = new(100);

        private static Dictionary<int, List<ResponsePlayerStats>> _teamPlayersCache = new(20);

        private static List<ResponsePlayerStats> _playerStatsHelper = new(25);

        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<List<string>> _tableData = new(5);
        private static string subscriptionFileLocation = "./subscription.json";

        public async static Task<string> SubscribeTo(string competitionId, long teamId)
        {
            foreach (var team in _footballDataOrgTeamsCache[competitionId])
            {
                if (team.id == teamId)
                {
                    bool result = await UpdateSubscriptionList(team);
                    if (result)
                    {
                        return $"Congratulations ! You are now subscribed to {team.name}";
                    }
                    else
                    {
                        return $"You are already subscribed to {team.name}";
                    }
                }
            }
            return $"Sorry, unable to subscribe to the team at this time";
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
        private async static Task<bool> UpdateSubscriptionList(Team team)
        {
            await GetSubscriptions(_subscriptions);

            foreach (var sub in _subscriptions)
            {
                if (sub.team.id == team.id)
                {
                    return false;
                }
            }

            _subscriptions.Add(new SubscriptionDetails(team));

            string output = JsonConvert.SerializeObject(_subscriptions);
            using StreamWriter writer = new StreamWriter(subscriptionFileLocation, false);
            await writer.WriteAsync(output);
            return true;
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
            string url = $"teams/{sub.team.id}/matches?status=SCHEDULED&dateFrom={currentDate}&dateTo={nextDateString}";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

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

        public async static Task<IReadOnlyList<Team>> GetTeamsFromCompetition(string competitionCode)
        {
            if (_footballDataOrgTeamsCache.TryGetValue(competitionCode, out var teamList)) return teamList;

            string url = $"competitions/{competitionCode}/teams";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

            if (response == "FAIL") return null;

            try
            {
                _footballDataOrgTeamsCache.Add(competitionCode, new(20));
                var listOfTeams = _footballDataOrgTeamsCache[competitionCode];


                var competitionTeams = JsonConvert.DeserializeObject<CompetitionTeamsResponse>(response);

                foreach (var team in competitionTeams.teams)
                {
                    if (!listOfTeams.Contains(team))
                    {
                        listOfTeams.Add(team);
                    }
                }
                return listOfTeams;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Trying to get competition teams {ex.Message}");
                return null;
            }
        }
        public async static Task<string> GetStandingsForCompetition(string competitionCode, List<string> stringsToSendBack)
        {
            string url = $"competitions/{competitionCode}/standings";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

            if (response == "FAIL") return "Sorry, Unable to fetch standings";

            try
            {
                var competitionStandings = JsonConvert.DeserializeObject<CompetitionStandingsResponse>(response);

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
                CreateTable(_tableData, stringsToSendBack, 1500);

                return $"{competitionStandings.competition.name}\t|Start Date : {FormatStringToDDMMYYYY(competitionStandings.season.startDate)}|\t|End Date : {FormatStringToDDMMYYYY(competitionStandings.season.endDate)}|\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in printing table {ex.Message}");
                return $"Sorry, Unable to print standings right now";
            }
        }

        public async static Task RefreshTeamsCache()
        {
            var values = Enum.GetValues(typeof(APIFootballLeagueOptions));

            foreach (var value in values)
            {
                string league = value.ToString();
                string url = $"competitions/{league}/teams";
                string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

                if (response == "FAIL") continue;

                _footballDataOrgTeamsCache.Remove(league);

                try
                {
                    _footballDataOrgTeamsCache.Add(league, new(20));
                    var listOfTeams = _footballDataOrgTeamsCache[league];

                    var competitionTeams = JsonConvert.DeserializeObject<CompetitionTeamsResponse>(response);

                    foreach (var team in competitionTeams.teams)
                    {
                        if (!listOfTeams.Contains(team))
                        {
                            listOfTeams.Add(team);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not refresh teams cache {ex.Message}");
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1)); //Only 10 calls allowed per minute 

            foreach (var value in values)
            {
                string league = value.ToString();
                Enum.TryParse(league, out APIFootballLeagueOptions options);
                int leagueAPIFootball = (int)options;

                string url = $"competitions/{league}/standings";
                string standingsFootballOrg = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);
                var competitionStandings = JsonConvert.DeserializeObject<CompetitionStandingsResponse>(standingsFootballOrg);

                url = $"standings?league={leagueAPIFootball}&season={GetCurrentFootballSeason()}";
                var standingsAPIFootball = await APIController.GetRequestAsync(url, APIChoice.APIFootball);
                var apiFootballStandings = JsonConvert.DeserializeObject<CompetitionStandingsResponseAPIFootball>(standingsAPIFootball);

                int matched = 0;
                _footballDataOrgTeamIdToAPIFootballTeamId.Clear();
                foreach (var team in competitionStandings.standings[0].table)
                {
                    foreach (var teamToMatch in apiFootballStandings.response[0].league.standings[0])
                    {
                        if (team.position == teamToMatch.rank.ToString())
                        {
                            matched++;
                            _footballDataOrgTeamIdToAPIFootballTeamId.Add(team.team.id, teamToMatch.team.id);
                            Console.WriteLine($"{team.team.name} - {teamToMatch.team.name}");
                        }
                    }
                }
            }
        }

        private static int GetCurrentFootballSeason()
        {
            int currentYear = DateTime.Now.Year;
            return DateTime.Now.Month >= 8 ? currentYear : (currentYear - 1);
        }

        public async static Task<string> GetAllScheduledFixturesForTeam(long teamId)
        {
            string message = "";
            string url = $"teams/{teamId}/matches?status=SCHEDULED";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

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
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Getting Scheduled Match {ex.Message}");
                return "Sorry, Unable to fetch next fixture";
            }
        }
        public async static Task<string> GetUpcomingFixtureForTeam(long teamId)
        {
            string url = $"teams/{teamId}/matches?status=SCHEDULED";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

            if (response == "FAIL") return "Sorry, Unable to fetch next fixture";

            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                if (fixtureList.matches.Count == 0)
                    return "Team has no upcoming matches";
                var match = fixtureList.matches[0];
                string message = $"**{match.homeTeam.name} VS {match.awayTeam.name}**\n\t{match.competition.name}\n\t{FormatTimeToIST(match.utcDate)}\n\n";
                return message;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Getting Scheduled Match {ex.Message}");
                return "Sorry, Unable to fetch next fixture";
            }
        }
        public async static Task<string> GetTopScorersForCompetition(string competitionCode, List<string> responseStrings)
        {
            string url = $"competitions/{competitionCode}/scorers";
            string response = await APIController.GetRequestAsync(url, APIChoice.FootbalDataOrg);

            if (response == "FAIL") return "Sorry, unable to fetch top scorers right now";
            try
            {
                var topScorers = JsonConvert.DeserializeObject<CompetitionTopScorerResponse>(response);
                _tableData.Clear();
                _tableData.Add(new() { "Pos", "Name", "Team", "G", "A", "P", "PL" });
                int pos = 1;
                foreach (var scorer in topScorers.scorers)
                {
                    _tableData.Add(new() { pos.ToString(), scorer.player.name, scorer.team.name, scorer.goals, scorer.assists ?? "0", scorer.penalties ?? "0", scorer.playedMatches ?? "0" });
                    pos++;
                }
                CreateTable(_tableData, responseStrings, 1800);
                return $"{topScorers.competition.name}\t|Start Date : {FormatStringToDDMMYYYY(topScorers.season.startDate)}|\t|End Date : {FormatStringToDDMMYYYY(topScorers.season.endDate)}|\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in fetching top scorers {ex.Message}");
                return $"Sorry, unable to print top scorers right now";
            }
        }
        public async static Task GetPlayerNamesFromTeam(long teamId, List<(int playerIndex, string playerName)> playerNames)
        {
            if (_footballDataOrgTeamIdToAPIFootballTeamId.TryGetValue(Convert.ToInt32(teamId), out int teamIdAPIFootball))
            {
                _playerStatsHelper.Clear();
                
                if (_teamPlayersCache.TryGetValue(teamIdAPIFootball, out var playerList))
                {
                    _playerStatsHelper.AddRange(playerList);
                }
                else
                {
                    string url = $"players?team={teamIdAPIFootball}&season={GetCurrentFootballSeason()}";
                    var response = await APIController.GetRequestAsync(url, APIChoice.APIFootball);

                    if (response == "FAIL") return;

                    var teamPlayersResponse = JsonConvert.DeserializeObject<TeamPlayersResponseAPIFootball>(response);                    

                    while (teamPlayersResponse.paging.current <= teamPlayersResponse.paging.total)
                    {
                        _playerStatsHelper.AddRange(teamPlayersResponse.response);

                        if (teamPlayersResponse.paging.current == teamPlayersResponse.paging.total) break;

                        url = $"players?team={teamIdAPIFootball}&season={GetCurrentFootballSeason()}&page={teamPlayersResponse.paging.current + 1}";
                        response = await APIController.GetRequestAsync(url, APIChoice.APIFootball);

                        if (response == "FAIL") break;

                        teamPlayersResponse = JsonConvert.DeserializeObject<TeamPlayersResponseAPIFootball>(response);
                    }
                    _teamPlayersCache.Add(teamIdAPIFootball, new(_playerStatsHelper));
                }
                
                //Cleaning up list since we only have 25 options
                for (int i = _playerStatsHelper.Count - 1; i >= 0; i--)
                {
                    ResponsePlayerStats player = _playerStatsHelper[i];
                    if (player.statistics.Count == 0
                        || string.IsNullOrEmpty(player.statistics[0].games.minutes) || Convert.ToInt32(player.statistics[0].games.minutes) < 90
                        || string.IsNullOrEmpty(player.statistics[0].games.appearences) || Convert.ToInt32(player.statistics[0].games.appearences) < 5)
                    {
                        _playerStatsHelper.RemoveAt(i);
                    }
                }

                playerNames.Clear();
                for (int i = 0; i < _playerStatsHelper.Count; i++)
                {
                    var playerName = _playerStatsHelper[i].player.firstname + " " + _playerStatsHelper[i].player.lastname;
                    playerNames.Add((i, playerName));
                }
            }
        }

        public async static Task<string> GetPlayerStats(int playerIndex, List<string> responseStrings)
        {
            var player = _playerStatsHelper[playerIndex];
            _tableData.Clear();
            _tableData.Add(new() { "Player Name", "DOB", "Age", "Nationality", "Height", "Weight" });
            _tableData.Add(new() { player.player.firstname + " " + player.player.lastname, FormatStringToDDMMYYYY(player.player.birth.date), player.player.age.ToString(), player.player.nationality ?? "NA", player.player.height ?? "NA", player.player.weight ?? "NA" });
            CreateTable(_tableData, responseStrings, 1800);
            if (player.statistics.Count > 0)
            {
                bool isKeeper = false;
                List<string> tableHeader = new();
                if (player.statistics[0].games.position != "Goalkeeper")
                {
                    tableHeader.AddRange(new List<string> { "Comp", "Team", "App", "St", "Min", "G", "A", "P", "KP", "DA", "DS", "Tck", "Int" });
                }
                else
                {
                    isKeeper = true;
                    tableHeader.AddRange(new List<string> { "Comp", "Team", "App", "St", "Min", "Sav", "GC", "P", "KP", "A", "G" });
                }
                foreach (var competitionStats in player.statistics)
                {
                    _tableData.Clear();
                    _tableData.Add(tableHeader);
                    if (!isKeeper)
                    {
                        _tableData.Add(new() { competitionStats.league.name,competitionStats.team.name,competitionStats.games.appearences,competitionStats.games.lineups,competitionStats.games.minutes,
                            competitionStats.goals.total??"NA",competitionStats.goals.assists??"NA",competitionStats.passes.total??"NA",competitionStats.passes.key??"NA",
                            competitionStats.dribbles.attempts??"NA",competitionStats.dribbles.success??"NA",competitionStats.tackles.total??"NA",competitionStats.tackles.interceptions??"NA"});
                    }
                    else
                    {
                        _tableData.Add(new() { competitionStats.league.name, competitionStats.team.name, competitionStats.games.appearences, competitionStats.games.lineups, competitionStats.games.minutes,
                            competitionStats.goals.saves??"NA",competitionStats.goals.conceded??"NA",competitionStats.passes.total??"NA",competitionStats.passes.key??"NA",
                            competitionStats.goals.assists??"NA",competitionStats.goals.total??"NA"});
                    }
                    CreateTable(_tableData, responseStrings, 1800);
                }
            }
            return "hehe";
        }

        private static void CreateTable(List<List<string>> tableData, List<string> stringsToSendBack, int charLim)
        {
            string tableString = "";

            // Determine the number of rows and columns in the table data
            int numRows = tableData.Count;
            int numCols = tableData[0].Count;

            // Determine the maximum width of each column
            int[] colWidths = new int[numCols];
            for (int col = 0; col < numCols; col++)
            {
                int maxColWidth = 0;
                for (int row = 0; row < numRows; row++)
                {
                    int cellWidth = tableData[row][col].Length;
                    if (cellWidth > maxColWidth)
                    {
                        maxColWidth = cellWidth;
                    }
                }
                colWidths[col] = maxColWidth;
            }

            // Set up table header
            tableString += "+";
            for (int col = 0; col < numCols; col++)
            {
                tableString += (new string('-', colWidths[col]) + "+");
            }
            tableString += "\n";
            int rowCounter = 0;
            // Set up table body
            for (int row = 0; row < numRows; row++)
            {
                tableString += ("|");
                for (int col = 0; col < numCols; col++)
                {
                    string cellValue = tableData[row][col];
                    tableString += cellValue;
                    tableString += new string('\u0020', colWidths[col] - cellValue.Length) + "|";
                    //message += ("\u0020" + cellValue.PadRight(colWidths[col]) + "\u0020|");
                }
                tableString += "\n";

                // Set up separator between rows
                tableString += ("+");
                for (int col = 0; col < numCols; col++)
                {
                    tableString += (new string('-', colWidths[col]) + "+");
                }
                tableString += "\n";
                rowCounter++;
                if (tableString.Length >= charLim)
                {
                    stringsToSendBack.Add(tableString);
                    tableString = "";
                }
            }
            if (tableString != "")
                stringsToSendBack.Add(tableString);
        }
        public static string FormatTimeToIST(DateTime utcDate)
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, istTimeZone);
            return istDateTime.ToString("dd/MM/yyyy \n\tHH:mm") + " Hrs";
        }
        private static string FormatStringToDDMMYYYY(string yyyymmdd)
        {
            DateTime date = DateTime.ParseExact(yyyymmdd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return date.ToString("dd-MM-yyyy");
        }
    }
}
