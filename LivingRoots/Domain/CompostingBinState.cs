namespace LivingRoots.Domain;

/// <summary>
/// Represents the current operational state of a composting bin machine.
/// State transitions: Empty → Processing → Ready → Empty (cycle repeats).
/// </summary>
public enum CompostingBinState
{
    /// <summary>Bin is empty and ready to accept organic waste.</summary>
    Empty = 0,

    /// <summary>Bin is processing input waste into compost.</summary>
    Processing = 1,

    /// <summary>Bin has finished processing and compost is ready for collection.</summary>
    Ready = 2
}
