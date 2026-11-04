namespace LivingRoots.Domain
{
    /// <summary>
    /// One measured soil-health entry in a snapshot of the runtime cache.
    /// The snapshot is a verification seam: it reports exactly what is stored,
    /// so a missing tile is distinguishable from a stored NaN or zero.
    /// </summary>
    public sealed class SoilHealthSnapshotEntry
    {
        public SoilHealthSnapshotEntry(string locationName, int tileX, int tileY, float health)
        {
            LocationName = locationName;
            TileX = tileX;
            TileY = tileY;
            Health = health;
        }

        /// <summary>The location the entry belongs to.</summary>
        public string LocationName { get; }

        /// <summary>The floored tile X coordinate.</summary>
        public int TileX { get; }

        /// <summary>The floored tile Y coordinate.</summary>
        public int TileY { get; }

        /// <summary>The stored value (may be NaN when a NaN was persisted).</summary>
        public float Health { get; }
    }
}
