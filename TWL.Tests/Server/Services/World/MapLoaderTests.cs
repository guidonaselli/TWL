using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Services.World;

namespace TWL.Tests.Server.Services.World;

public class MapLoaderTests : IDisposable
{
    private readonly MapLoader _loader;
    private readonly string _tempFile;

    public MapLoaderTests()
    {
        _tempFile = Path.GetTempFileName();
        _loader = new MapLoader(NullLogger<MapLoader>.Instance);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void LoadMap_ValidTmx_ParsesCorrectly()
    {
        var tmxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.0"" orientation=""orthogonal"" width=""10"" height=""10"" tilewidth=""32"" tileheight=""32"">
 <objectgroup name=""Triggers"">
  <object id=""1"" x=""32"" y=""32"" width=""32"" height=""32"">
   <properties>
    <property name=""TriggerType"" value=""MapTransition""/>
    <property name=""Id"" value=""Transition1""/>
    <property name=""TargetMapId"" value=""2""/>
    <property name=""TargetSpawnId"" value=""Spawn1""/>
   </properties>
  </object>
 </objectgroup>
 <objectgroup name=""Spawns"">
  <object id=""2"" x=""64"" y=""64"">
   <properties>
    <property name=""SpawnType"" value=""PlayerStart""/>
    <property name=""Id"" value=""Start1""/>
   </properties>
  </object>
 </objectgroup>
</map>";
        File.WriteAllText(_tempFile, tmxContent);

        var map = _loader.LoadMap(_tempFile);

        Assert.NotNull(map);
        Assert.Equal(10, map.Width);
        Assert.Equal(32, map.TileWidth);

        Assert.Single(map.Triggers);
        var trigger = map.Triggers[0];
        Assert.Equal("Transition1", trigger.Id); // Logical Id
        Assert.Equal("MapTransition", trigger.Type);
        Assert.Equal(32, trigger.X);

        Assert.Single(map.Spawns);
        var spawn = map.Spawns[0];
        Assert.Equal("Start1", spawn.Id);
        Assert.Equal("PlayerStart", spawn.Type);
        Assert.Equal(64, spawn.X);
    }
}