using System;
using System.Collections.Generic;

namespace TWL.Shared.Domain.DTO;

public class GuildRosterSyncEvent
{
    public List<GuildMemberDto> Members { get; set; } = new();
}

public class GuildRosterUpdateEvent
{
    public GuildMemberDto Member { get; set; } = new();
    public bool IsRemoved { get; set; }
}
