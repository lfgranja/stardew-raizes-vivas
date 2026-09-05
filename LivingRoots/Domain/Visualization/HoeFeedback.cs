using System;
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Transient state for hoe action visual feedback.
    /// Flash effect lasts 300ms, floating text lasts 1000ms.
    /// </summary>
    public class HoeFeedback
    {
        public Point TilePosition { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan FlashDuration { get; set; } = TimeSpan.FromMilliseconds(300);
        public TimeSpan TextDuration { get; set; } = TimeSpan.FromMilliseconds(1000);
        public float HealthValue { get; set; }
        public HealthCategory Category { get; set; }
        public string? HealthText { get; set; }

        /// <summary>
        /// Returns true if the feedback is still active (elapsed time &lt; max duration).
        /// </summary>
        public bool IsActive
        {
            get
            {
                var elapsed = DateTime.UtcNow - StartTime;
                var maxDuration = FlashDuration > TextDuration ? FlashDuration : TextDuration;
                return elapsed < maxDuration;
            }
        }

        /// <summary>
        /// Returns true if the feedback has expired (elapsed time &gt;= max duration).
        /// </summary>
        public bool IsExpired
        {
            get
            {
                var elapsed = DateTime.UtcNow - StartTime;
                var maxDuration = FlashDuration > TextDuration ? FlashDuration : TextDuration;
                return elapsed >= maxDuration;
            }
        }
    }
}
