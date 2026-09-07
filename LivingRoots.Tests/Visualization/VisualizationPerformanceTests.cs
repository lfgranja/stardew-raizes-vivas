using System.Diagnostics;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationPerformanceTests
    {
        [Fact]
        public void ColorInterpolationService_Performance_Under16ms()
        {
            var service = new Services.Visualization.ColorInterpolationService();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                service.GetColorForHealth(i % 101);
            }
            sw.Stop();
            Assert.True(sw.Elapsed.TotalMilliseconds < 16.67, $"Rendering took {sw.Elapsed.TotalMilliseconds}ms");
        }
    }
}
