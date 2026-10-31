namespace LivingRoots.Domain.Models
{
    /// <summary>
    /// Per-tile mulch state. Persisted via ModDataService with sparse cache
    /// (default values omitted). Farm-keyed via SaveIdProvider.
    /// </summary>
    public class MulchTileState
    {
        /// <summary>Whether the tile is currently mulched.</summary>
        public bool HasMulch { get; set; }

        /// <summary>Days remaining before mulch expires.</summary>
        public int DaysRemaining { get; set; }
    }
}