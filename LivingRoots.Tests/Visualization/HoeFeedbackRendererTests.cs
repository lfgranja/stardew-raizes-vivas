using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for <see cref="HoeFeedbackRenderer"/>.
    /// Verifies feedback creation (percentage formatting, FR-014 unknown formatting,
    /// durations sourced from <see cref="ModConstants"/>, start-time capture from the game
    /// clock), the active-window check, the null-guards on both render paths, and
    /// constructor null-validation.
    /// </summary>
    public class HoeFeedbackRendererTests
    {
        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly HoeFeedbackRenderer _renderer;

        public HoeFeedbackRendererTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockColorService
                .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
                .Returns(HealthCategory.Moderate);

            _renderer = new HoeFeedbackRenderer(
                _mockConfigService.Object,
                _mockMonitor.Object,
                _mockColorService.Object);
        }

        // ──────────────────────────────────────────────
        // Constructor validation
        // ──────────────────────────────────────────────

        [Fact]
        public void Constructor_NullConfigService_ThrowsArgumentNullException()
        {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new HoeFeedbackRenderer(null!, _mockMonitor.Object, _mockColorService.Object));

            // Assert
            Assert.Equal("configService", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMonitor_ThrowsArgumentNullException()
        {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new HoeFeedbackRenderer(_mockConfigService.Object, null!, _mockColorService.Object));

            // Assert
            Assert.Equal("monitor", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullColorInterpolationService_ThrowsArgumentNullException()
        {
            // Act
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new HoeFeedbackRenderer(_mockConfigService.Object, _mockMonitor.Object, null!));

            // Assert
            Assert.Equal("colorInterpolationService", exception.ParamName);
        }

        // ──────────────────────────────────────────────
        // CreateFeedback
        // ──────────────────────────────────────────────

        [Fact]
        public void CreateFeedback_KnownHealth_FormatsPercentageWithResolvedCategory()
        {
            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(12, 34), 50f, GameTimeAt(0));

            // Assert
            Assert.Equal("Soil Health: 50% (Moderate)", feedback.HealthText);
            Assert.Equal(HealthCategory.Moderate, feedback.Category);
            Assert.Equal(50f, feedback.HealthValue);
            Assert.Equal(new Point(12, 34), feedback.TilePosition);
        }

        [Theory]
        [InlineData(0f, "Soil Health: 0% (Moderate)")]
        [InlineData(33.4f, "Soil Health: 33% (Moderate)")]
        [InlineData(45.5f, "Soil Health: 46% (Moderate)")]
        [InlineData(66.6f, "Soil Health: 67% (Moderate)")]
        [InlineData(100f, "Soil Health: 100% (Moderate)")]
        [InlineData(-25f, "Soil Health: 0% (Moderate)")]
        [InlineData(250f, "Soil Health: 100% (Moderate)")]
        public void CreateFeedback_HealthValue_RoundsAndClampsPercentage(float healthValue, string expectedText)
        {
            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), healthValue, GameTimeAt(0));

            // Assert
            Assert.Equal(expectedText, feedback.HealthText);
        }

        [Fact]
        public void CreateFeedback_KnownHealth_UsesConfiguredDurations()
        {
            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(0));

            // Assert
            Assert.Equal((long)ModConstants.FlashDurationMs, feedback.FlashDuration);
            Assert.Equal((long)ModConstants.TextDurationMs, feedback.TextDuration);
        }

        [Fact]
        public void CreateFeedback_KnownHealth_ResolvesCategoryFromColorServiceOnce()
        {
            // Arrange
            _mockColorService.Setup(c => c.GetCategoryForHealth(50f)).Returns(HealthCategory.Healthy);

            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(0));

            // Assert
            Assert.Equal(HealthCategory.Healthy, feedback.Category);
            Assert.Equal("Soil Health: 50% (Healthy)", feedback.HealthText);
            _mockColorService.Verify(c => c.GetCategoryForHealth(50f), Times.Once);
        }

        [Fact]
        public void CreateFeedback_CapturesStartTimeFromGameClock()
        {
            // Arrange
            GameTime gameTime = GameTimeAt(1234.6);

            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(5, 6), 50f, gameTime);

            // Assert
            Assert.Equal(1234L, feedback.StartTime);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void CreateFeedback_NonFiniteHealth_FormatsUnknownWithoutPercentage(float healthValue)
        {
            // Arrange
            _mockColorService
                .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
                .Returns(HealthCategory.Unknown);

            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), healthValue, GameTimeAt(0));

            // Assert — FR-014: no percentage is rendered for an unreadable health value.
            Assert.Equal("Soil Health: Unknown", feedback.HealthText);
            Assert.DoesNotContain("%", feedback.HealthText);
            Assert.Equal(HealthCategory.Unknown, feedback.Category);
        }

        [Fact]
        public void CreateFeedback_UnknownCategory_FormatsUnknownWithoutPercentage()
        {
            // Arrange
            _mockColorService.Setup(c => c.GetCategoryForHealth(50f)).Returns(HealthCategory.Unknown);

            // Act
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(0));

            // Assert
            Assert.Equal("Soil Health: Unknown", feedback.HealthText);
            Assert.DoesNotContain("%", feedback.HealthText);
        }

        // ──────────────────────────────────────────────
        // IsFeedbackActive
        // ──────────────────────────────────────────────

        [Fact]
        public void IsFeedbackActive_BeforeMaxDuration_ReturnsTrue()
        {
            // Arrange
            long startMs = 1000;
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(startMs));

            // Act & Assert
            Assert.True(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs)));
            Assert.True(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs + ModConstants.FlashDurationMs)));
            Assert.True(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs + ModConstants.TextDurationMs - 1)));
        }

        [Fact]
        public void IsFeedbackActive_AtOrAfterMaxDuration_ReturnsFalse()
        {
            // Arrange
            long startMs = 1000;
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(startMs));

            // Act & Assert
            Assert.False(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs + ModConstants.TextDurationMs)));
            Assert.False(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs + ModConstants.TextDurationMs + 5000)));
        }

        [Fact]
        public void IsFeedbackActive_BeforeStartTime_ReturnsFalse()
        {
            // Arrange
            long startMs = 1000;
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(1, 1), 50f, GameTimeAt(startMs));

            // Act & Assert — a clock reading before the start time yields a negative elapsed value.
            Assert.False(_renderer.IsFeedbackActive(feedback, GameTimeAt(startMs - 1)));
        }

        [Fact]
        public void IsFeedbackActive_NullFeedback_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(_renderer.IsFeedbackActive(null!, GameTimeAt(0)));
        }

        // ──────────────────────────────────────────────
        // Render guards (no XNA graphics device available in unit tests)
        // ──────────────────────────────────────────────

        [Fact]
        public void RenderFlash_NullSpriteBatch_DoesNotThrowAndSkipsColorResolution()
        {
            // Arrange
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(2, 3), 50f, GameTimeAt(0));

            // Act
            Exception? exception = Record.Exception(() => _renderer.RenderFlash(null!, feedback, GameTimeAt(16)));

            // Assert
            Assert.Null(exception);
            _mockColorService.Verify(c => c.GetColorForHealth(It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void RenderFlash_NullFeedback_DoesNotThrow()
        {
            // Act — the guard runs before any texture is created, so no graphics device is touched.
            Exception? exception = Record.Exception(() => _renderer.RenderFlash(null!, null!, GameTimeAt(16)));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void RenderFloatingText_NullSpriteBatch_DoesNotThrowBeforeTouchingGameFont()
        {
            // Arrange
            HoeFeedback feedback = _renderer.CreateFeedback(new Point(2, 3), 50f, GameTimeAt(0));

            // Act — Game1.smallFont is unreachable in unit tests; reaching it would throw.
            Exception? exception = Record.Exception(() => _renderer.RenderFloatingText(null!, feedback, GameTimeAt(16)));

            // Assert
            Assert.Null(exception);
        }

        private static GameTime GameTimeAt(double totalGameTimeMs)
        {
            return new GameTime(TimeSpan.FromMilliseconds(totalGameTimeMs), TimeSpan.FromMilliseconds(16));
        }
    }
}
