using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// Outcome class for one 017 agrochemical application attempt.
    /// </summary>
    public enum AgrochemicalApplicationOutcome
    {
        Success,

        /// <remarks>
        /// Reserved (T074): synthesized by the controller for the unsupported-authority
        /// path (farmhand on the host-authoritative MVP); never produced by the service.
        /// Farmhand refusals occur before the service is called, in controller orchestration.
        /// </remarks>
        RefusedUnsupportedAuthority,

        RefusedInvalidTarget,

        /// <remarks>
        /// Reserved (T074): synthesized by the controller for the missing/invalid held-input
        /// path; never produced by the service. Input refusals occur before the service is
        /// called, in controller orchestration.
        /// </remarks>
        RefusedMissingInput,

        /// <remarks>
        /// Reserved for the controller-synthesized first-wins per-tile lock-contention class.
        /// The service no longer reports this outcome for a commit-drop refusal: seam commit
        /// failures are reported as <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/>.
        /// </remarks>
        RefusedConflict,

        RefusedSoilAtMinimum,

        /// <summary>
        /// Seam commit failed: the measured write was declined (capacity limit or a tile
        /// the soil service cannot persist). Result carries the computed baseline/penalty
        /// data with <c>RefusedCommitFailed</c> and
        /// <c>TranslationKeyAgrochemicalCommitFailed</c>; never reported as success.
        /// </summary>
        RefusedCommitFailed
    }

    /// <summary>
    /// Explicit result of one agrochemical application attempt.
    /// The service computes and commits domain state only; the controller owns
    /// held-item consumption, player feedback, and multiplayer broadcast.
    /// </summary>
    /// <remarks>
    /// Refusal outcomes that never read the soil (<see cref="AgrochemicalApplicationOutcome.RefusedInvalidTarget"/>)
    /// carry <see cref="float.NaN"/> sentinels for <see cref="BaselineUsed"/>, <see cref="PostPenalty"/>,
    /// and <see cref="AppliedPenalty"/>; NaN means "no value was resolved". A commit-drop refusal
    /// (the soil-health seam declined the write) instead carries the computed
    /// baseline/penalty values with <see cref="WasClamped"/> as computed — the computation did
    /// occur, only the commit failed. <see cref="AgrochemicalApplicationOutcome.RefusedSoilAtMinimum"/>
    /// carries the real measured baseline (0f) and is never a NaN sentinel.
    /// <see cref="Succeeded"/> is the authoritative success signal.
    /// </remarks>
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
    /// <remarks>
    /// Authoritative-context precondition (T086): <see cref="IAgrochemicalApplicationService.Apply(StardewValley.GameLocation, Vector2)"/>
    /// MUST be called only from an authoritative, lock-held host context (see the ModController
    /// orchestration: authority check, per-tile lock acquire, eligible-target validation all happen
    /// before this call). The service never acts as a second source of truth: it performs no
    /// authority, held-item, or UI checks of its own and trusts the caller's context.
    /// Single-player is the degenerate host case. Farmhand paths are refused before this call by
    /// the controller; calling <c>Apply</c> from a farmhand path violates this contract.
    /// </remarks>
    public interface IAgrochemicalApplicationService
    {
        /// <summary>
        /// Resolves the C2 baseline, computes the C3 bounded penalty, and on a
        /// successful authoritative path writes the explicit measured post-penalty
        /// value through <c>ISoilHealthService.TrySetSoilHealth</c>. Never consumes the
        /// held item, shows feedback, or broadcasts; those are controller-owned.
        /// </summary>
        /// <remarks>
        /// Authoritative-context precondition: MUST be called only from an authoritative,
        /// lock-held host context (see ModController orchestration), never as a second source
        /// of truth; single-player is the degenerate host case; farmhand paths are refused
        /// before this call by the controller. On a commit-drop refusal (the soil-health seam
        /// returned <c>false</c>), the result carries the computed baseline/penalty data with
        /// the <see cref="AgrochemicalApplicationOutcome.RefusedCommitFailed"/> outcome and the
        /// commit-failed message key, and is never reported as success. Refusal outcomes that never
        /// read the soil carry NaN sentinels for <see cref="AgrochemicalApplicationResult.BaselineUsed"/>,
        /// <see cref="AgrochemicalApplicationResult.PostPenalty"/>, and
        /// <see cref="AgrochemicalApplicationResult.AppliedPenalty"/>.
        /// </remarks>
        AgrochemicalApplicationResult Apply(StardewValley.GameLocation location, Vector2 tile);
    }
}
