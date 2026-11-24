using System;
using System.IO;
using LivingRoots.Domain;
using LivingRoots.Tests.Verification;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class SaveComparatorTests
    {
        private static string WriteSave(string dir, string name, SoilHealthState state)
        {
            var path = Path.Combine(dir, name);
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            File.WriteAllText(path, json);
            return path;
        }

        [Fact]
        public void Compare_DetectsAddedChangedRemoved()
        {
            var dir = Path.Combine(Path.GetTempPath(), "sc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var before = new SoilHealthState();
                before.LocationHealthData["Farm"] = new Dictionary<string, float>
                {
                    { "1,1", 10f },
                    { "2,2", 20f },
                };
                var after = new SoilHealthState();
                after.LocationHealthData["Farm"] = new Dictionary<string, float>
                {
                    { "1,1", 10.00005f }, // within tolerance -> not changed
                    { "2,2", 25f },        // changed
                    { "3,3", 5f },         // added
                };

                var beforePath = WriteSave(dir, "before.json", before);
                var afterPath = WriteSave(dir, "after.json", after);

                var result = SaveComparator.Compare(beforePath, afterPath);

                Assert.Single(result.Changed);
                Assert.Equal("2,2", result.Changed[0].Tile);
                Assert.Single(result.Added);
                Assert.Equal("3,3", result.Added[0].Tile);
                Assert.Empty(result.Removed);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Compare_MissingKeysTreatedAsZero_Removed()
        {
            var dir = Path.Combine(Path.GetTempPath(), "sc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var before = new SoilHealthState();
                before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", 7f } };
                var after = new SoilHealthState(); // missing key -> 0

                var beforePath = WriteSave(dir, "before.json", before);
                var afterPath = WriteSave(dir, "after.json", after);

                var result = SaveComparator.Compare(beforePath, afterPath);

                Assert.Single(result.Removed);
                Assert.Equal("1,1", result.Removed[0].Tile);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Compare_NaN_TilesIgnored()
        {
            var dir = Path.Combine(Path.GetTempPath(), "sc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var before = new SoilHealthState();
                before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", float.NaN } };
                var after = new SoilHealthState();
                after.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", 5f } };

                var result = SaveComparator.Compare(before, after);

                Assert.Empty(result.Added);
                Assert.Empty(result.Changed);
                Assert.Empty(result.Removed);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
