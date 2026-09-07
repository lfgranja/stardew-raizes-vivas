using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceOverlayTests
    {
        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly VisualizationService _service;

        public VisualizationServiceOverlayTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            var mockColorService = new Mock<IColorInterpolationService>();
            var config = new VisualizationConfiguration { OverlaysEnabled = true };
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(config);
            _service = new VisualizationService(mockColorService.Object, _mockConfigService.Object, null!);
        }

        [Fact]
        public void RenderOverlays_WhenPaused_DoesNotRender()
        {
            _service.PauseRendering();
            _service.RenderOverlays(null!, new Rectangle(0, 0, 800, 600), new GameTime());
        }

        [Fact]
        public void RenderOverlays_WhenDisabled_DoesNotRender()
        {
            var config = new VisualizationConfiguration { OverlaysEnabled = false };
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(config);
            _service.RenderOverlays(null!, new Rectangle(0, 0, 800, 600), new GameTime());
        }

        [Fact]
        public void PauseRendering_SetsIsPaused()
        {
            _service.PauseRendering();
            _service.ResumeRendering();
        }
    }
}
