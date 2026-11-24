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
            var feedback = new HoeFeedback
            {
                TilePosition = new Point(5, 10),
                StartTime = 1000,
                FlashDuration = 300,
                TextDuration = 1000,
                HealthValue = 45.5f,
                Category = HealthCategory.Moderate,
                HealthText = "Soil Health: 46% (Moderate)"
            };

            // Assert
            Assert.Equal(new Point(5, 10), feedback.TilePosition);
            Assert.Equal(1000, feedback.StartTime);
            Assert.Equal(300, feedback.FlashDuration);
            Assert.Equal(1000, feedback.TextDuration);
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
                StartTime = 1000,
                FlashDuration = 300,
                TextDuration = 1000
            };

            // Act & Assert
            // 500ms elapsed < 1000ms max duration → still active
            Assert.True(feedback.IsActive(1500));
        }

        [Fact]
        public void IsActive_ReturnsFalse_WhenElapsedExceedsMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = 1000,
                FlashDuration = 300,
                TextDuration = 1000
            };

            // Act & Assert
            // 1500ms elapsed > 1000ms max duration → not active
            Assert.False(feedback.IsActive(2500));
        }

        [Fact]
        public void IsExpired_ReturnsTrue_WhenElapsedExceedsMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = 1000,
                FlashDuration = 300,
                TextDuration = 1000
            };

            // Act & Assert
            // 1500ms elapsed >= 1000ms max duration → expired
            Assert.True(feedback.IsExpired(2500));
        }

        [Fact]
        public void IsExpired_ReturnsFalse_WhenElapsedLessThanMaxDuration()
        {
            // Arrange
            var feedback = new HoeFeedback
            {
                StartTime = 1000,
                FlashDuration = 300,
                TextDuration = 1000
            };

            // Act & Assert
            // 500ms elapsed < 1000ms max duration → not expired
            Assert.False(feedback.IsExpired(1500));
        }

        [Fact]
        public void Constructor_SetsProperties()
        {
            var feedback = new HoeFeedback(new Point(3, 4), 5000, 300, 1000, 75.0f);

            Assert.Equal(new Point(3, 4), feedback.TilePosition);
            Assert.Equal(5000, feedback.StartTime);
            Assert.Equal(300, feedback.FlashDuration);
            Assert.Equal(1000, feedback.TextDuration);
            Assert.Equal(75.0f, feedback.HealthValue);
        }

        [Fact]
        public void DefaultConstructor_SetsDefaultDurations()
        {
            var feedback = new HoeFeedback();

            Assert.Equal(300, feedback.FlashDuration);
            Assert.Equal(1000, feedback.TextDuration);
        }
    }
}
