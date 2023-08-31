using System;
using System.Drawing;
using HtmlAgilityPack;
using System.Net.Http;

namespace DiscordBot
{
    internal class Program
    {
        private static BotController _botController;
        private static bool isDailyTaskRunning = true;
        static async Task Main(string[] args)
        {
            //WebScrappingTest();
            _botController = new BotController();
            _botController.InitBot().GetAwaiter().GetResult();
            /*  

               Timer dailyTimer = new Timer(async (state) => await DailyTask(), null, TimeSpan.Zero, TimeSpan.FromHours(24));

               Timer halfHourlyTimer = new Timer(async (state) => { if (!isDailyTaskRunning) await HalfHourlyTask(); }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));*/            
            Console.ReadLine();
        }

        private static void WebScrappingTest()
        {
            var url = "https://www.espn.in/soccer/table/_/league/eng.1";
            var client = new HttpClient();
            var html = client.GetStringAsync(url).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var leagueTable = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'Table Table--align-right Table--fixed Table--fixed-left')]");
            var leagutTeams = leagueTable.SelectNodes("//span[@class='hide-mobile']/a[@class='AnchorLink']");

            var leagueTableRight = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'Table Table--align-right')]");            
            var leagueTeamStats = leagueTableRight.SelectNodes("//span[contains(@class, 'stat-cell')]");
            Console.WriteLine(leagueTeamStats.Count);
            foreach (var teamNode in leagutTeams)
            {
                string teamName = teamNode.InnerText;
                string hrefValue = teamNode.GetAttributeValue("href", "");

                Console.WriteLine($"Team Name: {teamName}");
                Console.WriteLine($"Href Value: {hrefValue}");
            }
        }      

        private static async Task DailyTask()
        {
            isDailyTaskRunning = true;
            Console.WriteLine($"Started Daily Task {BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");
            await BotCommandLogic.RefreshTeamsCache();
            Console.WriteLine($"Ended Daily Task {BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");
            isDailyTaskRunning = false;
        }

        private static async Task HalfHourlyTask()
        {
            Console.WriteLine($"Started Half Hourly Task {BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");
            await _botController.RoutineCheckUpcomingMatches();
            Console.WriteLine($"Ended Half Hourly Task {BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");
        }
    }
}
