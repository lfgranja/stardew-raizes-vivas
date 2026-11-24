using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Persisted state of mulch for the entire save.
    /// Key: Location Name. Value: Dictionary mapping "X,Y" tile key to (HasMulch, DaysRemaining).
    /// </summary>
    public class MulchState
    {
        public Dictionary<string, Dictionary<string, MulchTileEntry>> LocationMulchData { get; set; } = new();
    }

    /// <summary>
    /// Persisted mulch entry for a single tile.
    /// </summary>
    public class MulchTileEntry
    {
        public bool HasMulch { get; set; }
        public int DaysRemaining { get; set; }
    }
}
