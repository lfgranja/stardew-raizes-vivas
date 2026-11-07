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

        /// <summary>
        /// 017 T064 scaffold (RED phase): atomic bounded write that reports
        /// whether the value was committed. Returns <c>false</c> when the tile
        /// is rejected (invalid tile or capacity limit) instead of silently
        /// dropping the write.
        /// </summary>
        /// <remarks>
        /// Default interface member that throws so the 017 RED tests compile
        /// before the real implementation lands. No <c>SoilHealthService</c>
        /// implementation exists in this scaffold; the member is expected to be
        /// replaced by a concrete implementation in the T064 wave. Callers must
        /// not invoke it on production paths until then.
        /// </remarks>
        bool TrySetSoilHealth(string locationName, Point tile, float value)
        {
            throw new NotImplementedException();
        }
    }
}
