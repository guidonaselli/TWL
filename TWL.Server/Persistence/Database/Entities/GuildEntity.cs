namespace TWL.Server.Persistence.Database.Entities;

public class GuildEntity
{
    public int GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderId { get; set; }

    // JSONB columns mapping to collections
    public string MemberIdsJson { get; set; } = "[]";
    public string MemberRanksJson { get; set; } = "{}";
    public string MemberJoinDatesJson { get; set; } = "{}";
    public string StorageItemsJson { get; set; } = "{}";

    public DateTime LastSaved { get; set; }
    public uint Version { get; set; } // xmin for optimistic concurrency
}
