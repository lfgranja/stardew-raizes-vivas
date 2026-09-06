using System;
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Transient state for hoe action visual feedback.
    /// Flash effect lasts 300ms, floating text lasts 1000ms.
    /// Timing uses game clock milliseconds (long) for frame-accurate animation.
    /// </summary>
    public class HoeFeedback
    {
        public Point TilePosition { get; set; }
        public long StartTime { get; set; }
        public long FlashDuration { get; set; } = 300;
        public long TextDuration { get; set; } = 1000;
        public float HealthValue { get; set; }
        public HealthCategory Category { get; set; }
        public string? HealthText { get; set; }

        public HoeFeedback()
        {
        }

        public HoeFeedback(Point tilePosition, long startTime, long flashDuration, long textDuration, float healthValue)
        {
            TilePosition = tilePosition;
            StartTime = startTime;
            FlashDuration = flashDuration;
            TextDuration = textDuration;
            HealthValue = healthValue;
        }

        /// <summary>
        /// Returns true if the feedback is still active at the given game time.
        /// </summary>
        public bool IsActive(long currentTime)
        {
            var elapsed = currentTime - StartTime;
            var maxDuration = FlashDuration > TextDuration ? FlashDuration : TextDuration;
            return elapsed >= 0 && elapsed < maxDuration;
        }

        /// <summary>
        /// Returns true if the feedback has expired at the given game time.
        /// </summary>
        public bool IsExpired(long currentTime)
        {
            var elapsed = currentTime - StartTime;
            var maxDuration = FlashDuration > TextDuration ? FlashDuration : TextDuration;
            return elapsed >= maxDuration;
        }
    }
}
