using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    public class HoeFeedback
    {
        public Point TilePosition { get; set; }
        public long StartTime { get; set; }
        public int FlashDuration { get; set; } = 300;
        public int TextDuration { get; set; } = 1000;
        public float HealthValue { get; set; }
        public HealthCategory Category { get; set; }
        public string HealthText { get; set; } = string.Empty;

        public bool IsActive(long currentTime) =>
            currentTime - StartTime < System.Math.Max(FlashDuration, TextDuration);

        public bool IsExpired(long currentTime) =>
            currentTime - StartTime >= System.Math.Max(FlashDuration, TextDuration);
    }
}
