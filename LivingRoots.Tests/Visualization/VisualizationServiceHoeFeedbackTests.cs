using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Moq;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceHoeFeedbackTests
    {
        private readonly Mock<IVisualizationConfigurationService> _mockConfigService;
        private readonly Mock<IColorInterpolationService> _mockColorService;
        private readonly VisualizationService _service;

        public VisualizationServiceHoeFeedbackTests()
        {
            _mockConfigService = new Mock<IVisualizationConfigurationService>();
            _mockColorService = new Mock<IColorInterpolationService>();
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { HoeFeedbackEnabled = true });
            _mockColorService.Setup(c => c.GetCategoryForHealth(It.IsAny<float>())).Returns(HealthCategory.Healthy);
            _service = new VisualizationService(_mockColorService.Object, _mockConfigService.Object, null!);
        }

        [Fact]
        public void TriggerHoeFeedback_AddsFeedback()
        {
            _service.TriggerHoeFeedback(new Point(3, 4), 75f);
        }

        [Fact]
        public void RenderHoeFeedback_WhenDisabled_DoesNotRender()
        {
            _mockConfigService.Setup(c => c.GetConfiguration()).Returns(new VisualizationConfiguration { HoeFeedbackEnabled = false });
            _service.RenderHoeFeedback(null!, new GameTime());
        }
    }
}
