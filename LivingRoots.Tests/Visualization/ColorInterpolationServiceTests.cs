using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for <see cref="ColorInterpolationService"/>.
    /// Verifies linear RGB interpolation (FR-008), category boundaries,
    /// caching, cache invalidation, clamping, and NaN/Infinity handling.
    /// </summary>
    public class ColorInterpolationServiceTests
    {
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly ColorInterpolationService _service;

        // Expected base colors (defaults from the service constructor)
        private static readonly Color PoorColor = new Color(255, 0, 0, 255);
        private static readonly Color ModerateColor = new Color(255, 255, 0, 255);
        private static readonly Color HealthyColor = new Color(0, 255, 0, 255);
        private static readonly Color UnknownColor = new Color(128, 128, 128, 255);

        public ColorInterpolationServiceTests()
        {
            _mockMonitor = new Mock<IMonitor>();
            _service = new ColorInterpolationService(_mockMonitor.Object);
        }

        // ──────────────────────────────────────────────
        // Linear RGB Interpolation (FR-008)
        // ──────────────────────────────────────────────

        [Fact]
        public void GetColorForHealth_HealthZero_ReturnsPoorColor()
        {
            // Act
            var result = _service.GetColorForHealth(0f);

            // Assert
            Assert.Equal(PoorColor, result);
        }

        [Fact]
        public void GetColorForHealth_HealthHundred_ReturnsHealthyColor()
        {
            // Act
            var result = _service.GetColorForHealth(100f);

            // Assert
            Assert.Equal(HealthyColor, result);
        }

        [Fact]
        public void GetColorForHealth_HealthFifty_ReturnsMidpointBetweenModerateAndHealthy()
        {
            // Arrange
            // health=50 falls in Moderate range [34, 67).
            // t = (50 - 34) / (67 - 34) = 16/33
            // r = Lerp(255, 0, 16/33) = 255 + (0 - 255) * 16/33 ≈ 131
            // g = Lerp(255, 255, 16/33) = 255
            // b = Lerp(0, 0, 16/33) = 0
            // a = 255
            var expected = new Color(131, 255, 0, 255);

            // Act
            var result = _service.GetColorForHealth(50f);

            // Assert
            Assert.Equal(expected.R, result.R);
            Assert.Equal(expected.G, result.G);
            Assert.Equal(expected.B, result.B);
            Assert.Equal(expected.A, result.A);
        }

        // ──────────────────────────────────────────────
        // GetCategoryForHealth Boundary Values
        // ──────────────────────────────────────────────

        [Fact]
        public void GetCategoryForHealth_HealthZero_ReturnsPoor()
        {
            // Act
            var result = _service.GetCategoryForHealth(0f);

            // Assert
            Assert.Equal(HealthCategory.Poor, result);
        }

        [Fact]
        public void GetCategoryForHealth_HealthThirtyThree_ReturnsPoor()
        {
            // Act
            var result = _service.GetCategoryForHealth(33f);

            // Assert
            Assert.Equal(HealthCategory.Poor, result);
        }

        [Fact]
        public void GetCategoryForHealth_HealthThirtyFour_ReturnsModerate()
        {
            // Act
            var result = _service.GetCategoryForHealth(34f);

            // Assert
            Assert.Equal(HealthCategory.Moderate, result);
        }

        [Fact]
        public void GetCategoryForHealth_HealthSixtySix_ReturnsModerate()
        {
            // Act
            var result = _service.GetCategoryForHealth(66f);

            // Assert
            Assert.Equal(HealthCategory.Moderate, result);
        }

        [Fact]
        public void GetCategoryForHealth_HealthSixtySeven_ReturnsHealthy()
        {
            // Act
            var result = _service.GetCategoryForHealth(67f);

            // Assert
            Assert.Equal(HealthCategory.Healthy, result);
        }

        [Fact]
        public void GetCategoryForHealth_HealthHundred_ReturnsHealthy()
        {
            // Act
            var result = _service.GetCategoryForHealth(100f);

            // Assert
            Assert.Equal(HealthCategory.Healthy, result);
        }

        // ──────────────────────────────────────────────
        // Caching
        // ──────────────────────────────────────────────

        [Fact]
        public void GetColorForHealth_CalledTwice_WithSameValue_ReturnsSameResult()
        {
            // Arrange
            float health = 42.5f;

            // Act
            var first = _service.GetColorForHealth(health);
            var second = _service.GetColorForHealth(health);

            // Assert
            Assert.Equal(first, second);
        }

        // ──────────────────────────────────────────────
        // InvalidateCache
        // ──────────────────────────────────────────────

        [Fact]
        public void InvalidateCache_ClearsCache_AndLogsMessage()
        {
            // Arrange — populate the cache
            _service.GetColorForHealth(50f);

            // Act
            _service.InvalidateCache();

            // Assert — verify the log message was emitted
            _mockMonitor.Verify(
                m => m.Log("Color interpolation cache invalidated.", LogLevel.Trace),
                Times.Once);
        }

        [Fact]
        public void InvalidateCache_AfterInvalidation_ReturnsCorrectColor()
        {
            // Arrange — populate and then invalidate the cache
            var before = _service.GetColorForHealth(50f);
            _service.InvalidateCache();

            // Act
            var after = _service.GetColorForHealth(50f);

            // Assert — result should be the same even after cache cleared
            Assert.Equal(before, after);
        }

        // ──────────────────────────────────────────────
        // Clamping
        // ──────────────────────────────────────────────

        [Fact]
        public void GetColorForHealth_NegativeValue_ClampedToZero_ReturnsPoorColor()
        {
            // Act
            var result = _service.GetColorForHealth(-10f);

            // Assert
            Assert.Equal(PoorColor, result);
        }

        [Fact]
        public void GetColorForHealth_ValueAboveHundred_ClampedToHundred_ReturnsHealthyColor()
        {
            // Act
            var result = _service.GetColorForHealth(150f);

            // Assert
            Assert.Equal(HealthyColor, result);
        }

        [Fact]
        public void GetCategoryForHealth_NegativeValue_ClampedToZero_ReturnsPoor()
        {
            // Act
            var result = _service.GetCategoryForHealth(-25f);

            // Assert
            Assert.Equal(HealthCategory.Poor, result);
        }

        [Fact]
        public void GetCategoryForHealth_ValueAboveHundred_ClampedToHundred_ReturnsHealthy()
        {
            // Act
            var result = _service.GetCategoryForHealth(999f);

            // Assert
            Assert.Equal(HealthCategory.Healthy, result);
        }

        // ──────────────────────────────────────────────
        // NaN / Infinity Handling
        // ──────────────────────────────────────────────

        [Fact]
        public void GetColorForHealth_NaN_ReturnsUnknownColor()
        {
            // Act
            var result = _service.GetColorForHealth(float.NaN);

            // Assert
            Assert.Equal(UnknownColor, result);
        }

        [Fact]
        public void GetColorForHealth_PositiveInfinity_ReturnsUnknownColor()
        {
            // Act
            var result = _service.GetColorForHealth(float.PositiveInfinity);

            // Assert
            Assert.Equal(UnknownColor, result);
        }

        [Fact]
        public void GetColorForHealth_NegativeInfinity_ReturnsUnknownColor()
        {
            // Act
            var result = _service.GetColorForHealth(float.NegativeInfinity);

            // Assert
            Assert.Equal(UnknownColor, result);
        }

        [Fact]
        public void GetCategoryForHealth_NaN_ReturnsUnknown()
        {
            // Act
            var result = _service.GetCategoryForHealth(float.NaN);

            // Assert
            Assert.Equal(HealthCategory.Unknown, result);
        }

        [Fact]
        public void GetCategoryForHealth_PositiveInfinity_ReturnsUnknown()
        {
            // Act
            var result = _service.GetCategoryForHealth(float.PositiveInfinity);

            // Assert
            Assert.Equal(HealthCategory.Unknown, result);
        }
    }
}
