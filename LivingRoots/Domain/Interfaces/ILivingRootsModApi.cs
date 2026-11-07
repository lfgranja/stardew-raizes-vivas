namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// SMAPI-free public contract exposed by the Living Roots mod so that
    /// dev/CI-only verification mods can subscribe to deterministic signals
    /// (e.g. the tail of <c>OnDayEnding</c> after <c>ApplySoilGrowthModifier</c>).
    /// Raised synchronously on the game thread.
    /// </summary>
    public interface ILivingRootsModApi
    {
        /// <summary>API version string for compatibility checks.</summary>
        string ApiVersion { get; }

        /// <summary>
        /// Raised synchronously on the game thread at the tail of
        /// <c>OnDayEnding</c>, after <c>ApplySoilGrowthModifier</c> has run
        /// for all locations (success or caught exception), before the day
        /// transition.
        /// </summary>
        event EventHandler<SoilGrowthModifierCompletedEventArgs>? SoilGrowthModifierCompleted;

        /// <summary>
        /// True when the production mod has bound the verification services.
        /// The dev/CI verification mod refuses to act when this is false, so a
        /// partially wired composition root fails loudly instead of silently
        /// dropping scenario commands.
        /// </summary>
        bool IsVerificationReady { get; }

        /// <summary>
        /// Reads the stored health for one tile. Returns false when the tile
        /// has no entry; a stored NaN returns true with a NaN value.
        /// </summary>
        bool TryGetSoilHealth(string locationName, int tileX, int tileY, out float health);

        /// <summary>Sets the stored health for one tile (clamped by the service).</summary>
        void SetSoilHealth(string locationName, int tileX, int tileY, float health);

        /// <summary>Returns a point-in-time copy of every stored tile.</summary>
        IReadOnlyList<SoilHealthSnapshotEntry> SnapshotSoilHealth();

        /// <summary>
        /// Places the composting bin on a farm/greenhouse tile. Returns false
        /// when the location or tile is invalid or already occupied.
        /// </summary>
        bool PlaceBin(string locationName, int tileX, int tileY);

        /// <summary>
        /// Inserts organic waste into the bin at the tile. Returns false when
        /// the item is not valid organic waste or the bin is not empty.
        /// </summary>
        bool InsertWaste(string locationName, int tileX, int tileY, string itemId, int count);

        /// <summary>
        /// Dev/CI-only host path for the 017 Joja/agrochemical input. Applies the
        /// item through the same authoritative controller route as a right-click
        /// interaction. The application delegate is bound unconditionally at
        /// wiring time and binding is not host-restricted at that point. Host
        /// authority, target eligibility, held-item and lock gates are enforced
        /// downstream by ModController (the <c>IsMainPlayerCheck</c> authority
        /// gate and the application route), not by this API. Any in-process mod
        /// may call <c>ApplyAgrochemical</c>, but no call can produce an unsafe
        /// local mutation or bypass host authority: a non-host caller is refused
        /// by the controller before any service, lock, soil, or consumption
        /// effect. Dev/CI-only usage is the expected consumption of this member;
        /// it is not a privilege boundary. Returns false when the application
        /// delegate is not bound or the application is refused or fails; the API
        /// performs no soil mutation itself.
        /// </summary>
        bool ApplyAgrochemical(string locationName, int tileX, int tileY);

        /// <summary>
        /// Dev/CI-only host path for 013 mulch application. The delegate is bound
        /// at wiring time and delegates to <see cref="IMulchApplicationService.TryApplyMulch"/>.
        /// Host authority and eligibility (tilled/valid location, not already mulched,
        /// held mulch item) are enforced by the service/delegate path; the API
        /// performs no mutation itself. Returns false when unbound, refused, or
        /// failed. Intended for headless verification; not a gameplay privilege boundary.
        /// </summary>
        bool ApplyMulch(string locationName, int tileX, int tileY);
    }

    /// <summary>
    /// Payload carried by <see cref="ILivingRootsModApi.SoilGrowthModifierCompleted"/>.
    /// </summary>
    public sealed class SoilGrowthModifierCompletedEventArgs : EventArgs
    {
        /// <summary>Day key (e.g. "spring_1") for the day that just ended.</summary>
        public string DayKey { get; set; } = string.Empty;

        /// <summary>Whether the acting player is the main (host) player.</summary>
        public bool IsMainPlayer { get; set; }

        /// <summary>Number of locations processed by the modifier.</summary>
        public int LocationsProcessed { get; set; }

        /// <summary>Elapsed wall-clock milliseconds for the modifier run.</summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>Whether the modifier completed without an unhandled exception.</summary>
        public bool Success { get; set; }

        /// <summary>Error message when <see cref="Success"/> is false; empty otherwise.</summary>
        public string Error { get; set; } = string.Empty;
    }
}
