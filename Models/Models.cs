public enum LeagueOptions
{
    ENG,
    ESP,
    GER,
    ITA
}
[Serializable]
public class SubscriptionDetails
{
    private Team team;
    public Team Team => team;
    public SubscriptionDetails(Team team)
    {
        this.team = team;
    }
}
[Serializable]
public class Team
{
    public string teamId;
    public string teamName;

    public Team(string teamName, string teamId)
    {
        this.teamName = teamName;
        this.teamId = teamId;
    }
}