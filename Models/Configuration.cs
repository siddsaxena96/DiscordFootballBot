using Newtonsoft.Json;

[Serializable]
public class Configuration
{
    [JsonProperty("token")]
    public string Token { get; private set; }

    [JsonProperty("baseURL")]
    public string baseURL { get; private set; }

    [JsonProperty("leagueTableURL")]
    public string leagueTableURL { get; private set; }

    [JsonProperty("fixturesURL")]
    public string fixturesURL { get; private set; }

    [JsonProperty("leagueStatsURL")]
    public string leagueStatsURL { get; private set; }
    [JsonProperty("pastResultsURL")]
    public string pastResultsURL { get; private set; }
    [JsonProperty("transferDataURL")]
    public string transferDataURL { get; private set; }

    [JsonProperty("prefix")]
    public string Prefix { get; private set; }

    [JsonProperty("serverid")]
    public ulong ServerId { get; private set; }

    [JsonProperty("footychannelid")]
    public ulong FootyChannelId { get; private set; }

    [JsonProperty("adminusers")]
    public List<ulong> AdminUsers { get; private set; }
}