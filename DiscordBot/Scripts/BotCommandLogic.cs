using DSharpPlus.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiscordBot
{
    public static class BotCommandLogic
    {
        private static Dictionary<string, List<Team>> _teamDataCache = new(5);
        private static Dictionary<string, List<Team_Old>> _footballDataOrgTeamsCache = new(5);
        private static Dictionary<int, int> _footballDataOrgTeamIdToAPIFootballTeamId = new(100);

        private static Dictionary<int, List<ResponsePlayerStats>> _teamPlayersCache = new(20);

        private static List<ResponsePlayerStats> _playerStatsHelper = new(25);

        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<List<string>> _tableData = new(5);
        private static List<string> _stringList = new(5);
        private static List<Team> _teamList = new(5);
        private static string subscriptionFileLocation = "./subscription.json";
        private static HttpClient _httpClient;

        public static void Init()
        {
            _httpClient = new HttpClient();
        }
        public async static Task<string> SubscribeTo(string competitionId, string teamId)
        {
            foreach (var team in _teamDataCache[competitionId])
            {
                if (team.teamId == teamId)
                {
                    bool result = await UpdateSubscriptionList(team);
                    if (result)
                    {
                        return $"Congratulations ! You are now subscribed to {team.teamName}";
                    }
                    else
                    {
                        return $"You are already subscribed to {team.teamName}";
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
                if (sub.team.teamId == team.teamId)
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
            string url = $"teams/{sub.team.teamId}/matches?status=SCHEDULED&dateFrom={currentDate}&dateTo={nextDateString}";
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
            if (_teamDataCache.TryGetValue(competitionCode, out var teamData)) return teamData;

            _teamDataCache.Add(competitionCode, new(20));

            var leagueTableString = BotController.Configuration.baseURL + BotController.Configuration.leagueTableURL.Replace("***", competitionCode); ;
            var html = _httpClient.GetStringAsync(leagueTableString).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var leagueTableLeft = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'Table Table--align-right Table--fixed Table--fixed-left')]");
            if (leagueTableLeft == null) return null;
            var leagueTeams = leagueTableLeft.SelectNodes("//span[@class='hide-mobile']/a[@class='AnchorLink']");
            if (leagueTeams == null) return null;


            foreach (var team in leagueTeams)
            {
                string teamName = team.InnerText;
                string hrefValue = team.GetAttributeValue("href", "");
                string teamId = ExtractTeamID(hrefValue);
                _teamDataCache[competitionCode].Add(new(teamName, teamId));
            }
            return _teamDataCache[competitionCode];

            string ExtractTeamID(string teamHref)
            {
                string pattern = @"/id/(\d+)/";
                System.Text.RegularExpressions.Match match = Regex.Match(teamHref, pattern);

                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                return "-1";
            }
        }

        public async static Task<string> GetStandingsForCompetition(string competitionCode, List<string> stringsToSendBack)
        {
            var leagueTableString = BotController.Configuration.baseURL + BotController.Configuration.leagueTableURL.Replace("***", competitionCode); ;
            var html = _httpClient.GetStringAsync(leagueTableString).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var leagueTableLeft = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'Table Table--align-right Table--fixed Table--fixed-left')]");
            if (leagueTableLeft == null) return "Sorry, Unable to fetch standings";
            var leagueTeams = leagueTableLeft.SelectNodes("//span[@class='hide-mobile']/a[@class='AnchorLink']");

            var leagueTableRight = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'Table Table--align-right')]");
            if (leagueTableRight == null) return "Sorry, Unable to fetch standings";
            var leagueTeamStats = leagueTableRight.SelectNodes("//span[contains(@class, 'stat-cell')]");

            if (leagueTeams == null || leagueTeams.Count == 0 || leagueTeamStats == null || leagueTeamStats.Count == 0) return "Sorry, Unable to fetch standings";

            var seasonYear = htmlDocument.DocumentNode.SelectSingleNode("//select[@aria-label='Standings Season']/option[@selected]").InnerText;
            var competitionName = htmlDocument.DocumentNode.SelectSingleNode("//select[@aria-label='Standings Season Type']/option[@selected]").InnerText;

            int positionCounter = 1;
            _tableData.Clear();
            _tableData.Add(new() { "Pos", "Team", "Played", "Win", "Draw", "Loss", "GF", "GA", "GD", "Pts" });
            stringsToSendBack.Clear();
            int statCounter = 0;
            for (int i = 0; i < leagueTeams.Count; i++)
            {
                _stringList.Clear();
                _stringList.Add(positionCounter.ToString());
                _stringList.Add(leagueTeams[i].InnerText);
                for (int j = statCounter; j <= statCounter + 7; j++)
                {
                    _stringList.Add(leagueTeamStats[j].InnerText);
                }
                statCounter += 8;
                positionCounter++;
                _tableData.Add(new(_stringList));
            }
            CreateTable(_tableData, stringsToSendBack, 1800);

            return $"{competitionName}\t| {seasonYear} |\n";
        }

        public async static Task<string> GetTeamFixtures(string extractedId, List<string> responseStrings, int upUntil = -1)
        {
            var fixturesUrl = BotController.Configuration.baseURL + BotController.Configuration.fixturesURL.Replace("***", extractedId);
            var html = _httpClient.GetStringAsync(fixturesUrl).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var rows = htmlDocument.DocumentNode.SelectNodes("//tbody[@class='Table__TBODY']//tr");
            _tableData.Clear();
            _tableData.Add(new() { "DATE", "HOME", "V", "AWAY", "TIME", "COMPETITION" });
            for (int i = 0; i < rows.Count; i++)
            {
                HtmlNode row = rows[i];
                var date = row.SelectSingleNode(".//div[@class='matchTeams']/text()").InnerText;
                var home_team = row.SelectSingleNode(".//div[@class='local flex items-center']/a[@class='AnchorLink Table__Team']/text()").InnerText;
                var away_team = row.SelectSingleNode(".//div[@class='away flex items-center']/a[@class='AnchorLink Table__Team']/text()").InnerText;
                var home_team_logo = row.SelectSingleNode(".//div[@class='local flex items-center']/a[@class='AnchorLink Table__Team']/img/@src");
                var away_team_logo = row.SelectSingleNode(".//div[@class='away flex items-center']/a[@class='AnchorLink Table__Team']/img/@src");
                var time = row.SelectSingleNode(".//td[@class='Table__TD'][5]/a[@class='AnchorLink']/text()").InnerText;
                var competition = row.SelectSingleNode(".//td[@class='Table__TD'][6]/span/text()").InnerText;
                _tableData.Add(new() { date, home_team, "V", away_team, time, competition });
                Console.WriteLine($"{i} - {upUntil}");
                if (upUntil > -1 && i == upUntil - 1) break;
            }
            CreateTable(_tableData, responseStrings, 1800);
            return "Upcoming Matches :";
        }

        public async static Task RefreshTeamsCache()
        {
            var values = Enum.GetValues(typeof(FDataLeagueOptions));

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
                        listOfTeams.Add(team);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not refresh teams cache {ex.Message}");
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1)); //Only 10 calls allowed per minute 
            _footballDataOrgTeamIdToAPIFootballTeamId.Clear();

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
                foreach (var team in competitionStandings.standings[0].table)
                {
                    foreach (var teamToMatch in apiFootballStandings.response[0].league.standings[0])
                    {
                        if (team.position == teamToMatch.rank.ToString())
                        {
                            matched++;
                            _footballDataOrgTeamIdToAPIFootballTeamId.Add(team.team.id, teamToMatch.team.id);
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

        public async static Task<string> GetUpcomingFixtureForTeam(string teamId, long numMatches, List<string> responseStrings)
        {            
            return await GetTeamFixtures(teamId, responseStrings, (int)numMatches);
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
                _tableData.Add(new() { "Pos", "Name", "Team", "Goals", "Assists", "Penalties", "Played" });
                int pos = 1;
                foreach (var scorer in topScorers.scorers)
                {
                    _tableData.Add(new() { pos.ToString(), scorer.player.name, scorer.team.name, scorer.goals, scorer.assists ?? "NA", scorer.penalties ?? "NA", scorer.playedMatches ?? "NA" });
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
                Console.WriteLine($"Matched {teamId} to {teamIdAPIFootball}");
                _playerStatsHelper.Clear();

                if (_teamPlayersCache.TryGetValue(teamIdAPIFootball, out var playerList))
                {
                    Console.WriteLine($"Fom Dict");
                    _playerStatsHelper.AddRange(playerList);
                }
                else
                {
                    string url = $"players?team={teamIdAPIFootball}&season={GetCurrentFootballSeason()}";
                    var response = await APIController.GetRequestAsync(url, APIChoice.APIFootball);
                    Console.WriteLine(response);
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
                    Console.WriteLine($"FETCHED PLAYERS = {_playerStatsHelper.Count}");
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
                    tableHeader.AddRange(new List<string> { "Competition", "Team", "App", "Start", "Minutes", "Gls", "Ast", "Pass", "K.Pass", "Drib Att", "Drib Succ", "Tck", "Int" });
                }
                else
                {
                    isKeeper = true;
                    tableHeader.AddRange(new List<string> { "Competition", "Team", "App", "Start", "Minutes", "Sav", "Gls Conc", "Pass", "K.Pass", "Ast", "Gls" });
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
            int curSeason = GetCurrentFootballSeason();
            return $"Displaying Stats For {curSeason} - {curSeason + 1} Season";
        }

        public static void ClearTeamStatsCache()
        {
            _teamPlayersCache.Clear();
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
                tableString += new string('-', colWidths[col] + 2) + "+";
            }
            tableString += "\n";

            // Set up table body
            for (int row = 0; row < numRows; row++)
            {
                tableString += ("|");
                for (int col = 0; col < numCols; col++)
                {
                    string cellValue = tableData[row][col];
                    int padding = (colWidths[col] - cellValue.Length) / 2;
                    string paddedValue = cellValue.PadLeft(padding + cellValue.Length).PadRight(colWidths[col]);
                    tableString += " " + paddedValue + " |";
                }
                tableString += "\n";

                // Set up separator between rows
                tableString += ("+");
                for (int col = 0; col < numCols; col++)
                {
                    tableString += new string('-', colWidths[col] + 2) + "+";
                }
                tableString += "\n";

                if (tableString.Length >= charLim)
                {
                    stringsToSendBack.Add(tableString);
                    tableString = "";
                }
            }

            if (!string.IsNullOrEmpty(tableString))
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
