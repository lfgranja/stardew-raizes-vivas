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
