using System;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Concurrency tests for the visualization layer.
    /// Exercises the feedback and color-interpolation paths from many tasks at once and
    /// asserts that no exception escapes and no result is corrupted.
    /// </summary>
    public class VisualizationThreadSafetyTests
    {
        [Fact]
        public async Task ColorInterpolationService_ConcurrentAccess_NoDeadlocks()
        {
            // Arrange
            var service = new ColorInterpolationService();
            var tasks = new Task[100];

            // Act
            for (int i = 0; i < 100; i++)
            {
                int health = i;
                tasks[i] = Task.Run(() => service.GetColorForHealth(health));
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task HoeFeedbackRenderer_ConcurrentFeedbackCreationAndRender_NoExceptions()
        {
            // Arrange
            var mockConfigService = new Mock<IVisualizationConfigurationService>();
            var mockMonitor = new Mock<IMonitor>();
            var mockColorService = new Mock<IColorInterpolationService>();
            mockColorService
                .Setup(c => c.GetCategoryForHealth(It.IsAny<float>()))
                .Returns(HealthCategory.Moderate);

            var renderer = new HoeFeedbackRenderer(
                mockConfigService.Object,
                mockMonitor.Object,
                mockColorService.Object);

            const int taskCount = 64;
            var tasks = new Task<bool>[taskCount];

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() =>
                {
                    var gameTime = new GameTime(
                        TimeSpan.FromMilliseconds(index),
                        TimeSpan.FromMilliseconds(16));

                    HoeFeedback feedback = renderer.CreateFeedback(new Point(index, index), index, gameTime);
                    renderer.RenderFlash(null!, feedback, gameTime);

                    return renderer.IsFeedbackActive(feedback, gameTime);
                });
            }

            bool[] results = await Task.WhenAll(tasks);

            // Assert — every task completed and observed freshly created feedback as active.
            Assert.All(results, Assert.True);
        }
    }
}
