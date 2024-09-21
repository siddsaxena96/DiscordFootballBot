using DSharpPlus.Entities;

namespace BaichungBotia
{
    public static class Program
    {
        private static bool isDailyTaskRunning = true;
        static async Task Main(string[] args)
        {
            await BotController.SetupBotController();
            BotCommandLogic.Init();
            Timer dailyTimer = new Timer(async (state) => await DailyTask(), null, TimeSpan.Zero, TimeSpan.FromHours(24));
            Timer halfHourlyTimer = new Timer(async (state) => { if (!isDailyTaskRunning) await HalfHourlyTask(); }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            await Task.Delay(-1);
        }

        private static async Task DailyTask()
        {
            isDailyTaskRunning = true;
            Console.WriteLine($"Started Daily Task {FormatTimeToIST(DateTime.UtcNow)}");
            var teamsRefresh = await BotCommandLogic.RefreshTeamsCache();
            if (!teamsRefresh)
            {
                DiscordChannel channel = await BotController.Client.GetChannelAsync(BotController.Configuration.FootyChannelId);
                await channel.SendMessageAsync($"Daily task has failed boss :(");
            }
            Console.WriteLine($"Ended Daily Task {FormatTimeToIST(DateTime.UtcNow)}");
            isDailyTaskRunning = false;
        }

        private static async Task HalfHourlyTask()
        {
            Console.WriteLine($"Started Half Hourly Task {FormatTimeToIST(DateTime.UtcNow)}");
            await HelperFunctions.RoutineCheckUpcomingMatches();
            Console.WriteLine($"Ended Half Hourly Task {FormatTimeToIST(DateTime.UtcNow)}");
        }
        private static string FormatTimeToIST(DateTime utcDate)
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, istTimeZone);
            return istDateTime.ToString("dd/MM/yyyy \n\tHH:mm") + " Hrs";
        }
    }
}