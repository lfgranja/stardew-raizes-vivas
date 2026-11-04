using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    public interface ISoilHealthService
    {
        event Action<string, Vector2, float>? OnTileHealthChanged;

        void LoadData(string saveId);
        void SaveData(string saveId);
        float GetSoilHealth(string locationName, Vector2 tile);
        /// <summary>
        /// Reads the stored value for a tile, distinguishing "no entry" from a
        /// stored NaN. <see cref="GetSoilHealth"/> returns the NaN sentinel for
        /// both a missing key and a stored NaN; verification needs the
        /// difference to report <c>missing</c> versus <c>nan</c>.
        /// </summary>
        /// <returns>True when the tile has an entry (the value may be NaN).</returns>
        bool TryGetSoilHealth(string locationName, Vector2 tile, out float health);
        /// <summary>
        /// Returns a point-in-time copy of every stored tile. The snapshot is
        /// taken under the cache lock so callers never enumerate a mutating
        /// dictionary.
        /// </summary>
        IReadOnlyList<SoilHealthSnapshotEntry> Snapshot();
        void SetSoilHealth(string locationName, Vector2 tile, float value);
        void UpdateHealth(string locationName, Vector2 tile, float delta);
        void Reset();
    }
}
