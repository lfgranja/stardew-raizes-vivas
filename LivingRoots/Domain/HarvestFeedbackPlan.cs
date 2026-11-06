using System;
using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>One floating-text/sprite message in a harvest feedback plan.</summary>
    public sealed record HarvestFeedbackMessage(string SpriteKey, string Text);

    /// <summary>
    /// Pure description of the transient feedback for one harvest. The Harmony patch
    /// turns it into <c>TemporaryAnimatedSprite</c> instances; keeping the plan pure
    /// lets the FR-005/FR-006/FR-007/FR-008 rules be tested without a booted game.
    /// </summary>
    public sealed record HarvestFeedbackPlan(
        HarvestFeedbackFlags Flags,
        IReadOnlyList<HarvestFeedbackMessage> Messages,
        string? SoundName,
        int DurationMs,
        bool BroadcastToAllPlayers)
    {
        /// <summary>No feedback (Moderate, Unknown, out of scope, or a silent Living harvest).</summary>
        public static HarvestFeedbackPlan None { get; } = new HarvestFeedbackPlan(
            HarvestFeedbackFlags.None,
            Array.Empty<HarvestFeedbackMessage>(),
            null,
            0,
            false);

        /// <summary>True when the plan has no observable feedback.</summary>
        public bool IsEmpty
        {
            get { return Flags == HarvestFeedbackFlags.None; }
        }
    }
}
