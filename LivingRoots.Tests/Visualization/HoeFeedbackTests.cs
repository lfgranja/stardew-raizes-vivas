using System;
using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HoeFeedbackTests
    {
        [Fact]
        public void Properties_CanBeSetAndGet()
        {
            // Arrange
            var startTime = new DateTime(2026, 1, 1, 12, 0, 0);
            var feedback = new HoeFeedback
            {
                TilePosition = new Point(5, 10),
                StartTime = startTime,
                FlashDuration = TimeSpan.FromMilliseconds(300),
                TextDuration = TimeSpan.FromMilliseconds(1000),
                HealthValue = 45.5f,
                Category = HealthCategory.Moderate,
                HealthText = "Soil Health: 46% (Moderate)"
            };

            // Assert
            Assert.Equal(new Point(5, 10), feedback.TilePosition);
            Assert.Equal(startTime, feedback.StartTime);
            Assert.Equal(TimeSpan.FromMilliseconds(300), feedback.FlashDuration);
            Assert.Equal(TimeSpan.FromMilliseconds(1000), feedback.TextDuration);
            Assert.Equal(45.5f, feedback.HealthValue);
            Assert.Equal(HealthCategory.Moderate, feedback.Category);
            Assert.Equal("Soil Health: 46% (Moderate)", feedback.HealthText);
        }

        [Fact]
        public void IsActive_ReturnsTrue_WhenElapsedLessThanMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = DateTime.UtcNow.AddMilliseconds(-500),
                FlashDuration = TimeSpan.FromMilliseconds(300),
                TextDuration = TimeSpan.FromMilliseconds(1000)
            };

            // Act & Assert
            // 500ms elapsed < 1000ms max duration → still active
            Assert.True(feedback.IsActive);
        }

        [Fact]
        public void IsActive_ReturnsFalse_WhenElapsedExceedsMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = DateTime.UtcNow.AddMilliseconds(-1500),
                FlashDuration = TimeSpan.FromMilliseconds(300),
                TextDuration = TimeSpan.FromMilliseconds(1000)
            };

            // Act & Assert
            // 1500ms elapsed > 1000ms max duration → not active
            Assert.False(feedback.IsActive);
        }

        [Fact]
        public void IsExpired_ReturnsTrue_WhenElapsedExceedsMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = DateTime.UtcNow.AddMilliseconds(-1500),
                FlashDuration = TimeSpan.FromMilliseconds(300),
                TextDuration = TimeSpan.FromMilliseconds(1000)
            };

            // Act & Assert
            Assert.True(feedback.IsExpired);
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenElapsedLessThanMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = DateTime.UtcNow.AddMilliseconds(-500),
                FlashDuration = TimeSpan.FromMilliseconds(300),
                TextDuration = TimeSpan.FromMilliseconds(1000)
            };

            // Act & Assert
            Assert.False(feedback.IsExpired);
        }
    }
}
