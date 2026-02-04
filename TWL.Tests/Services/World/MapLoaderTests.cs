using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Domain.World;
using TWL.Server.Services.World;
using Xunit;

namespace TWL.Tests.Services.World;

public class MapLoaderTests
{
    private readonly MapLoader _mapLoader;
    private readonly Mock<ILogger<MapLoader>> _loggerMock;

    public MapLoaderTests()
    {
        _loggerMock = new Mock<ILogger<MapLoader>>();
        _mapLoader = new MapLoader(_loggerMock.Object);
    }

    [Fact]
    public void LoadMap_ShouldParseTimerTriggerProperties()
    {
        // Arrange
        var tmxXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.10"" tiledversion=""1.10.2"" orientation=""orthogonal"" renderorder=""right-down"" width=""10"" height=""10"" tilewidth=""32"" tileheight=""32"" infinite=""0"" nextlayerid=""3"" nextobjectid=""2"">
 <objectgroup id=""2"" name=""Triggers"">
  <object id=""1"" name=""TestTrigger"" x=""32"" y=""32"" width=""32"" height=""32"">
   <properties>
    <property name=""TriggerType"" value=""DamageRegion""/>
    <property name=""Activation"" value=""Timer""/>
    <property name=""Interval"" value=""5000""/>
    <property name=""Cooldown"" value=""1000""/>
    <property name=""Id"" value=""trigger_1""/>
   </properties>
  </object>
 </objectgroup>
</map>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, tmxXml, Encoding.UTF8);

        try
        {
            // Act
            var map = _mapLoader.LoadMap(tempFile);

            // Assert
            Assert.NotNull(map);
            Assert.Single(map.Triggers);
            var trigger = map.Triggers[0];

            Assert.Equal("trigger_1", trigger.Id);
            Assert.Equal("DamageRegion", trigger.Type);
            Assert.Equal(TriggerActivationType.Timer, trigger.ActivationType);
            Assert.Equal(5000, trigger.IntervalMs);
            Assert.Equal(1000, trigger.CooldownMs);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadMap_ShouldDefaultToEnterActivation()
    {
        // Arrange
        var tmxXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.10"" tiledversion=""1.10.2"" orientation=""orthogonal"" renderorder=""right-down"" width=""10"" height=""10"" tilewidth=""32"" tileheight=""32"">
 <objectgroup name=""Triggers"">
  <object id=""1"" x=""0"" y=""0"" width=""32"" height=""32"">
   <properties>
    <property name=""TriggerType"" value=""Simple""/>
   </properties>
  </object>
 </objectgroup>
</map>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, tmxXml, Encoding.UTF8);

        try
        {
            // Act
            var map = _mapLoader.LoadMap(tempFile);

            // Assert
            var trigger = map.Triggers[0];
            Assert.Equal(TriggerActivationType.Enter, trigger.ActivationType);
            Assert.Equal(0, trigger.IntervalMs);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
