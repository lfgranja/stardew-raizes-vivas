using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Creates real <see cref="GameLocation"/> instances for unit tests.
/// </summary>
/// <remarks>
/// <para>
/// The map-backed <c>GameLocation(string mapPath, string name)</c> constructor cannot be used in a
/// unit-test host: it calls <c>reloadMap</c>, which resolves the map loader through the
/// <c>Game1</c> singleton and throws <see cref="NullReferenceException"/>. The parameterless
/// constructor does no loading and yields a location with an initialised <c>terrainFeatures</c>
/// and <c>TemporarySprites</c> dictionary.
/// </para>
/// <para>
/// <c>GameLocation.Name</c> and <c>GameLocation.IsFarm</c> expose no public setter in 1.6, but the
/// backing <c>NetString</c> / <c>NetBool</c> fields are public and their <c>Value</c> property is
/// publicly settable, so both are populated that way without reflection into private state.
/// </para>
/// </remarks>
public class GameLocationFixture
{
    /// <summary>The game location under construction.</summary>
    public GameLocation Location { get; }

    /// <summary>Coordinates of every tilled tile added through this fixture.</summary>
    public List<Vector2> TilledTiles { get; } = new();

    /// <summary>
    /// Creates a location with the given name, flagged as a farm when <paramref name="isFarm"/> is set.
    /// </summary>
    /// <param name="name">Location name, matching the key services use to look it up.</param>
    /// <param name="isFarm">Whether <see cref="GameLocation.IsFarm"/> should report <c>true</c>.</param>
    public GameLocationFixture(string name = "Farm", bool isFarm = true)
    {
        GameStateFixture.Install();

        Location = new GameLocation();
#pragma warning disable AvoidNetField
        // GameLocation.Name and GameLocation.IsFarm expose no public setter in 1.6, so these
        // public NetField backing values are the only way to populate them from a test.
        Location.name.Value = name;
        Location.isFarm.Value = isFarm;
#pragma warning restore AvoidNetField
    }

    /// <summary>
    /// Registers a tilled tile at the given coordinates.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="GameStateFixture.Install"/> to have run, which this constructor does.
    /// </remarks>
    /// <param name="x">X tile coordinate.</param>
    /// <param name="y">Y tile coordinate.</param>
    /// <param name="hasCrop">When <c>true</c> the tile is covered by a crop.</param>
    public void AddHoeDirtTile(int x, int y, bool hasCrop = false)
    {
        var tile = new Vector2(x, y);
        var hoeDirt = new HoeDirt();

        if (hasCrop)
        {
            hoeDirt.crop = new StardewValley.Crop();
        }

        Location.terrainFeatures.Add(tile, hoeDirt);
        TilledTiles.Add(tile);
    }
}
