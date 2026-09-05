using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the <see cref="HoeFeedback"/> domain model.
    /// Verifies property storage, default durations, and timing logic for IsActive/IsExpired.
    /// </summary>
    public class HoeFeedbackTests
    {
        // ──────────────────────────────────────────────
        // Property Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void TilePosition_ShouldStoreAndReturnValue()
        {
            // Arrange
            var expected = new Point(5, 10);
            var feedback = new HoeFeedback { TilePosition = expected };

            // Act
            var actual = feedback.TilePosition;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StartTime_ShouldStoreAndReturnValue()
        {
            // Arrange
            const long expected = 12345678L;
            var feedback = new HoeFeedback { StartTime = expected };

            // Act
            var actual = feedback.StartTime;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FlashDuration_ShouldDefaultTo300()
        {
            // Arrange
            var feedback = new HoeFeedback();

            // Act
            var actual = feedback.FlashDuration;

            // Assert
            Assert.Equal(300, actual);
        }

        [Fact]
        public void TextDuration_ShouldDefaultTo1000()
        {
            // Arrange
            var feedback = new HoeFeedback();

            // Act
            var actual = feedback.TextDuration;

            // Assert
            Assert.Equal(1000, actual);
        }

        [Fact]
        public void HealthValue_ShouldStoreAndReturnValue()
        {
            // Arrange
            const float expected = 0.67f;
            var feedback = new HoeFeedback { HealthValue = expected };

            // Act
            var actual = feedback.HealthValue;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Category_ShouldStoreAndReturnValue()
        {
            // Arrange
            var feedback = new HoeFeedback { Category = HealthCategory.Healthy };

            // Act
            var actual = feedback.Category;

            // Assert
            Assert.Equal(HealthCategory.Healthy, actual);
        }

        [Fact]
        public void HealthText_ShouldStoreAndReturnValue()
        {
            // Arrange
            const string expected = "Healthy";
            var feedback = new HoeFeedback { HealthText = expected };

            // Act
            var actual = feedback.HealthText;

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // IsActive Timing Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void IsActive_AtStartTime_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsActive(startTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsActive_At299Ms_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 299;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsActive(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsActive_At999Ms_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 999;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsActive(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsActive_AtMaxDurationMinusOne_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 999; // Max(300, 1000) - 1 = 999
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsActive(currentTime);

            // Assert
            Assert.True(actual);
        }

        // ──────────────────────────────────────────────
        // IsExpired Timing Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void IsExpired_AtMaxDuration_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 1000; // Max(300, 1000) = 1000
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsExpired_AtMaxDurationPlusOne_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 1001;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsExpired_AtLargeElapsedTime_ShouldReturnTrue()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 5000;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsExpired_BeforeMaxDuration_ShouldReturnFalse()
        {
            // Arrange
            const long startTime = 0;
            const long currentTime = 999;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(currentTime);

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void IsExpired_AtStartTime_ShouldReturnFalse()
        {
            // Arrange
            const long startTime = 0;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(startTime);

            // Assert
            Assert.False(actual);
        }

        // ──────────────────────────────────────────────
        // IsActive / IsExpired Mutual Exclusion
        // ──────────────────────────────────────────────

        [Fact]
        public void IsActive_AndIsExpired_ShouldBeMutuallyExclusive()
        {
            // Arrange
            const long startTime = 0;
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act & Assert - before max duration
            Assert.True(feedback.IsActive(500));
            Assert.False(feedback.IsExpired(500));

            // Act & Assert - at/after max duration
            Assert.False(feedback.IsActive(1000));
            Assert.True(feedback.IsExpired(1000));
        }

        // ──────────────────────────────────────────────
        // Non-Zero StartTime Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void IsActive_WithNonZeroStartTime_ShouldCalculateElapsedCorrectly()
        {
            // Arrange
            const long startTime = 10000;
            const long currentTime = 10900; // elapsed = 900 < 1000
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsActive(currentTime);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void IsExpired_WithNonZeroStartTime_ShouldCalculateElapsedCorrectly()
        {
            // Arrange
            const long startTime = 10000;
            const long currentTime = 11000; // elapsed = 1000 >= 1000
            var feedback = new HoeFeedback { StartTime = startTime };

            // Act
            var actual = feedback.IsExpired(currentTime);

            // Assert
            Assert.True(actual);
        }
    }
}
