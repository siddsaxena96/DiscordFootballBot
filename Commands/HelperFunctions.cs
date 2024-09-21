using System.Globalization;
using DSharpPlus.Entities;

namespace BaichungBotia
{
    public static class HelperFunctions
    {
        private static List<DiscordEmbed> _matchReminders = new(10);        
        public async static Task RoutineCheckUpcomingMatches()
        {
            _matchReminders.Clear();
            await BotCommandLogic.RoutineCheckUpcomingMatches(_matchReminders);
            if (_matchReminders.Count > 0)
            {
                DiscordChannel channel = await BotController.Client.GetChannelAsync(BotController.Configuration.FootyChannelId);
                foreach (var embedMessage in _matchReminders)
                {
                    await channel.SendMessageAsync(embed: embedMessage);
                }
            }
        }

        public static DateTime ConvertISTToUTCTime(string dateString, string timeString)
        {
            if (string.IsNullOrEmpty(dateString) || dateString == "TBD" || string.IsNullOrEmpty(timeString) || timeString == "TBD")
                return DateTime.MinValue;

            DateTime istMatchDate = DateTime.MinValue;
            string[] dateParts = dateString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            if (dateParts.Length == 3)
            {
                //dateParts[0] is day of the week
                if (int.TryParse(dateParts[1], out int day))
                {
                    string monthAbbreviation = dateParts[2];

                    if (DateTime.TryParseExact(monthAbbreviation, "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime matchMonth))
                    {
                        int year = CalculateYear(day, matchMonth, istTimeZone);
                        istMatchDate = new DateTime(year, matchMonth.Month, day);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Date Skipped {dateString}");
            }

            DateTime istTime = DateTime.ParseExact(timeString, "h:mm tt", CultureInfo.InvariantCulture);

            DateTime finalDateTime = new(istMatchDate.Year, istMatchDate.Month, istMatchDate.Day, istTime.Hour, istTime.Minute, istTime.Second);

            return TimeZoneInfo.ConvertTimeToUtc(finalDateTime, istTimeZone);
        }

        public static int CalculateYear(int day, DateTime parsedMonth, TimeZoneInfo timeZone)
        {
            var timeZoneDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            return DateTime.UtcNow.Year + (parsedMonth.Month <= timeZoneDateTime.Month && day < timeZoneDateTime.Day ? 1 : 0);
        }
    }
}