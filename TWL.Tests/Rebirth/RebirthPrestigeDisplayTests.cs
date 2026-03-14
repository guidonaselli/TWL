using System.Text.Json;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Net.Payloads;
using Xunit;

namespace TWL.Tests.Rebirth;

public class RebirthPrestigeDisplayTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void LoginResponseDto_SerializesRebirthLevel_Correctly()
    {
        // Arrange
        var dto = new LoginResponseDto
        {
            Success = true,
            UserId = 1,
            RebirthLevel = 3,
            Hp = 100,
            MaxHp = 100
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<LoginResponseDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.RebirthLevel);
    }

    [Fact]
    public void PlayerDataDTO_SerializesRebirthLevel_Correctly()
    {
        // Arrange
        var dto = new PlayerDataDTO
        {
            PlayerId = 1,
            UserName = "Test",
            RebirthLevel = 2,
            Hp = 100,
            MaxHp = 100
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<PlayerDataDTO>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.RebirthLevel);
    }
}