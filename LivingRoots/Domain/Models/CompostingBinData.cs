using System.Collections.Generic;
using Newtonsoft.Json;

namespace LivingRoots.Domain.Models;

/// <summary>
/// Persisted data for all composting bins across all locations.
/// Serialized to JSON via IModDataService.
/// </summary>
public class CompostingBinData
{
    /// <summary>
    /// Key: location name → key: "X,Y" tile → bin state data.
    /// </summary>
    public Dictionary<string, Dictionary<string, CompostingBinStateData>> LocationBinData { get; set; } = new();
}

/// <summary>
/// Serializable DTO for a single composting bin's state.
/// Used for JSON serialization/deserialization.
/// </summary>
public class CompostingBinStateData
{
    /// <summary>"Empty", "Processing", or "Ready".</summary>
    public string State { get; set; } = "Empty";

    /// <summary>Qualified item ID of input waste, or null.</summary>
    public string? InputItemId { get; set; }

    /// <summary>Game day when waste was added via ITimeProvider.TotalDays, or null.</summary>
    public int? InputTimestamp { get; set; }

    /// <summary>Maturation level (1-5).</summary>
    /// <remarks>
    /// T252 (FR-005a/FR-012): retired multiplier — the JSON seam ignores it
    /// (JsonIgnore) so no maturation trace is re-written to disk. Kept
    /// in memory for save-compat observability only.
    /// </remarks>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int MaturationLevel { get; set; } = 1;

    /// <summary>Consecutive idle days since last activity (0-14).</summary>
    /// <remarks>
    /// T252 (FR-012): retired idle-reset counter — ignored by the JSON seam.
    /// </remarks>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int ConsecutiveIdleDays { get; set; } = 0;

    /// <summary>Consecutive active days (Processing state) for maturation increment.</summary>
    /// <remarks>
    /// T252 (FR-012): retired level-up counter — ignored by the JSON seam.
    /// </remarks>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int ConsecutiveActiveDays { get; set; } = 0;
}
