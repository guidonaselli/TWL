using TWL.Shared.Domain.Guilds;

namespace TWL.Server.Simulation.Managers;

public class GuildPermissionService
{
    public virtual GuildPermissions GetPermissionsForRank(GuildRank rank)
    {
        return rank switch
        {
            GuildRank.Leader => GuildPermissions.All,
            GuildRank.Officer => GuildPermissions.Invite | GuildPermissions.Kick | GuildPermissions.Promote | GuildPermissions.Demote | GuildPermissions.WithdrawStorage,
            GuildRank.Member => GuildPermissions.None,
            GuildRank.Recruit => GuildPermissions.None,
            _ => GuildPermissions.None
        };
    }

    public virtual bool HasPermission(GuildRank rank, GuildPermissions requiredPermission)
    {
        var permissions = GetPermissionsForRank(rank);
        return (permissions & requiredPermission) == requiredPermission;
    }

    public virtual bool CanPromoteDemote(GuildRank actorRank, GuildRank targetCurrentRank, GuildRank newRank)
    {
        // Must have Promote/Demote permission entirely
        bool isPromotion = newRank > targetCurrentRank;
        
        if (isPromotion && !HasPermission(actorRank, GuildPermissions.Promote))
            return false;
            
        if (!isPromotion && !HasPermission(actorRank, GuildPermissions.Demote))
            return false;

        // Cannot promote or demote someone of equal or higher rank
        if (targetCurrentRank >= actorRank)
            return false;

        // Cannot promote someone to a rank equal or higher than actor's own rank
        if (newRank >= actorRank)
            return false;

        return true;
    }

    public virtual bool CanKick(GuildRank actorRank, GuildRank targetRank)
    {
        if (!HasPermission(actorRank, GuildPermissions.Kick))
            return false;

        // Can only kick members strictly below own rank
        return targetRank < actorRank;
    }
}
