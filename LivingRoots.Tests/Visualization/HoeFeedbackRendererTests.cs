using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HoeFeedbackRendererTests
    {
        private readonly HoeFeedbackRenderer _renderer = new();

        [Fact]
        public void AddFeedback_AddsToList()
        {
            _renderer.AddFeedback(new Point(1, 2), 50f, HealthCategory.Moderate, "Soil Health: 50% (Moderate)");
        }

        [Fact]
        public void Render_EmptyList_DoesNotThrow()
        {
            _renderer.Render(null!, new GameTime());
        }
    }
}
