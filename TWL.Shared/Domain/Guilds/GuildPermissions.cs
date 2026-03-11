using System;

namespace TWL.Shared.Domain.Guilds;

[Flags]
public enum GuildPermissions
{
    None = 0,
    Invite = 1 << 0,
    Kick = 1 << 1,
    Promote = 1 << 2,
    Demote = 1 << 3,
    WithdrawStorage = 1 << 4,
    
    // Derived shorthand
    All = Invite | Kick | Promote | Demote | WithdrawStorage
}
