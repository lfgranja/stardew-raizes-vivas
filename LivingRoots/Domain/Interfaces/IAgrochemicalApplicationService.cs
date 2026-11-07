using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// Outcome class for one 017 agrochemical application attempt.
    /// </summary>
    public enum AgrochemicalApplicationOutcome
    {
        Success,
        RefusedUnsupportedAuthority,
        RefusedInvalidTarget,
        RefusedMissingInput,
        RefusedConflict,
        RefusedSoilAtMinimum
    }

    /// <summary>
    /// Explicit result of one agrochemical application attempt.
    /// The service computes and commits domain state only; the controller owns
    /// held-item consumption, player feedback, and multiplayer broadcast.
    /// </summary>
    public sealed record AgrochemicalApplicationResult(
        AgrochemicalApplicationOutcome Outcome,
        string MessageKey,
        float BaselineUsed,
        float PostPenalty,
        float RequestedPenalty,
        float AppliedPenalty,
        bool WasClamped,
        bool BaselineWasUnknown)
    {
        public bool Succeeded
        {
            get { return Outcome == AgrochemicalApplicationOutcome.Success; }
        }
    }

    /// <summary>
    /// Applies the single Joja/agrochemical input to one soil tile (017 MVP).
    /// </summary>
    public interface IAgrochemicalApplicationService
    {
        /// <summary>
        /// Resolves the C2 baseline, computes the C3 bounded penalty, and on a
        /// successful authoritative path writes the explicit measured post-penalty
        /// value through <c>ISoilHealthService.SetSoilHealth</c>. Never consumes the
        /// held item, shows feedback, or broadcasts; those are controller-owned.
        /// </summary>
        AgrochemicalApplicationResult Apply(StardewValley.GameLocation location, Vector2 tile);
    }
}
