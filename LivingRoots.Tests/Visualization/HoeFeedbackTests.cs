using System;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HoeFeedbackTests
    {
        [Fact]
        public void Properties_Work()
        {
            var feedback = new HoeFeedback
            {
                TilePosition = new Point(3, 4),
                StartTime = DateTime.UtcNow,
                HealthValue = 75f,
                Category = HealthCategory.Healthy,
                HealthText = "Soil Health: 75% (Healthy)"
            };
            Assert.Equal(new Point(3, 4), feedback.TilePosition);
            Assert.Equal(75f, feedback.HealthValue);
            Assert.Equal(HealthCategory.Healthy, feedback.Category);
        }

        [Fact]
        public void IsActive_ReturnsTrue_WhenElapsedLessThanMaxDuration()
        {
            var feedback = new HoeFeedback { StartTime = DateTime.UtcNow };
            Assert.True(feedback.IsActive(DateTime.UtcNow));
        }

        [Fact]
        public void IsExpired_ReturnsTrue_WhenElapsedExceedsMaxDuration()
        {
            var feedback = new HoeFeedback { StartTime = DateTime.UtcNow.AddMilliseconds(-2000) };
            Assert.True(feedback.IsExpired(DateTime.UtcNow));
        }

        [Fact]
        public void DefaultDurations_AreCorrect()
        {
            var feedback = new HoeFeedback();
            Assert.Equal(300, feedback.FlashDuration.TotalMilliseconds);
            Assert.Equal(1000, feedback.TextDuration.TotalMilliseconds);
        }
    }
}
