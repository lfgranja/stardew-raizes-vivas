using System.Collections.Generic;
using System.Linq;
using FsCheck;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Shared FsCheck infrastructure for the spec 004 property-based invariant
/// suite (T000-Ra): the <see cref="Gen{T}"/> generators and
/// <see cref="Arbitrary{T}"/> instances used by T000-R1..R9 and T031a/T031b,
/// plus factory helpers enforcing the bin-state consistency rule
/// (<see cref="CompostingBinState.Empty"/> carries no input; Processing and
/// Ready always carry one). Every bound comes from <see cref="ModConstants"/>
/// — no hardcoded values.
/// </summary>
/// <remarks>
/// Call <see cref="Register"/> once per test class (constructor or module
/// initializer) so <see cref="FsCheck.Xunit.Property"/>-attributed test
/// methods can take the fixture types — (X, Y) coordinate tuples,
/// <see cref="CompostingBinStateModel"/>, <see cref="CompostingBinData"/> —
/// directly as parameters.
/// </remarks>
public static class FsCheckFixture
{
    private static readonly char[] NameChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_".ToCharArray();

    private static readonly string[] ItemIdPrefixes = { "(O)", "(BO)", "(BC)" };

    // ------------------------------------------------------------------
    // Arbitrary instances — resolved by Arb.Register<FsCheckFixture>()
    // ------------------------------------------------------------------

    /// <summary>
    /// (X, Y) tile coordinate pair, each within
    /// <see cref="ModConstants.MaxAbsoluteTileCoordinate"/>.
    /// </summary>
    public static Arbitrary<(int X, int Y)> TileCoordinate
    {
        get
        {
            return Arb.From(TileCoordinates());
        }
    }

    /// <summary>
    /// Soil health value in [<see cref="ModConstants.MinSoilHealth"/>,
    /// <see cref="ModConstants.MaxSoilHealth"/>] with centesimal granularity.
    /// </summary>
    public static Arbitrary<float> Health
    {
        get
        {
            return Arb.From(HealthValues());
        }
    }

    /// <summary>Non-empty location name within <see cref="ModConstants.MaxLocationNameLength"/>.</summary>
    public static Arbitrary<string> LocationName
    {
        get
        {
            return Arb.From(LocationNames());
        }
    }

    /// <summary>Non-empty qualified item ID, e.g. "(O)fiber123".</summary>
    public static Arbitrary<string> ItemId
    {
        get
        {
            return Arb.From(ItemIds());
        }
    }

    /// <summary>Non-negative game-day timestamp.</summary>
    public static Arbitrary<int> DayTimestamp
    {
        get
        {
            return Arb.From(DayTimestamps());
        }
    }

    /// <summary>One of the three valid <see cref="CompostingBinState"/> values.</summary>
    public static Arbitrary<CompostingBinState> State
    {
        get
        {
            return Arb.From(States());
        }
    }

    /// <summary>
    /// <see cref="CompostingBinStateModel"/> whose input fields follow the
    /// state (Empty → null input; Processing/Ready → input present).
    /// </summary>
    public static Arbitrary<CompostingBinStateModel> BinModel
    {
        get
        {
            return Arb.From(BinModels());
        }
    }

    /// <summary>
    /// <see cref="CompostingBinData"/> with 0-4 entries under one randomly
    /// named location.
    /// </summary>
    public static Arbitrary<CompostingBinData> BinData
    {
        get
        {
            return Arb.From(BinDataSets());
        }
    }

    // ------------------------------------------------------------------
    // Generators — compose with Prop.ForAll / Arb.From
    // ------------------------------------------------------------------

    /// <summary>One tile coordinate in [0, <see cref="ModConstants.MaxAbsoluteTileCoordinate"/>].</summary>
    public static Gen<int> TileCoordinateValues()
    {
        return Gen.Choose(0, ModConstants.MaxAbsoluteTileCoordinate);
    }

    /// <summary>
    /// An (X, Y) tile coordinate pair, each within
    /// <see cref="ModConstants.MaxAbsoluteTileCoordinate"/>.
    /// </summary>
    public static Gen<(int X, int Y)> TileCoordinates()
    {
        return TileCoordinateValues()
            .Two()
            .Select(t => (t.Item1, t.Item2));
    }

    /// <summary>Soil health value in [0, 100] with centesimal granularity.</summary>
    public static Gen<float> HealthValues()
    {
        return Gen.Choose(0, (int)(ModConstants.MaxSoilHealth * 100))
            .Select(h => h / 100f);
    }

    /// <summary>Non-empty location name (letters, digits, '-', '_').</summary>
    public static Gen<string> LocationNames()
    {
        return Gen.Choose(1, ModConstants.MaxLocationNameLength)
            .SelectMany(
                length => Gen.ArrayOf(length, Gen.Elements(NameChars)),
                (length, chars) => new string(chars));
    }

    /// <summary>Non-empty qualified item ID, e.g. "(O)fiber123".</summary>
    public static Gen<string> ItemIds()
    {
        return Gen.Elements(ItemIdPrefixes)
            .SelectMany(
                prefix => Gen.ArrayOf(Gen.Elements(NameChars))
                    .Where(chars => chars.Length > 0)
                    .Select(chars => prefix + new string(chars)));
    }

    /// <summary>Non-negative game-day timestamp.</summary>
    public static Gen<int> DayTimestamps()
    {
        return Gen.Choose(0, int.MaxValue);
    }

    /// <summary>One of the three valid <see cref="CompostingBinState"/> values.</summary>
    public static Gen<CompostingBinState> States()
    {
        return Gen.Elements(new[]
        {
            CompostingBinState.Empty,
            CompostingBinState.Processing,
            CompostingBinState.Ready
        });
    }

    /// <summary>
    /// A <see cref="CompostingBinStateModel"/> with consistent input fields:
    /// Empty forces null input; Processing/Ready always carry an item ID and
    /// timestamp (random timestamp, default item ID when none is given).
    /// </summary>
    public static Gen<CompostingBinStateModel> BinModels()
    {
        return States()
            .SelectMany(
                state => TileCoordinates().SelectMany(
                    coord => DayTimestamps().Select(timestamp =>
                        CreateBinWithState(state, coord.X, coord.Y, null, timestamp))));
    }

    /// <summary>
    /// A <see cref="CompostingBinData"/> with 0-4 entries under one randomly
    /// named location; each entry key is a "X,Y" tile key with a consistent
    /// <see cref="CompostingBinStateData"/> value.
    /// </summary>
    public static Gen<CompostingBinData> BinDataSets()
    {
        return LocationNames()
            .SelectMany(
                location => BinEntries()
                    .Select(entries => CreateDataWithEntries(location, entries)));
    }

    private static Gen<KeyValuePair<string, CompostingBinStateData>> BinEntry()
    {
        return TileCoordinates()
            .SelectMany(
                coord => States().Select(state => new KeyValuePair<string, CompostingBinStateData>(
                    TileKey(coord.X, coord.Y),
                    CreateStateData(state, null, null))));
    }

    private static Gen<Dictionary<string, CompostingBinStateData>> BinEntries()
    {
        return Gen.Choose(0, 4)
            .SelectMany(
                count => Gen.ArrayOf(count, BinEntry()),
                (count, entries) => entries
                    .GroupBy(e => e.Key)
                    .ToDictionary(g => g.Key, g => g.First().Value));
    }

    // ------------------------------------------------------------------
    // Factory helpers — consistency-enforcing constructors
    // ------------------------------------------------------------------

    /// <summary>Formats a tile coordinate as the "X,Y" key used by the persistence dictionaries.</summary>
    public static string TileKey(int x, int y)
    {
        return $"{x},{y}";
    }

    /// <summary>
    /// Creates a <see cref="CompostingBinStateModel"/> with the given state and
    /// consistent input fields: Empty forces both to null; Processing/Ready
    /// require non-null values (defaulting to
    /// <see cref="ModConstants.CompostingBinItemId"/> and day 1 when not given).
    /// </summary>
    public static CompostingBinStateModel CreateBinWithState(
        CompostingBinState state,
        int tileX,
        int tileY,
        string? inputItemId,
        int? inputTimestamp)
    {
        var bin = new CompostingBinStateModel
        {
            TileX = tileX,
            TileY = tileY,
            State = state,
            MaturationLevel = 1,
            ConsecutiveIdleDays = 0,
            ConsecutiveActiveDays = 0
        };

        if (state == CompostingBinState.Empty)
        {
            bin.InputItemId = null;
            bin.InputTimestamp = null;
        }
        else
        {
            bin.InputItemId = string.IsNullOrEmpty(inputItemId)
                ? ModConstants.CompostingBinItemId
                : inputItemId;
            bin.InputTimestamp = inputTimestamp ?? 1;
        }

        return bin;
    }

    /// <summary>
    /// Creates a <see cref="CompostingBinStateData"/> DTO mirroring
    /// <see cref="CompostingBinStateModel"/> state with the same input
    /// consistency rule (Empty → null input; Processing/Ready → input present).
    /// </summary>
    public static CompostingBinStateData CreateStateData(
        CompostingBinState state,
        string? inputItemId,
        int? inputTimestamp)
    {
        var data = new CompostingBinStateData
        {
            State = state.ToString(),
            InputItemId = inputItemId,
            InputTimestamp = inputTimestamp,
            MaturationLevel = 1,
            ConsecutiveIdleDays = 0,
            ConsecutiveActiveDays = 0
        };

        if (state == CompostingBinState.Empty)
        {
            data.InputItemId = null;
            data.InputTimestamp = null;
        }
        else
        {
            data.InputItemId = string.IsNullOrEmpty(inputItemId)
                ? ModConstants.CompostingBinItemId
                : inputItemId;
            data.InputTimestamp = inputTimestamp ?? 1;
        }

        return data;
    }

    /// <summary>
    /// Creates a <see cref="CompostingBinData"/> holding the given location's
    /// "X,Y" → state-data entries under a single key.
    /// </summary>
    public static CompostingBinData CreateDataWithEntries(
        string locationName,
        IReadOnlyDictionary<string, CompostingBinStateData> entries)
    {
        var data = new CompostingBinData();
        data.LocationBinData[locationName] = new Dictionary<string, CompostingBinStateData>(entries);
        return data;
    }

    // ------------------------------------------------------------------
    // FsCheck registration
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers every <see cref="Arbitrary{T}"/> property above with FsCheck so
    /// <see cref="FsCheck.Xunit.Property"/>-attributed test methods can take the
    /// fixture types (tuple coordinates, <see cref="CompostingBinStateModel"/>,
    /// <see cref="CompostingBinData"/>, ...) directly as parameters.
    /// </summary>
    public static void Register()
    {
        Arb.Register(typeof(FsCheckFixture));
    }
}
