using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Helper class to create real GameLocation instances with HoeDirt tiles.
/// Uses correct constructor signature: GameLocation(string mapPath, string name).
/// </summary>
public class GameLocationFixture
{
    /// <summary>The game location with tilled tiles.</summary>
    public GameLocation Location { get; }

    /// <summary>List of all tilled tile coordinates for cleanup/reference.</summary>
    public List<Vector2> TilledTiles { get; } = new();

    /// <summary>Creates a new game location with the specified map path and name.</summary>
    public GameLocationFixture(string mapPath = "Maps\\Farm", string name = "Farm")
    {
        Location = new GameLocation(mapPath, name);
    }

    /// <summary>Adds a HoeDirt tile at the specified coordinates.</summary>
    /// <param name="x">X tile coordinate.</param>
    /// <param name="y">Y tile coordinate.</param>
    /// <param name="bare">If true, tile has no crop (bare). If false, tile has a crop.</param>
    public void AddHoeDirtTile(int x, int y, bool bare = true)
    {
        var tile = new Vector2(x, y);
        var hoeDirt = new HoeDirt(0, Location);
        if (!bare)
        {
            hoeDirt.crop = new Crop("0", x, y, Location);
        }
        Location.terrainFeatures.Add(tile, hoeDirt);
        TilledTiles.Add(tile);
    }
}
