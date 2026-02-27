using System.Collections.Generic;
using TWL.Shared.Domain.DTO;

namespace TWL.Shared.Domain.State;

public class PartyState
{
    public int? PartyId { get; set; }
    public int? LeaderId { get; set; }
    public List<PartyMemberDto> Members { get; set; } = new();
    public List<PartyChatMessage> ChatLog { get; set; } = new();

    public void Update(PartyUpdateBroadcast update)
    {
        if (update.PartyId == 0)
        {
            PartyId = null;
            LeaderId = null;
            Members.Clear();
            ChatLog.Clear();
        }
        else
        {
            PartyId = update.PartyId;
            LeaderId = update.LeaderId;
            Members = update.Members;
        }
    }

    public void AddMessage(PartyChatMessage message)
    {
        ChatLog.Add(message);
        if (ChatLog.Count > 50)
        {
            ChatLog.RemoveAt(0);
        }
    }
}
