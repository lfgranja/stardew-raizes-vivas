using System.Collections.Generic;

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

    /// <summary>Game time in minutes when waste was added, or null.</summary>
    public long? InputTimestamp { get; set; }

    /// <summary>Maturation level (1-5).</summary>
    public int MaturationLevel { get; set; } = 1;

    /// <summary>Consecutive idle days since last activity (0-14).</summary>
    public int ConsecutiveIdleDays { get; set; } = 0;
}
