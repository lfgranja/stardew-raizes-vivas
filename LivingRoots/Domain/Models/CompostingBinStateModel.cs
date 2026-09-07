namespace LivingRoots.Domain.Models;

/// <summary>
/// Runtime state for a single composting bin machine.
/// Tracks tile position, operational state, input item, maturation, and idle time.
/// </summary>
public class CompostingBinStateModel
{
    /// <summary>X tile coordinate of the bin.</summary>
    public int TileX { get; set; }

    /// <summary>Y tile coordinate of the bin.</summary>
    public int TileY { get; set; }

    /// <summary>Current operational state of the bin.</summary>
    public CompostingBinState State { get; set; }

    /// <summary>Qualified item ID of the input waste item (null if empty).</summary>
    public string? InputItemId { get; set; }

    /// <summary>Game time in minutes when waste was added (null if empty).</summary>
    public long? InputTimestamp { get; set; }

    /// <summary>Current maturation level (1-5), determines output multiplier.</summary>
    public int MaturationLevel { get; set; } = 1;

    /// <summary>Consecutive days without activity, used for maturation reset.</summary>
    public int ConsecutiveIdleDays { get; set; } = 0;

    /// <summary>Days of continuous operation, used for maturation increment.</summary>
    public int ConsecutiveActiveDays { get; set; } = 0;
}
