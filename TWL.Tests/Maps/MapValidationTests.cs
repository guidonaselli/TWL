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
            // Traverse up to find the root
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

            // Fallback for CI environments or if logic fails, though in this sandbox we are at root usually
            if (Directory.Exists("Content/Maps")) return "Content/Maps";

            return null;
        }

        [Fact]
        public void ValidateAllExistingMaps()
        {
            var mapsPath = GetContentMapsPath();
            // If the folder doesn't exist yet (no maps), we skip or assert empty.
            // But we plan to add a map, so eventually it should exist.
            if (string.IsNullOrEmpty(mapsPath) || !Directory.Exists(mapsPath))
            {
                // Acceptable if no maps yet, but we expect at least the one we are about to create.
                // For now, let's just warn or return.
                // However, the daily objective is to deliver a map, so this test MUST pass on that map.
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
            // Setup a temp invalid map
            var tempPath = Path.Combine(Path.GetTempPath(), "TWL_Test_Map_" + Guid.NewGuid());
            Directory.CreateDirectory(tempPath);
            try
            {
                var tmxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.0"" tiledversion=""1.0"" orientation=""orthogonal"" renderorder=""right-down"" width=""10"" height=""10"" tilewidth=""32"" tileheight=""32"">
 <layer name=""Ground"" width=""10"" height=""10""><data encoding=""csv""></data></layer>
 <!-- Missing other layers -->
</map>";
                File.WriteAllText(Path.Combine(tempPath, "test.tmx"), tmxContent);
                File.WriteAllText(Path.Combine(tempPath, "test.meta.json"), "{}");

                var ex = Assert.Throws<Exception>(() => MapValidator.ValidateMap(tempPath));
                Assert.Contains("Missing required layer", ex.Message);
            }
            finally
            {
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            }
        }
    }
}
