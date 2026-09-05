using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class TooltipDataTests
    {
        [Fact]
        public void Properties_CanBeSetAndGet()
        {
            // Arrange
            var tooltip = new TooltipData
            {
                Text = "Soil Health: 75% (Healthy)",
                Position = new Vector2(100f, 200f),
                BackgroundColor = Color.Black,
                TextColor = Color.White
            };

            // Assert
            Assert.Equal("Soil Health: 75% (Healthy)", tooltip.Text);
            Assert.Equal(new Vector2(100f, 200f), tooltip.Position);
            Assert.Equal(Color.Black, tooltip.BackgroundColor);
            Assert.Equal(Color.White, tooltip.TextColor);
        }

        [Theory]
        [InlineData(0, "Poor", "Soil Health: 0% (Poor)")]
        [InlineData(33, "Poor", "Soil Health: 33% (Poor)")]
        [InlineData(50, "Moderate", "Soil Health: 50% (Moderate)")]
        [InlineData(75, "Healthy", "Soil Health: 75% (Healthy)")]
        [InlineData(100, "Healthy", "Soil Health: 100% (Healthy)")]
        public void FormatString_RendersCorrectly(int percentage, string category, string expected)
        {
            // Act
            var result = TooltipData.Format(percentage, category);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_WithCategoryName_ProducesExpectedText()
        {
            // Act
            var result = TooltipData.Format(42, "Moderate");

            // Assert
            Assert.Equal("Soil Health: 42% (Moderate)", result);
        }

        [Fact]
        public void DefaultState_HasDefaultValues()
        {
            // Arrange
            var tooltip = new TooltipData();

            // Assert
            Assert.Null(tooltip.Text);
            Assert.Equal(default(Vector2), tooltip.Position);
            Assert.Equal(default(Color), tooltip.BackgroundColor);
            Assert.Equal(default(Color), tooltip.TextColor);
        }
    }
}
