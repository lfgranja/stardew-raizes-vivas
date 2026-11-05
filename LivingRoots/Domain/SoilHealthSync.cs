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
            throw new NotImplementedException("T118 implements host-to-client soil-health sync.");
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
            throw new NotImplementedException("T118 implements host-to-client soil-health sync.");
        }
    }
}
