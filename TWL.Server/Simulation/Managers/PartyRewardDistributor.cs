using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public class PartyRewardDistributor
{
    private readonly IPartyService _partyService;
    private readonly PlayerService _playerService;
    private readonly MonsterManager _monsterManager;
    private readonly IRandomService _randomService;

    // Config: Maximum distance to share XP
    private const float MaxShareDistance = 30.0f;

    public PartyRewardDistributor(
        IPartyService partyService,
        PlayerService playerService,
        MonsterManager monsterManager,
        IRandomService randomService)
    {
        _partyService = partyService;
        _playerService = playerService;
        _monsterManager = monsterManager;
        _randomService = randomService;
    }

    public void DistributeKillRewards(ServerCombatant killer, ServerCombatant victim)
    {
        // 1. Identify Victim (Must be a Monster)
        if (victim is not ServerCharacter victimChar || victimChar.MonsterId <= 0)
        {
            return;
        }

        var monsterDef = _monsterManager.GetDefinition(victimChar.MonsterId);
        if (monsterDef == null)
        {
            return;
        }

        // 2. Identify Beneficiary (Player)
        ServerCharacter? beneficiary = null;

        if (killer is ServerCharacter playerKiller && playerKiller.MonsterId == 0)
        {
            beneficiary = playerKiller;
        }
        else if (killer is ServerPet petKiller && petKiller.OwnerId > 0)
        {
            var ownerSession = _playerService.GetSession(petKiller.OwnerId);
            beneficiary = ownerSession?.Character;
        }

        if (beneficiary == null)
        {
            return; // No player to reward
        }

        // 3. Determine Group (Party or Solo)
        var party = _partyService.GetPartyByMember(beneficiary.Id);
        List<ServerCharacter> eligibleMembers = new();

        if (party != null)
        {
            List<int> memberIdsSnapshot;
            lock (party)
            {
                memberIdsSnapshot = new List<int>(party.MemberIds);
            }

            // Filter eligible members
            foreach (var memberId in memberIdsSnapshot)
            {
                var memberSession = _playerService.GetSession(memberId);
                var memberChar = memberSession?.Character;

                if (memberChar != null &&
                    memberChar.MapId == victimChar.MapId &&
                    IsWithinRange(memberChar, victimChar, MaxShareDistance))
                {
                    eligibleMembers.Add(memberChar);
                }
            }
        }

        // Fallback: If no eligible members found (e.g. solo or everyone too far), reward the beneficiary
        if (eligibleMembers.Count == 0)
        {
            eligibleMembers.Add(beneficiary);
        }

        // 4. Distribute XP
        int totalXp = monsterDef.ExpReward;
        if (totalXp > 0)
        {
            int xpPerMember = totalXp / eligibleMembers.Count;
            // Ensure at least 1 XP if total > 0
            if (xpPerMember == 0 && totalXp > 0) xpPerMember = 1;

            foreach (var member in eligibleMembers)
            {
                member.AddExp(xpPerMember);
            }
        }

        // 5. Distribute Loot
        if (monsterDef.Drops != null && monsterDef.Drops.Count > 0)
        {
            foreach (var drop in monsterDef.Drops)
            {
                if (_randomService.NextDouble() <= drop.Chance)
                {
                    int quantity = _randomService.Next(drop.MinQuantity, drop.MaxQuantity + 1);
                    if (quantity <= 0) continue;

                    // Determine Recipient
                    ServerCharacter recipient;
                    if (party != null && eligibleMembers.Count > 1)
                    {
                        // Round Robin
                        recipient = GetNextLootRecipient(party, eligibleMembers);
                    }
                    else
                    {
                        // Solo or only 1 eligible
                        recipient = eligibleMembers[0];
                    }

                    recipient.AddItem(drop.ItemId, quantity);
                }
            }
        }
    }

    private ServerCharacter GetNextLootRecipient(Party party, List<ServerCharacter> eligibleMembers)
    {
        lock (party)
        {
            if (party.MemberIds.Count == 0) return eligibleMembers[0];

            // Try to find the next member in the rotation who is also eligible
            for (int i = 0; i < party.MemberIds.Count; i++)
            {
                int currentPointer = (party.NextLootMemberIndex + i) % party.MemberIds.Count;
                int candidateId = party.MemberIds[currentPointer];

                var candidate = eligibleMembers.FirstOrDefault(m => m.Id == candidateId);
                if (candidate != null)
                {
                    // Found a valid recipient.
                    // Advance index for next time to the one *after* this one (currentPointer + 1)
                    // This ensures rotation continues from where we left off.
                    party.NextLootMemberIndex = (currentPointer + 1) % party.MemberIds.Count;
                    return candidate;
                }
            }

            // If we looped all and found no one eligible (shouldn't happen as eligibleMembers.Count > 0)
            // Fallback to first eligible
            // Still advance index to keep it moving
            party.NextLootMemberIndex = (party.NextLootMemberIndex + 1) % party.MemberIds.Count;
            return eligibleMembers[0];
        }
    }

    private bool IsWithinRange(ServerCharacter a, ServerCharacter b, float threshold)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return (dx * dx + dy * dy) <= (threshold * threshold);
    }
}
