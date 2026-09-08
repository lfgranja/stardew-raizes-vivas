using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Tests.Fixtures
{
    public static class GameLocationFixture
    {
        public static GameLocation CreateLocation(string mapPath = "Maps\\Farm", string name = "Farm")
        {
            return new GameLocation(mapPath, name);
        }

        public static void AddHoeDirtTile(GameLocation location, Vector2 tile, int seed = 0)
        {
            var hoeDirt = new HoeDirt(seed, location);
            location.terrainFeatures.Add(tile, hoeDirt);
        }
    }
}
