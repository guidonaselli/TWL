namespace TWL.Shared.Domain.Guilds;

public class Guild
{
    public int GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public List<int> MemberIds { get; set; } = new();
    public Dictionary<int, GuildRank> MemberRanks { get; set; } = new();
    public Dictionary<int, DateTimeOffset> MemberJoinDates { get; set; } = new();
    public Dictionary<int, int> StorageItems { get; set; } = new();
}
