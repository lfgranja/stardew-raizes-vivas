using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for <see cref="ColorInterpolationService"/>.
    /// Verifies linear RGB interpolation (FR-008), category boundaries,
    /// caching, cache invalidation, clamping, and NaN/Infinity handling.
    /// Base colors are never hardcoded here — they are read from
    /// <see cref="ModConstants"/> (earth-tone palette, spec clarification 127).
    /// </summary>
    public class ColorInterpolationServiceTests
    {
        private readonly ColorInterpolationService _service;

        public ColorInterpolationServiceTests()
        {
            _service = new ColorInterpolationService();
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
            Assert.Equal(ModConstants.PoorColor, result);
        }

        [Fact]
        public void GetColorForHealth_HealthHundred_ReturnsHealthyColor()
        {
            // Act
            var result = _service.GetColorForHealth(100f);

            // Assert
            Assert.Equal(ModConstants.HealthyColor, result);
        }

        [Fact]
        public void GetColorForHealth_HealthFifty_ReturnsMidpointBetweenModerateAndHealthy()
        {
            // Arrange
            // Earth-tone palette (spec clarification 127): Moderate #D97706 (217,119,6),
            // Healthy #15803D (21,128,61). health=50 falls in Moderate range [34, 67).
            // t = (50 - 34) / 33 = 16/33
            // r = Lerp(217, 21, 16/33)  = 217 + (21 - 217) * 16/33 = 217 - 95.03 = 121.97 -> 121
            // g = Lerp(119, 128, 16/33) = 119 + (128 - 119) * 16/33 = 119 + 4.36  = 123.36 -> 123
            // b = Lerp(6, 61, 16/33)    = 6 + (61 - 6) * 16/33    = 6 + 26.67   = 32.67  -> 32
            // a = opacity 1.0 * 255 = 255
            var expected = new Color(121, 123, 32, 255);

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
        public void InvalidateCache_ClearsCache_StaleColorsAreRecomputed()
        {
            // Arrange — populate the cache, then change the palette. SetCategoryColors
            // invalidates the cache, so the next lookup must not serve the stale color.
            var before = _service.GetColorForHealth(50f);
            _service.SetCategoryColors(Color.Black, Color.Black, Color.Black);

            // Act
            var after = _service.GetColorForHealth(50f);

            // Assert — the cached value was discarded, not replayed
            Assert.NotEqual(before, after);
            Assert.Equal(new Color(0, 0, 0, 255), after);
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
            Assert.Equal(ModConstants.PoorColor, result);
        }

        [Fact]
        public void GetColorForHealth_ValueAboveHundred_ClampedToHundred_ReturnsHealthyColor()
        {
            // Act
            var result = _service.GetColorForHealth(150f);

            // Assert
            Assert.Equal(ModConstants.HealthyColor, result);
        }

        [Fact]
        public void GetCategoryForHealth_NegativeValue_OutOfRange_ReturnsUnknown()
        {
            // Act
            var result = _service.GetCategoryForHealth(-25f);

            // Assert — out-of-range health is invalid data, not "Poor".
            // Matches HealthCategoryExtensions.FromHealthValue and
            // HealthCategoryTests.FromHealthValue_OutOfRange_ReturnsUnknown.
            Assert.Equal(HealthCategory.Unknown, result);
        }

        [Fact]
        public void GetCategoryForHealth_ValueAboveHundred_OutOfRange_ReturnsUnknown()
        {
            // Act
            var result = _service.GetCategoryForHealth(999f);

            // Assert — out-of-range health is invalid data, not "Healthy".
            // Matches HealthCategoryExtensions.FromHealthValue and
            // HealthCategoryTests.FromHealthValue_OutOfRange_ReturnsUnknown.
            Assert.Equal(HealthCategory.Unknown, result);
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
            Assert.Equal(ModConstants.UnknownColor, result);
        }

        [Fact]
        public void GetColorForHealth_PositiveInfinity_ReturnsUnknownColor()
        {
            // Act
            var result = _service.GetColorForHealth(float.PositiveInfinity);

            // Assert
            Assert.Equal(ModConstants.UnknownColor, result);
        }

        [Fact]
        public void GetColorForHealth_NegativeInfinity_ReturnsUnknownColor()
        {
            // Act
            var result = _service.GetColorForHealth(float.NegativeInfinity);

            // Assert
            Assert.Equal(ModConstants.UnknownColor, result);
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
