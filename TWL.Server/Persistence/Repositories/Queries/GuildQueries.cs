namespace TWL.Server.Persistence.Repositories.Queries;

public static class GuildQueries
{
    public const string SelectAll = @"
        SELECT
            guild_id as GuildId,
            name as Name,
            leader_id as LeaderId,
            member_ids_json as MemberIdsJson,
            member_ranks_json as MemberRanksJson,
            member_join_dates_json as MemberJoinDatesJson,
            storage_items_json as StorageItemsJson,
            last_saved as LastSaved,
            xmin as Version
        FROM guilds
    ";

    public const string SelectById = SelectAll + " WHERE guild_id = @GuildId";

    public const string SelectByName = SelectAll + " WHERE name = @Name";
}
