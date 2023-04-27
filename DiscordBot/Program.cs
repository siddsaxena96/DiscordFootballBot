namespace DiscordBot
{
    internal class Program
    {
        private static BotController _botController;
        static async Task Main(string[] args)
        {
            _botController = new BotController();
            _botController.RunAsync().GetAwaiter().GetResult();
            
            await BotCommandLogic.RefreshTeamsCache();
            
            CancellationTokenSource cts = new CancellationTokenSource();

            // Create a periodic timer that will call the DoWork method every 5 minutes
            TimeSpan period = TimeSpan.FromMinutes(30);
            using var timer = new Timer(async _ => await DoWork(), null, TimeSpan.Zero, period);

            // Wait for the user to press a key to cancel the timer
            Console.WriteLine("Press any key to stop the timer...");
            await Console.In.ReadLineAsync();
            cts.Cancel();
        }

        static async Task DoWork()
        {
            // Do your work here
            Console.WriteLine($"DoWork started at { BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");

            // Simulate some async work
            await _botController.RoutineCheckUpcomingMatches();

            Console.WriteLine($"DoWork completed at {BotCommandLogic.FormatTimeToIST(DateTime.UtcNow)}");
        }
    }
}
