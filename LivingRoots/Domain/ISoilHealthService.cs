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
        /// Atomic bounded write of an explicit measured health value for one tile
        /// (017 T064). Mirrors <see cref="SetSoilHealth"/>'s measured-write path but
        /// reports whether the value was committed instead of silently dropping the
        /// write.
        /// </summary>
        /// <param name="locationName">The location name to validate and key the write by.</param>
        /// <param name="tile">
        /// The tile as integer grid coordinates. Values must be finite integers within
        /// <c>ModConstants.MaxAbsoluteTileCoordinate</c>; <see cref="Point"/> carries no
        /// NaN/Infinity, so callers must floor and bound-check <see cref="Vector2"/>
        /// coordinates before calling.
        /// </param>
        /// <param name="value">
        /// The health value. Clamped to <c>[ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth]</c>
        /// with the same NaN/Infinity mapping as <see cref="SetSoilHealth"/>: NaN clamps to
        /// <c>MinSoilHealth</c>, positive infinity to <c>MaxSoilHealth</c>, negative infinity to
        /// <c>MinSoilHealth</c>. An explicit <c>0f</c> is a measured floor reading and round-trips;
        /// a near-zero result reached only through clamping of a non-zero input follows the
        /// sparse-cache drop rule of the measured-write path.
        /// </param>
        /// <returns>
        /// <c>true</c> when the measured value was handled as a committed write (a near-zero
        /// clamped result reached only through clamping of a non-zero input follows the
        /// sparse-cache drop rule and counts as committed, mirroring the
        /// <see cref="SetSoilHealth"/> measured-write path). <c>false</c> when the write was
        /// refused — invalid location name, tile coordinate outside
        /// <c>ModConstants.MaxAbsoluteTileCoordinate</c>, or the
        /// <c>MaxLocationsPerSave</c>/<c>MaxTilesPerLocation</c> capacity guards — in which
        /// case nothing is persisted. Callers that need to know the commit landed MUST use
        /// this member instead of <see cref="SetSoilHealth"/>.
        /// </returns>
        /// <remarks>
        /// On a successful commit the implementation raises
        /// <see cref="OnTileHealthChanged"/> with the floored tile as a
        /// <see cref="Vector2"/>, mirroring the measured-write path of
        /// <see cref="SetSoilHealth"/>. The event fires outside the internal cache lock.
        /// </remarks>
        bool TrySetSoilHealth(string locationName, Point tile, float value);
    }
}
