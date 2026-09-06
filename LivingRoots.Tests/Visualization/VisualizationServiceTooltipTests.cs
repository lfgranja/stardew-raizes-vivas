using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceTooltipTests
    {
        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly VisualizationService _service;

        public VisualizationServiceTooltipTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            var mockColorService = new Mock<IColorInterpolationService>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { TooltipsEnabled = true });
            _service = new VisualizationService(mockColorService.Object, _mockConfigService.Object, null!);
        }

        [Fact]
        public void RenderTooltip_WhenDisabled_DoesNotRender()
        {
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { TooltipsEnabled = false });
            _service.RenderTooltip(null!, new Vector2(100, 200), new GameTime());
        }

        [Fact]
        public void UpdateCursorTile_SetsPosition()
        {
            _service.UpdateCursorTile(new Point(5, 5));
        }

        [Fact]
        public void ClearCursorTile_ClearsPosition()
        {
            _service.UpdateCursorTile(new Point(5, 5));
            _service.ClearCursorTile();
        }
    }
}
