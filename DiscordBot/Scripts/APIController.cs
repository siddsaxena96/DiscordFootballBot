namespace DiscordBot
{
    public static class APIController
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly Dictionary<APIChoice, List<(string header, string token)>> _apiHeaders = new(2);

        public static async Task<string> GetRequestAsync(string url, APIChoice apiChoice)
        {
            switch (apiChoice)
            {
                case APIChoice.FootbalDataOrg:
                    url = BotController.Configuration.FDataAPIEndpoint + url;
                    if (!_apiHeaders.ContainsKey(apiChoice))
                    {
                        _apiHeaders.Add(apiChoice, new() { ("X-Auth-Token", BotController.Configuration.FDataAPIToken) });
                    }
                    break;
                case APIChoice.APIFootball:
                    url = BotController.Configuration.APIFootbalAPIEndPoint + url;
                    if (!_apiHeaders.ContainsKey(apiChoice))
                    {
                        _apiHeaders.Add(apiChoice, new() { ("x-apisports-key", BotController.Configuration.APIFootballToken) });
                    }
                    break;
            }
            return await GetAsync(url, _apiHeaders[apiChoice]);
        }
        private static async Task<string> GetAsync(string url, List<(string header, string token)> headers)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            foreach (var header in headers)
            {
                request.Headers.Add(header.header, header.token);
            }

            HttpResponseMessage response = await _client.SendAsync(request);

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
