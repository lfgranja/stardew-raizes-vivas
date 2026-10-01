using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceHoeFeedbackTests
    {
        private const float HealthyValue = 75f;

        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly VisualizationService _service;

        public VisualizationServiceHoeFeedbackTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { HoeFeedbackEnabled = true });
            _mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>())).Returns(HealthCategory.Healthy);
            _service = new VisualizationService(_mockColorService.Object, _mockConfigService.Object, _mockMonitor.Object);
        }

        /// <summary>
        /// <c>TriggerHoeFeedback</c> resolves the health category through
        /// <c>IColorInterpolationService</c> before it does anything else, so the category
        /// delegation is what this test pins down.
        /// </summary>
        /// <remarks>
        /// Known unit-test limitation: the feedback's <c>StartTime</c> is read from the
        /// static <c>Game1.currentGameTime</c>, which is null outside the running game. The
        /// call therefore throws <see cref="NullReferenceException"/> before the feedback is
        /// queued and before the monitor log, and the "adds feedback" outcome itself can only
        /// be observed in-game (or with an injected clock, which the service does not expose).
        /// Asserting the game-clock failure is the honest boundary here.
        /// </remarks>
        [Fact]
        public void TriggerHoeFeedback_RequiresGameClock()
        {
            var exception = Record.Exception(() => _service.TriggerHoeFeedback(new Point(3, 4), HealthyValue));

            Assert.IsType<NullReferenceException>(exception);
            _mockColorService.Verify(c => c.GetCategoryForHealth(HealthyValue), Times.Once);
            _mockMonitor.Verify(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Never);
        }

        /// <summary>
        /// FR-007: with hoe feedback disabled the configuration is read once and the frame
        /// returns before the feedback list is inspected or drawn.
        /// </summary>
        [Fact]
        public void RenderHoeFeedback_WhenDisabled_DoesNotRender()
        {
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { HoeFeedbackEnabled = false });

            var exception = Record.Exception(() => _service.RenderHoeFeedback(null!, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Once);
            _mockMonitor.Verify(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Never);
        }

        /// <summary>
        /// A paused service returns before it even reads the configuration, so the feedback
        /// list is never locked, expired entries are never pruned, and nothing is drawn.
        /// </summary>
        [Fact]
        public void RenderHoeFeedback_WhenPaused_DoesNotRender()
        {
            _service.PauseRendering();

            var exception = Record.Exception(() => _service.RenderHoeFeedback(null!, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Never);
        }

        /// <summary>
        /// With feedback enabled but nothing triggered, the frame reads the configuration
        /// and finds an empty feedback list. This separates the "no active feedback" exit
        /// from the disabled-toggle exit above, which both produce a silent frame.
        /// </summary>
        [Fact]
        public void RenderHoeFeedback_WithNoActiveFeedback_DoesNotRender()
        {
            var exception = Record.Exception(() => _service.RenderHoeFeedback(null!, new GameTime()));

            Assert.Null(exception);
            _mockConfigService.Verify(c => c.GetConfiguration(), Times.Once);
        }
    }
}
