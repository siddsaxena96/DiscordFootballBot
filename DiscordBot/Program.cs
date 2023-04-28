using System;
using System.Drawing;

namespace DiscordBot
{
    internal class Program
    {
        private static BotController _botController;
        private static bool isDailyTaskRunning = true;
        static async Task Main(string[] args)
        {
            _botController = new BotController();
            _botController.RunAsync().GetAwaiter().GetResult();

            Timer dailyTimer = new Timer(async (state) => await DailyTask(), null, TimeSpan.Zero, TimeSpan.FromHours(24));
            
            Timer halfHourlyTimer = new Timer(async (state) => { if (!isDailyTaskRunning) await HalfHourlyTask(); }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

            Console.ReadLine();                                 
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
