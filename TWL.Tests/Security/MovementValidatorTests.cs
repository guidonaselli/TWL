using System;
using TWL.Server.Security;
using TWL.Shared.Domain.DTO;
using Xunit;

namespace TWL.Tests.Security;

public class MovementValidatorTests
{
    private readonly MovementValidationOptions _options;
    private readonly MovementValidator _validator;

    public MovementValidatorTests()
    {
        _options = new MovementValidationOptions
        {
            MaxDistancePerTick = 1.5f,
            MaxAxisDeltaPerTick = 1.0f,
            AllowDiagonalBoost = false,
            MaxAbsoluteCoordinate = 10000f
        };
        _validator = new MovementValidator(_options);
    }

    [Fact]
    public void Validate_ValidMove_ReturnsTrue()
    {
        var move = new MoveDTO { dx = 1.0f, dy = 0.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = _validator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.True(result);
        Assert.Empty(reason);
    }

    [Fact]
    public void Validate_MalformedPayload_NaN_ReturnsFalse()
    {
        var move = new MoveDTO { dx = float.NaN, dy = 0.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = _validator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.False(result);
        Assert.Equal("MalformedPayload:NaNOrInfinity", reason);
    }

    [Fact]
    public void Validate_MalformedPayload_Infinity_ReturnsFalse()
    {
        var move = new MoveDTO { dx = 0.0f, dy = float.PositiveInfinity };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = _validator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.False(result);
        Assert.Equal("MalformedPayload:NaNOrInfinity", reason);
    }

    [Fact]
    public void Validate_ExceedsAxisDelta_ReturnsFalse()
    {
        var move = new MoveDTO { dx = 1.1f, dy = 0.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = _validator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.False(result);
        Assert.StartsWith("SpeedHack:AxisSpeedLimitExceeded", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ExceedsMaxDistance_WithoutDiagonalBoost_ReturnsFalse()
    {
        // 1.0 and 1.0 gives a distance of ~1.414.
        // If MaxDistancePerTick is 1.2, this should fail even if axes are within limit.
        var localOptions = new MovementValidationOptions
        {
            MaxDistancePerTick = 1.2f, // Less than sqrt(2)
            MaxAxisDeltaPerTick = 1.0f,
            AllowDiagonalBoost = false
        };
        var localValidator = new MovementValidator(localOptions);

        var move = new MoveDTO { dx = 1.0f, dy = 1.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = localValidator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.False(result);
        Assert.StartsWith("SpeedHack:EuclideanSpeedLimitExceeded", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ExceedsMaxDistance_WithDiagonalBoost_ReturnsTrue()
    {
        // 1.0 and 1.0 gives a distance of ~1.414.
        // With AllowDiagonalBoost, limit becomes MaxDistancePerTick * sqrt(2).
        var localOptions = new MovementValidationOptions
        {
            MaxDistancePerTick = 1.2f,
            MaxAxisDeltaPerTick = 1.0f,
            AllowDiagonalBoost = true // Limit is now 1.2 * 1.414 = 1.69
        };
        var localValidator = new MovementValidator(localOptions);

        var move = new MoveDTO { dx = 1.0f, dy = 1.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        var result = localValidator.Validate(0f, 0f, move, deltaTime, out string reason);

        Assert.True(result);
        Assert.Empty(reason);
    }

    [Fact]
    public void Validate_ExceedsAbsoluteCoordinates_ReturnsFalse()
    {
        var move = new MoveDTO { dx = 1.0f, dy = 0.0f };
        var deltaTime = TimeSpan.FromMilliseconds(500);

        // Current position + dx = 10000.5 > 10000.0
        var result = _validator.Validate(9999.5f, 0f, move, deltaTime, out string reason);

        Assert.False(result);
        Assert.StartsWith("OutOfBounds:MaxCoordinateExceeded", reason, StringComparison.OrdinalIgnoreCase);
    }
}
