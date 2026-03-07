using System.Collections.Generic;

namespace TWL.Shared.Domain.Party;

public struct GridPosition
{
    public int X { get; set; } // Row (Front = 0, Mid = 1, Back = 2)
    public int Y { get; set; } // Column (0 to 3)

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class TacticalFormation
{
    // Dictionary mapping CharacterId (or PetId) to their specific position.
    public Dictionary<int, GridPosition> MemberPositions { get; set; } = new();

    public TacticalFormation Clone()
    {
        var copy = new TacticalFormation();
        foreach (var kvp in MemberPositions)
        {
            copy.MemberPositions[kvp.Key] = new GridPosition(kvp.Value.X, kvp.Value.Y);
        }
        return copy;
    }
}
