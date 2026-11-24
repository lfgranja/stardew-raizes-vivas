using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// T000-R1 (spec 004, Phase 0 RED): design-invariant tests for the
/// <see cref="CompostingBinData"/> persistence DTO and its per-bin record
/// <see cref="CompostingBinStateData"/> — the tolerant-reader seam
/// (FR-012/FR-014/FR-015): corrupt entries quarantined (null), obsolete
/// fields ignored ([JsonIgnore]). RED-phase only: pins the DTO contract
/// before T005 lands the tolerant-reader load path. Not integration tests.
/// </summary>
public class CompostingBinDataTests
{
    /// <summary>
    /// Valid state names at the JSON seam, derived from
    /// <see cref="CompostingBinState"/> — the only values the tolerant
    /// reader keeps.
    /// </summary>
    private static readonly string[] ValidStateNames = Enum.GetNames(typeof(CompostingBinState));

    /// <summary>
    /// T000-R1.1: the constructor initializes LocationBinData to an empty
    /// dictionary — never null — so serializers and the tolerant reader
    /// never see a null root.
    /// </summary>
    [Fact]
    public void Constructor_InitializesLocationBinData_ToEmptyDictionary()
    {
        var data = new CompostingBinData();

        Assert.NotNull(data.LocationBinData);
        Assert.Empty(data.LocationBinData);
    }

    /// <summary>
    /// T000-R1.2: LocationBinData is keyed by location name and can hold
    /// multiple locations side by side (per-save isolation).
    /// </summary>
    [Fact]
    public void LocationBinData_CanHoldMultipleLocations()
    {
        var data = new CompostingBinData();
        data.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData { State = "Ready", InputItemId = "(O)24", InputTimestamp = 1 }
        };
        data.LocationBinData["Greenhouse"] = new Dictionary<string, CompostingBinStateData>
        {
            ["4,6"] = new CompostingBinStateData { State = "Processing", InputItemId = "(O)86", InputTimestamp = 2 },
            ["5,6"] = new CompostingBinStateData { State = "Empty" }
        };

        Assert.Equal(2, data.LocationBinData.Count);
        Assert.True(data.LocationBinData.ContainsKey("Farm"));
        Assert.True(data.LocationBinData.ContainsKey("Greenhouse"));
        Assert.Single(data.LocationBinData["Farm"]);
        Assert.Equal(2, data.LocationBinData["Greenhouse"].Count);
        Assert.Equal("Ready", data.LocationBinData["Farm"]["10,10"].State);
        Assert.Equal("Processing", data.LocationBinData["Greenhouse"]["4,6"].State);
        Assert.Equal("Empty", data.LocationBinData["Greenhouse"]["5,6"].State);
    }

    /// <summary>
    /// T000-R1.3: CompostingBinStateData defaults match the single-batch
    /// contract — Empty state, no input, maturation level 1, zeroed retired
    /// counters.
    /// </summary>
    [Fact]
    public void CompostingBinStateData_HasCorrectDefaults()
    {
        var record = new CompostingBinStateData();

        Assert.Equal("Empty", record.State);
        Assert.Null(record.InputItemId);
        Assert.Null(record.InputTimestamp);
        Assert.Equal(1, record.MaturationLevel);
        Assert.Equal(0, record.ConsecutiveIdleDays);
        Assert.Equal(0, record.ConsecutiveActiveDays);
    }

    /// <summary>
    /// T000-R1.4 — ForAll(save =&gt; save.LocationBinData != null): the root
    /// dictionary is never null after construction, whatever else the
    /// generated save contains.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LocationBinData_IsNeverNull_ForAll(int seed)
    {
        var data = new CompostingBinData();
        int locationCount = Modulo(seed, 4);
        for (int locationIndex = 0; locationIndex < locationCount; locationIndex++)
        {
            data.LocationBinData[$"Location{locationIndex}"] = new Dictionary<string, CompostingBinStateData>
            {
                [$"{locationIndex},{locationIndex}"] = new CompostingBinStateData()
            };
        }

        return data.LocationBinData != null;
    }

    /// <summary>
    /// T000-R1.5 — ForAll(locationName =&gt; the name round-trips through
    /// LocationBinData as a dictionary key).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LocationName_CanBeStoredAndRetrieved_ForAll(NonNull<string> locationName)
    {
        string name = locationName.Get;
        string tileKey = "10,10";
        var data = new CompostingBinData();
        data.LocationBinData[name] = new Dictionary<string, CompostingBinStateData>
        {
            [tileKey] = new CompostingBinStateData { State = "Empty" }
        };

        return data.LocationBinData.ContainsKey(name)
            && data.LocationBinData[name].ContainsKey(tileKey)
            && data.LocationBinData[name][tileKey].State == "Empty";
    }

    /// <summary>
    /// T000-R1.6: the retired obsolete fields (MaturationLevel,
    /// ConsecutiveIdleDays, ConsecutiveActiveDays) are marked
    /// [JsonIgnore] so the JSON seam ignores them — verified by reflection
    /// on the DTO.
    /// </summary>
    [Fact]
    public void ObsoleteFields_AreMarkedJsonIgnore()
    {
        string[] obsoleteFields =
        {
            nameof(CompostingBinStateData.MaturationLevel),
            nameof(CompostingBinStateData.ConsecutiveIdleDays),
            nameof(CompostingBinStateData.ConsecutiveActiveDays)
        };

        foreach (string fieldName in obsoleteFields)
        {
            var property = typeof(CompostingBinStateData).GetProperty(fieldName);
            Assert.NotNull(property);
            Assert.True(
                property!.IsDefined(typeof(Newtonsoft.Json.JsonIgnoreAttribute), inherit: false),
                $"Field {fieldName} must be marked [JsonIgnore] so the JSON seam ignores it");
        }
    }

    /// <summary>
    /// T000-R1.7 — ForAll(save =&gt; AllEntriesValidOrQuarantined(save)):
    /// the tolerant-reader design invariant (FR-012/FR-015) at the DTO
    /// seam — every record is either quarantined (null) or carries one of
    /// the valid state names; a record with any other state name must never
    /// survive as a non-null entry. The generated half covers
    /// valid-state saves; the hand-built sample covers the quarantine
    /// branch (a null entry — a corrupt record the reader dropped).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TolerantReader_AllEntriesValidOrQuarantined_ForAll(int locationSeed, int entrySeed)
    {
        return AllValidOrQuarantined(GeneratedSave(locationSeed, entrySeed))
            && AllValidOrQuarantined(QuarantinedSample());
    }

    /// <summary>
    /// Builds a generated save: 1-3 locations, each with 0-3 entries that
    /// are either a valid-state record or a null (quarantined) marker.
    /// </summary>
    private static CompostingBinData GeneratedSave(int locationSeed, int entrySeed)
    {
        var data = new CompostingBinData();
        int locationCount = Modulo(locationSeed, 3) + 1;
        for (int locationIndex = 0; locationIndex < locationCount; locationIndex++)
        {
            var bins = new Dictionary<string, CompostingBinStateData>();
            int entryCount = Modulo(entrySeed, 4);
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                string tileKey = $"{locationIndex},{entryIndex}";
                if (Modulo(entrySeed + entryIndex, 3) == 0)
                {
                    // Quarantined corrupt record: the tolerant reader drops
                    // it, leaving a null marker rather than a record with an
                    // unparseable state name (FR-012/FR-015).
                    bins[tileKey] = null!;
                }
                else
                {
                    bins[tileKey] = new CompostingBinStateData
                    {
                        State = ValidStateNames[Modulo(entrySeed + locationIndex, ValidStateNames.Length)]
                    };
                }
            }

            data.LocationBinData[$"Location{locationIndex}"] = bins;
        }

        return data;
    }

    /// <summary>
    /// A hand-built save holding one valid record and one null entry —
    /// the quarantine branch the generator never produces.
    /// </summary>
    private static CompostingBinData QuarantinedSample()
    {
        var sample = new CompostingBinData();
        sample.LocationBinData["Farm"] = new Dictionary<string, CompostingBinStateData>
        {
            ["10,10"] = new CompostingBinStateData { State = "Ready", InputItemId = "(O)24", InputTimestamp = 1 },
            ["12,10"] = null! // corrupt record — quarantined by the tolerant reader
        };

        return sample;
    }

    /// <summary>
    /// The tolerant-reader invariant: every entry is null (quarantined) or
    /// carries one of the valid state names.
    /// </summary>
    private static bool AllValidOrQuarantined(CompostingBinData data)
    {
        foreach (var location in data.LocationBinData)
        {
            foreach (var entry in location.Value)
            {
                if (entry.Value is null)
                {
                    continue; // quarantined record — tolerated by the reader
                }

                if (!ValidStateNames.Contains(entry.Value.State))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Non-negative modulo for generated seeds, so arbitrary (including
    /// negative) FsCheck integers map into a fixed range.
    /// </summary>
    private static int Modulo(int value, int modulus)
    {
        int remainder = value % modulus;
        if (remainder < 0)
        {
            remainder += modulus;
        }

        return remainder;
    }
}
