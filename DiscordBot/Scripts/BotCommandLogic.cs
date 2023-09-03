using DSharpPlus.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiscordBot
{
    public static class BotCommandLogic
    {
        private static Dictionary<string, List<Team>> _teamDataCache = new(5);
        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<List<string>> _tableData = new(5);
        private static List<string> _stringList = new(5);        
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
                if (sub.Team.teamId == team.teamId)
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
            DateTime nextDate = DateTime.Now.AddDays(1);
            string nextDateString = nextDate.ToString("yyyy-MM-dd");
            foreach (var sub in _subscriptions)
            {
                await CheckForUpcomingMatch(sub, currentDate, nextDateString, matchReminders);
            }
            async static Task CheckForUpcomingMatch(SubscriptionDetails sub, string currentDate, string nextDateString, List<DiscordEmbed> matchReminders)
            {
                var fixturesUrl = BotController.Configuration.baseURL + BotController.Configuration.fixturesURL.Replace("***", sub.Team.teamId);
                var html = _httpClient.GetStringAsync(fixturesUrl).Result;
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var row = htmlDocument.DocumentNode.SelectSingleNode("//tbody[@class='Table__TBODY']//tr");
                var date = row.SelectSingleNode(".//div[@class='matchTeams']/text()").InnerText;
                var home_team = row.SelectSingleNode(".//div[@class='local flex items-center']/a[@class='AnchorLink Table__Team']/text()").InnerText;
                var away_team = row.SelectSingleNode(".//div[@class='away flex items-center']/a[@class='AnchorLink Table__Team']/text()").InnerText;
                var home_team_logo = row.SelectSingleNode(".//div[@class='local flex items-center']/a[@class='AnchorLink Table__Team']/img/@src");
                var away_team_logo = row.SelectSingleNode(".//div[@class='away flex items-center']/a[@class='AnchorLink Table__Team']/img/@src");
                var time = row.SelectSingleNode(".//td[@class='Table__TD'][5]/a[@class='AnchorLink']/text()").InnerText;
                var competition = row.SelectSingleNode(".//td[@class='Table__TD'][6]/span/text()").InnerText;

                var matchTime = ConvertToUTCTime(date, time);

                TimeSpan timeDifference = matchTime - DateTime.UtcNow;

                bool remind = timeDifference.TotalHours is < 24 and >= 23.5
                    || timeDifference.TotalHours is < 12 and >= 11.5
                    || timeDifference.TotalHours is < 1 and > 0;

                if (!remind) return;

                DiscordEmbedBuilder embedMessage = new()
                {
                    Title = $"{home_team} VS {away_team}",
                    Description = $"{competition}\n\t{time}\n"
                };
                matchReminders.Add(embedMessage);
            }

            static DateTime ConvertToUTCTime(string dateString, string timeString)
            {
                if (string.IsNullOrEmpty(dateString) || dateString == "TBD" || string.IsNullOrEmpty(timeString) || timeString == "TBD")
                    return DateTime.MinValue;

                DateTime istMatchDate = DateTime.MinValue;
                string[] dateParts = dateString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (dateParts.Length == 3)
                {
                    //dateParts[0] is day of the week
                    if (int.TryParse(dateParts[1], out int day))
                    {
                        string monthAbbreviation = dateParts[2];
                        DateTime parsedDate;

                        if (DateTime.TryParseExact(monthAbbreviation, "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                        {
                            // Determine the year (you can set it to a specific year)
                            int year = CalculateYear(day, parsedDate);

                            // Create the DateTime object

                            istMatchDate = new DateTime(year, parsedDate.Month, day);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Date Skipped {dateString}");
                }

                TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

                DateTime istTime = DateTime.ParseExact(timeString, "h:mm tt", CultureInfo.InvariantCulture);

                DateTime finalDateTime = new DateTime(istMatchDate.Year, istMatchDate.Month, istMatchDate.Day, istTime.Hour, istTime.Minute, istTime.Second);
                Console.WriteLine(finalDateTime);
                DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(istTime, istTimeZone);
                return istMatchDate;                
            }
            /// <summary>
            /// Calculates the year for the date based on day and month passed, it the day and month have passed in current year it will return the next year
            /// </summary>
            /// <param name="day"></param>
            /// <param name="parsedDate"></param>
            /// <returns></returns>
            static int CalculateYear(int day, DateTime parsedDate)
            {
                var istDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                if (parsedDate.Month <= istDateTime.Month)
                {
                    if (day < istDateTime.Day)
                    {
                        return DateTime.UtcNow.Year + 1;
                    }
                }
                return DateTime.UtcNow.Year;
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
        public async static Task<string> GetUpcomingFixtureForTeam(string teamId, long numMatches, List<string> responseStrings)
        {
            return await GetTeamFixtures(teamId, responseStrings, (int)numMatches);
        }
        public async static Task<string> GetLeagueStatsForCompetition(string competitionCode, List<string> responseStrings, int statType = -1)
        {
            var leagueStats = BotController.Configuration.baseURL + BotController.Configuration.leagueStatsURL.Replace("***", competitionCode);
            var html = _httpClient.GetStringAsync(leagueStats).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            HtmlNode selectedTable = null;
            _tableData.Clear();
            string tableHeader = null;
            if (statType == 0)
            {
                selectedTable = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='ResponsiveTable top-score-table']//table[@class='Table']");
                _tableData.Add(new() { "Rank", "Name", "Team", "GP", "Goals" });
                tableHeader = "Top Scorers :\n";
            }
            else if (statType == 1)
            {
                selectedTable = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='ResponsiveTable top-assists-table']//table[@class='Table']");
                _tableData.Add(new() { "Rank", "Name", "Team", "GP", "Assists" });
                tableHeader = "Top Assists :\n";
            }
            PopulateLeagueStatTable(responseStrings, selectedTable, tableHeader);

            return "League Stats :";
            static void PopulateLeagueStatTable(List<string> responseStrings, HtmlNode selectedTable, string tableHeader)
            {
                if (selectedTable == null) return;

                var rows = selectedTable.SelectNodes(".//tr").Skip(1).ToList();
                int rowCount = rows.Count() > 10 ? 10 : rows.Count();
                for (int i = 0; i < rowCount; i++)
                {
                    HtmlNode row = rows[i];
                    var cells = row.SelectNodes(".//td");

                    if (cells != null && cells.Count >= 5)
                    {
                        _tableData.Add(new() { cells[0].InnerText, cells[1].InnerText, cells[2].InnerText, cells[3].InnerText, cells[4].InnerText });
                    }
                }
                CreateTable(_tableData, responseStrings, 1800, tableHeader);
            }
        }

        public async static Task RefreshTeamsCache()
        {

        }

        public static void ClearTeamStatsCache()
        {
        }
        private static void CreateTable(List<List<string>> tableData, List<string> stringsToSendBack, int charLim, string header = "")
        {
            string tableString = string.IsNullOrEmpty(header) ? "" : header;

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
    }
}
