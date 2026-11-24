using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationServiceRegistrationTests
    {
        [Fact]
        public void Services_CanBeInstantiated()
        {
            var colorService = new Services.Visualization.ColorInterpolationService();
            Assert.NotNull(colorService);
        }
    }
}
