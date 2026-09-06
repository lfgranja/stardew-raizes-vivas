using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class TooltipDataTests
    {
        [Fact]
        public void Properties_Work()
        {
            var tooltip = new TooltipData
            {
                Text = "Soil Health: 75% (Healthy)",
                Position = new Vector2(100, 200),
                BackgroundColor = Color.Black,
                TextColor = Color.White
            };
            Assert.Equal("Soil Health: 75% (Healthy)", tooltip.Text);
            Assert.Equal(new Vector2(100, 200), tooltip.Position);
        }
    }
}
