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
        private static List<SubscriptionDetails> _subscriptions = new(5);
        private static List<(string, string)> _parametersToSend = new(2);
        private static string subscriptionFileLocation = "./subscription.json";

        public async static Task<string> SubscribeTo(string teamName, string competitionCode)
        {
            string url = $"http://api.football-data.org/v4/competitions/{competitionCode}/teams";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);
            if (!string.IsNullOrEmpty(response))
            {
                var competitionTeams = JsonConvert.DeserializeObject<CompetitionTeamsResponse>(response);
                foreach (var team in competitionTeams.teams)
                {
                    if (team.name.Contains(teamName))
                    {
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
                }
            }
            return "Sorry, could not find the team";
        }
        private async static Task GetSubscriptions(List<SubscriptionDetails> subscriptions)
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
            string message = "";
            string url = $"http://api.football-data.org//v4/teams/{sub.teamId}/matches?dateFrom={currentDate}&dateTo={nextDateString}";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);
            if (string.IsNullOrEmpty(response)) return;
            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                foreach (var match in fixtureList.matches)
                {
                    bool remind = false;
                    TimeSpan timeDifference = match.utcDate- DateTime.UtcNow;
                    Console.WriteLine($"{match.homeTeam.name} vs {match.awayTeam.name} - {timeDifference.TotalHours}");
                    if (timeDifference.TotalHours < 24 && timeDifference.TotalHours >= 23.5)
                    {
                        remind = true;
                    }
                    else if (timeDifference.TotalHours < 12 && timeDifference.TotalHours >= 11.5)
                    {
                        remind = true;
                    }
                    else if (timeDifference.TotalHours < 1)
                    {
                        remind = true;
                    }
                    
                    if (!remind)
                    {
                        continue;
                    }
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
                Console.WriteLine($"GEGEG{ex.Message}");
            }
        }

        public async static Task<string> ShowScheduledMatchesForSubscribedTeams(string teamName)
        {
            string message = "";
            await GetSubscriptions(_subscriptions);
            Console.WriteLine(_subscriptions.Count);
            if (_subscriptions.Count > 0)
            {
                foreach (var sub in _subscriptions)
                {
                    if (teamName != null && !sub.teamName.Contains(teamName)) continue;
                    message += $"**Scheduled Matches For {sub.teamName} :** \n\n";
                    message += await GetAllScheduledFixturesForTeam(sub.teamId);
                }
            }
            else
            {
                message = "No matches found";
            }

            return message;
        }
        public async static Task<string> GetAllScheduledFixturesForTeam(int teamId)
        {
            string message = "";
            string url = $"http://api.football-data.org//v4/teams/{teamId}/matches?status=SCHEDULED";
            string response = await APIController.GetAsync(url, BotController.configuration.APIToken);
            try
            {
                var fixtureList = JsonConvert.DeserializeObject<TeamFixturesResponse>(response);
                foreach (var match in fixtureList.matches)
                {
                    message += $"**{match.homeTeam.name} VS {match.awayTeam.name}**\n\t{match.competition.name}\n\t{FormatTimeToIST(match.utcDate)}\n\n";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GEGEG{ex.Message}");
            }
            return message;
        }

        private static string FormatTimeToIST(DateTime utcDate)
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, istTimeZone);
            return istDateTime.ToString("dd/MM/yyyy \n\tHH:mm") + " Hrs";
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
