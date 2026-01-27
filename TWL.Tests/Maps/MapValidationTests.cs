using System;
using System.IO;
using Xunit;

namespace TWL.Tests.Maps
{
    public class MapValidationTests
    {
        private string GetContentMapsPath()
        {
            // Try to find Content/Maps relative to the test execution directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var directory = new DirectoryInfo(baseDir);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "TheWonderlandSolution.sln")))
            {
                directory = directory.Parent;
            }

            if (directory != null)
            {
                var mapsPath = Path.Combine(directory.FullName, "Content", "Maps");
                if (Directory.Exists(mapsPath))
                {
                    return mapsPath;
                }
            }

            if (Directory.Exists("Content/Maps")) return "Content/Maps";

            return null;
        }

        private string CreateTempMap(string tmxContent)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "TWL_Test_Map_" + Guid.NewGuid());
            Directory.CreateDirectory(tempPath);
            File.WriteAllText(Path.Combine(tempPath, "test.tmx"), tmxContent);
            File.WriteAllText(Path.Combine(tempPath, "test.meta.json"),
                @"{""MapId"":1, ""RegionId"":""Test"", ""EntryPoints"":[], ""Exits"":[]}");
            return tempPath;
        }

        private void CleanupTempMap(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        [Fact]
        public void ValidateAllExistingMaps()
        {
            var mapsPath = GetContentMapsPath();
            if (string.IsNullOrEmpty(mapsPath) || !Directory.Exists(mapsPath))
            {
                return;
            }

            var regionDirs = Directory.GetDirectories(mapsPath);
            foreach (var regionDir in regionDirs)
            {
                var mapDirs = Directory.GetDirectories(regionDir);
                foreach (var mapDir in mapDirs)
                {
                    try
                    {
                        MapValidator.ValidateMap(mapDir);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Validation failed for map in {mapDir}: {ex.Message}");
                    }
                }
            }
        }

        [Fact]
        public void Validator_Fails_On_Missing_Layer()
        {
            var tmx = @"<?xml version=""1.0""?>
<map><layer name=""Ground""></layer></map>"; // Missing other layers

            var path = CreateTempMap(tmx);
            try
            {
                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(path));
                Assert.Contains("Missing required layer", ex.Message);
            }
            finally { CleanupTempMap(path); }
        }

        [Fact]
        public void Validator_Fails_On_Invalid_CollisionType()
        {
            var tmx = GetBaseTmxWithLayers() + @"
 <objectgroup name=""Collisions"">
  <object id=""1"">
   <properties><property name=""CollisionType"" value=""Invalid""/></properties>
  </object>
 </objectgroup>
 <objectgroup name=""Spawns""></objectgroup>
 <objectgroup name=""Triggers""></objectgroup>
</map>";
            var path = CreateTempMap(tmx);
            try
            {
                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(path));
                Assert.Contains("invalid 'CollisionType'", ex.Message);
            }
            finally { CleanupTempMap(path); }
        }

        [Fact]
        public void Validator_Fails_On_Missing_CollisionType()
        {
            var tmx = GetBaseTmxWithLayers() + @"
 <objectgroup name=""Collisions"">
  <object id=""1"">
   <!-- Missing property -->
  </object>
 </objectgroup>
 <objectgroup name=""Spawns""></objectgroup>
 <objectgroup name=""Triggers""></objectgroup>
</map>";
            var path = CreateTempMap(tmx);
            try
            {
                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(path));
                Assert.Contains("missing 'CollisionType'", ex.Message);
            }
            finally { CleanupTempMap(path); }
        }

        [Fact]
        public void Validator_Fails_On_Duplicate_TriggerId()
        {
            var tmx = GetBaseTmxWithLayers() + @"
 <objectgroup name=""Collisions""></objectgroup>
 <objectgroup name=""Spawns""></objectgroup>
 <objectgroup name=""Triggers"">
  <object id=""1"">
   <properties>
    <property name=""TriggerType"" value=""Interaction""/>
    <property name=""Id"" value=""100""/>
   </properties>
  </object>
  <object id=""2"">
   <properties>
    <property name=""TriggerType"" value=""Interaction""/>
    <property name=""Id"" value=""100""/> <!-- Duplicate -->
   </properties>
  </object>
 </objectgroup>
</map>";
            var path = CreateTempMap(tmx);
            try
            {
                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(path));
                Assert.Contains("Duplicate Trigger ID", ex.Message);
            }
            finally { CleanupTempMap(path); }
        }

        [Fact]
        public void Validator_Fails_On_Missing_TargetMapId_For_Transition()
        {
            var tmx = GetBaseTmxWithLayers() + @"
 <objectgroup name=""Collisions""></objectgroup>
 <objectgroup name=""Spawns""></objectgroup>
 <objectgroup name=""Triggers"">
  <object id=""1"">
   <properties>
    <property name=""TriggerType"" value=""MapTransition""/>
    <property name=""Id"" value=""1""/>
    <!-- Missing TargetMapId -->
   </properties>
  </object>
 </objectgroup>
</map>";
            var path = CreateTempMap(tmx);
            try
            {
                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(path));
                Assert.Contains("missing required property 'TargetMapId'", ex.Message);
            }
            finally { CleanupTempMap(path); }
        }

        private string GetBaseTmxWithLayers()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.0"" tiledversion=""1.0"" orientation=""orthogonal"" renderorder=""right-down"" width=""10"" height=""10"" tilewidth=""32"" tileheight=""32"">
 <layer name=""Ground"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Ground_Detail"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Water"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Cliffs"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Rocks"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Props_Low"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <layer name=""Props_High"" width=""10"" height=""10""><data encoding=""csv""></data></layer>";
        }
    }
}
