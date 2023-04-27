using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public static class APIController
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetAsync(string url, string header)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("X-Auth-Token", header);

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {                
                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            else
            {
                Console.WriteLine($"Request failed with status code {response.StatusCode} {Environment.StackTrace}");
                return "FAIL";                
            }
        }
    }
}
