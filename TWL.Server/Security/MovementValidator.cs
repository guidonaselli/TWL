using System;
using TWL.Shared.Domain.DTO;

namespace TWL.Server.Security;

public class MovementValidator
{
    private readonly MovementValidationOptions _options;

    public MovementValidator(MovementValidationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Validates a movement attempt from the client.
    /// </summary>
    public bool Validate(float currentX, float currentY, MoveDTO move, TimeSpan deltaTime, float moveSpeedModifier, out string reason)
    {
        reason = string.Empty;

        // 1. Check for missing or malformed payload values (NaN / Infinity)
        if (float.IsNaN(move.dx) || float.IsInfinity(move.dx) ||
            float.IsNaN(move.dy) || float.IsInfinity(move.dy))
        {
            reason = "MalformedPayload:NaNOrInfinity";
            return false;
        }

        // 2. Check maximum axis delta limits (scaled by modifier)
        var maxAxisDelta = _options.MaxAxisDeltaPerTick * moveSpeedModifier;
        if (Math.Abs(move.dx) > maxAxisDelta || Math.Abs(move.dy) > maxAxisDelta)
        {
            reason = $"SpeedHack:AxisSpeedLimitExceeded:dx={move.dx:F2},dy={move.dy:F2},max={maxAxisDelta:F2}";
            return false;
        }

        // 3. Euclidean distance checks (scaled by modifier)
        var distance = (float)Math.Sqrt(move.dx * move.dx + move.dy * move.dy);
        
        var allowedDistance = _options.MaxDistancePerTick * moveSpeedModifier;
        if (_options.AllowDiagonalBoost)
        {
            // If diagonal boost is allowed, limit is effectively sqrt(max_x^2 + max_y^2)
            allowedDistance = (float)Math.Sqrt(2 * maxAxisDelta * maxAxisDelta);
        }

        // Compare using a small tolerance for floating point errors (e.g. 0.001) if necessary, 
        // but distance > allowedDistance is enough.
        if (distance > allowedDistance + 0.001f)
        {
            reason = $"SpeedHack:EuclideanSpeedLimitExceeded:dist={distance:F2},max={allowedDistance:F2}";
            return false;
        }

        // 4. Check absolute coordinates boundaries (destination)
        var nextX = currentX + move.dx;
        var nextY = currentY + move.dy;
        
        if (Math.Abs(nextX) > _options.MaxAbsoluteCoordinate || Math.Abs(nextY) > _options.MaxAbsoluteCoordinate)
        {
            reason = $"OutOfBounds:MaxCoordinateExceeded:x={nextX:F2},y={nextY:F2},max={_options.MaxAbsoluteCoordinate:F2}";
            return false;
        }

        return true;
    }
}
