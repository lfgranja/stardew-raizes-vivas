using System;
using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Host-to-client soil-health snapshot for the replicated deterministic roll
    /// (FR-009/A8). Pure data so serialization and apply rules can be tested
    /// without a multiplayer session.
    /// </summary>
    public sealed record SoilHealthSyncMessage(
        string SaveId,
        int Day,
        IReadOnlyList<SoilHealthSnapshotEntry> Entries);

    /// <summary>Builds and applies the host-authoritative soil-health snapshot.</summary>
    public static class SoilHealthSync
    {
        /// <summary>Builds the DayStarted/host snapshot payload.</summary>
        public static SoilHealthSyncMessage Create(
            string saveId,
            int day,
            IReadOnlyList<SoilHealthSnapshotEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                throw new ArgumentException("saveId is required", nameof(saveId));
            }

            return new SoilHealthSyncMessage(saveId, day, entries ?? Array.Empty<SoilHealthSnapshotEntry>());
        }

        /// <summary>
        /// Applies a snapshot to the local cache. Entries from a different save are
        /// ignored (host state wins, but never across saves). Returns the number of
        /// tiles applied.
        /// </summary>
        public static int Apply(
            SoilHealthSyncMessage message,
            string currentSaveId,
            Action<string, int, int, float> applyTile)
        {
            if (message == null || applyTile == null)
            {
                return 0;
            }

            if (!string.Equals(message.SaveId, currentSaveId, StringComparison.Ordinal))
            {
                return 0;
            }

            int applied = 0;
            foreach (var entry in message.Entries)
            {
                applyTile(entry.LocationName, entry.TileX, entry.TileY, entry.Health);
                applied++;
            }

            return applied;
        }
    }
}
