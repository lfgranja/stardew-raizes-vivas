using System.Threading.Tasks;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationThreadSafetyTests
    {
        [Fact]
        public void ColorInterpolationService_ConcurrentAccess_NoDeadlocks()
        {
            var service = new ColorInterpolationService();
            var tasks = new Task[100];
            for (int i = 0; i < 100; i++)
            {
                int health = i;
                tasks[i] = Task.Run(() => service.GetColorForHealth(health));
            }
            Task.WaitAll(tasks);
        }

        [Fact]
        public void HoeFeedbackRenderer_ConcurrentRender_NoExceptions()
        {
            var renderer = new HoeFeedbackRenderer();
            renderer.AddFeedback(new Microsoft.Xna.Framework.Point(1, 1), 50f, HealthCategory.Moderate, "test");
            renderer.Render(null!, new Microsoft.Xna.Framework.GameTime());
        }
    }
}
