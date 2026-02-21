namespace TWL.Server.Security;

public class MovementValidationOptions
{
    /// <summary>
    /// The maximum Euclidean distance a player can move in one packet.
    /// Default is 1.5 units (assuming normal movement is around 1 unit/tick).
    /// </summary>
    public float MaxDistancePerTick { get; set; } = 1.5f;

    /// <summary>
    /// The maximum delta allowed on a single axis (X or Y) per packet.
    /// Default is 1.0 unit.
    /// </summary>
    public float MaxAxisDeltaPerTick { get; set; } = 1.0f;

    /// <summary>
    /// If true, allows diagonal distance up to sqrt(2) * MaxAxisDeltaPerTick.
    /// If false, strictly limits distance to MaxDistancePerTick.
    /// </summary>
    public bool AllowDiagonalBoost { get; set; } = false;

    /// <summary>
    /// The maximum absolute X and Y bounds of the world to prevent out-of-bounds exploits.
    /// </summary>
    public float MaxAbsoluteCoordinate { get; set; } = 1000000f;
}
