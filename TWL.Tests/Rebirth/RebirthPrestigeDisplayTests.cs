using System;
using System.Text.Json;
using Xunit;
using TWL.Shared.Net.Payloads;
using TWL.Shared.Domain.DTO;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.Characters;

namespace TWL.Tests.Rebirth;

public class RebirthPrestigeDisplayTests
{
    [Fact]
    public void LoginResponseDto_SerializesRebirthLevel()
    {
        // Arrange
        var dto = new LoginResponseDto
        {
            Success = true,
            UserId = 123,
            RebirthLevel = 5
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<LoginResponseDto>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(5, deserialized.RebirthLevel);
    }

    [Fact]
    public void PlayerCharacterData_MapsRebirthLevelFromDTO()
    {
        // Arrange
        var dto = new PlayerDataDTO
        {
            PlayerId = 1,
            UserName = "Hero",
            RebirthLevel = 2
        };

        // Act
        var data = PlayerCharacterData.FromDTO(dto);

        // Assert
        Assert.Equal(2, data.RebirthLevel);
    }

    [Fact]
    public void PlayerCharacterData_MapsRebirthLevelFromLoginResponse()
    {
        // Arrange
        var response = new LoginResponseDto
        {
            UserId = 123,
            Hp = 80,
            MaxHp = 100,
            RebirthLevel = 3,
            PosX = 10f,
            PosY = 20f
        };

        // Act
        var data = PlayerCharacterData.FromLoginResponse(response, "TestUser");

        // Assert
        Assert.Equal(3, data.RebirthLevel);
        Assert.Equal(123, data.UserId);
        Assert.Equal("TestUser", data.Name);
        Assert.Equal(10f, data.PosX);
        Assert.Equal(20f, data.PosY);
    }
}
