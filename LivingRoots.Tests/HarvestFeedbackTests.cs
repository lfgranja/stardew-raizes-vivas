using System;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Integration tests for floating-text feedback when a harvest tier modifies
    /// the outcome (FR-002/FR-003, spec 005). When ApplyHarvestModifiers changes
    /// the quality roll or extra harvest probability, a floating-text message
    /// should be shown to the player. When the tier doesn't modify the outcome
    /// (Moderate), no floating-text should appear.
    /// </summary>
    /// <remarks>
    /// RED PHASE: ApplyHarvestModifiers currently calls TriggerHoeFeedback (wrong
    /// channel) instead of a floating-text method. The tests below verify the
    /// DESIRED behavior and will FAIL until the floating-text wiring (T116) lands.
    /// </remarks>
    public class HarvestFeedbackTests : IDisposable
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ICompostingBinService> _mockCompostingBinService;
        private readonly Mock<ISoilDecayService> _mockSoilDecayService;
        private readonly Mock<IVisualizationService> _mockVisualizationService;
        private readonly ModController _controller;

        public HarvestFeedbackTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();
            _mockCompostingBinService = new Mock<ICompostingBinService>();
            _mockSoilDecayService = new Mock<ISoilDecayService>();
            _mockVisualizationService = new Mock<IVisualizationService>();

            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));

            var mockEvents = new Mock<IModEvents>();
            var mockGameLoop = new Mock<IGameLoopEvents>();
            var mockInputEvents = new Mock<IInputEvents>();
            var mockDisplayEvents = new Mock<IDisplayEvents>();
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoop.Object);
            mockEvents.Setup(x => x.Input).Returns(mockInputEvents.Object);
            mockEvents.Setup(x => x.Display).Returns(mockDisplayEvents.Object);
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);

            _controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockCompostingBinService.Object,
                _mockSoilDecayService.Object,
                visualizationService: _mockVisualizationService.Object);
        }

        /// <summary>
        /// When ApplyHarvestModifiers modifies the outcome (Healthy boost),
        /// TriggerHoeFeedback should NOT be called — floating-text should be
        /// used instead. RED PHASE: fails because the current code calls
        /// TriggerHoeFeedback.
        /// </summary>
        [Fact]
        public void ApplyHarvestModifiers_WhenHealthyBoosts_DoesNotTriggerHoeFeedback()
        {
            // Arrange
            _mockSoilHealthService
                .Setup(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
                .Returns(80f); // Healthy

            // Act
            var modified = _controller.ApplyHarvestModifiers(
                "Farm", new Vector2(10, 10), 0.5f, 0.2f, out _, out _);

            // Assert
            Assert.True(modified, "Healthy health should modify the outcome");
            _mockVisualizationService.Verify(
                v => v.TriggerHoeFeedback(It.IsAny<Point>(), It.IsAny<float>()),
                Times.Never,
                "ApplyHarvestModifiers must not trigger hoe feedback when harvest outcome is modified");
        }

        /// <summary>
        /// When ApplyHarvestModifiers modifies the outcome (Poor forces 0),
        /// TriggerHoeFeedback should NOT be called — floating-text should be
        /// used instead. RED PHASE: fails because the current code calls
        /// TriggerHoeFeedback.
        /// </summary>
        [Fact]
        public void ApplyHarvestModifiers_WhenPoorForcesZero_DoesNotTriggerHoeFeedback()
        {
            // Arrange
            _mockSoilHealthService
                .Setup(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
                .Returns(15f); // Poor

            // Act
            var modified = _controller.ApplyHarvestModifiers(
                "Farm", new Vector2(10, 10), 0.5f, 0.2f, out _, out _);

            // Assert
            Assert.True(modified, "Poor health should modify the outcome");
            _mockVisualizationService.Verify(
                v => v.TriggerHoeFeedback(It.IsAny<Point>(), It.IsAny<float>()),
                Times.Never,
                "ApplyHarvestModifiers must not trigger hoe feedback when harvest outcome is modified");
        }

        /// <summary>
        /// When the tier doesn't modify the outcome (Moderate), no feedback
        /// of any kind should be triggered. GREEN: passes with current code.
        /// </summary>
        [Fact]
        public void ApplyHarvestModifiers_WhenModerate_DoesNotTriggerAnyFeedback()
        {
            // Arrange
            _mockSoilHealthService
                .Setup(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
                .Returns(50f); // Moderate

            // Act
            var modified = _controller.ApplyHarvestModifiers(
                "Farm", new Vector2(10, 10), 0.5f, 0.2f, out var qualityRoll, out var extraProb);

            // Assert
            Assert.False(modified, "Moderate health should not modify the outcome");
            Assert.Equal(0.5f, qualityRoll, 5);
            Assert.Equal(0.2f, extraProb, 5);
            _mockVisualizationService.Verify(
                v => v.TriggerHoeFeedback(It.IsAny<Point>(), It.IsAny<float>()),
                Times.Never,
                "No feedback should be triggered when outcome is not modified");
        }

        public void Dispose()
        {
            _controller.Dispose();
        }
    }
}
