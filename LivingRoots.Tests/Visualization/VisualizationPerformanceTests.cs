using System.Diagnostics;
using LivingRoots.Services.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationPerformanceTests
    {
        /// <summary>
        /// Verifies color interpolation stays inside the 16.67ms frame budget for 1,000 tiles.
        /// </summary>
        /// <remarks>
        /// The loop is warmed up first: the budget in FR-013 covers steady-state rendering during
        /// a frame, not one-time JIT of the interpolation path. Measuring cold made the result a
        /// function of JIT rather than of the code under test, and the test flaked between roughly
        /// 16ms and 35ms run to run.
        /// </remarks>
        [Fact]
        public void ColorInterpolationService_Performance_Under16ms()
        {
            var service = new ColorInterpolationService();

            for (int i = 0; i < 1000; i++)
            {
                service.GetColorForHealth(i % 101);
            }

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                service.GetColorForHealth(i % 101);
            }
            stopwatch.Stop();

            Assert.True(
                stopwatch.Elapsed.TotalMilliseconds < ModConstants.MaxRenderTimeMs,
                $"Rendering took {stopwatch.Elapsed.TotalMilliseconds}ms, budget is {ModConstants.MaxRenderTimeMs}ms");
        }
    }
}
