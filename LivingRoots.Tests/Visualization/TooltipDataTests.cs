using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the <see cref="TooltipData"/> domain model.
    /// Verifies property storage and the "Soil Health: {percentage}% ({category})" format string.
    /// </summary>
    public class TooltipDataTests
    {
        // ──────────────────────────────────────────────
        // Property Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void Text_ShouldStoreAndReturnValue()
        {
            // Arrange
            var tooltip = new TooltipData { Text = "Soil Health: 50% (Moderate)" };

            // Act
            var actual = tooltip.Text;

            // Assert
            Assert.Equal("Soil Health: 50% (Moderate)", actual);
        }

        [Fact]
        public void Position_ShouldStoreAndReturnVector2()
        {
            // Arrange
            var expected = new Vector2(128.5f, 256.0f);
            var tooltip = new TooltipData { Position = expected };

            // Act
            var actual = tooltip.Position;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BackgroundColor_ShouldStoreAndReturnColor()
        {
            // Arrange
            var expected = new Color(0, 0, 0, 200);
            var tooltip = new TooltipData { BackgroundColor = expected };

            // Act
            var actual = tooltip.BackgroundColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TextColor_ShouldStoreAndReturnColor()
        {
            // Arrange
            var expected = new Color(255, 255, 255, 255);
            var tooltip = new TooltipData { TextColor = expected };

            // Act
            var actual = tooltip.TextColor;

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // Format String Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void FormatString_0Percent_ShouldRenderPoor()
        {
            // Arrange
            const int percentage = 0;
            const string category = "Poor";
            var expected = "Soil Health: 0% (Poor)";

            // Act
            var actual = string.Format("Soil Health: {0}% ({1})", percentage, category);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatString_33Percent_ShouldRenderPoor()
        {
            // Arrange
            const int percentage = 33;
            const string category = "Poor";
            var expected = "Soil Health: 33% (Poor)";

            // Act
            var actual = string.Format("Soil Health: {0}% ({1})", percentage, category);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatString_50Percent_ShouldRenderModerate()
        {
            // Arrange
            const int percentage = 50;
            const string category = "Moderate";
            var expected = "Soil Health: 50% (Moderate)";

            // Act
            var actual = string.Format("Soil Health: {0}% ({1})", percentage, category);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatString_67Percent_ShouldRenderHealthy()
        {
            // Arrange
            const int percentage = 67;
            const string category = "Healthy";
            var expected = "Soil Health: 67% (Healthy)";

            // Act
            var actual = string.Format("Soil Health: {0}% ({1})", percentage, category);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatString_100Percent_ShouldRenderHealthy()
        {
            // Arrange
            const int percentage = 100;
            const string category = "Healthy";
            var expected = "Soil Health: 100% (Healthy)";

            // Act
            var actual = string.Format("Soil Health: {0}% ({1})", percentage, category);

            // Assert
            Assert.Equal(expected, actual);
        }

        // ──────────────────────────────────────────────
        // Integration: TooltipData with Format String
        // ──────────────────────────────────────────────

        [Fact]
        public void TooltipData_WithFormattedText_ShouldContainCorrectValues()
        {
            // Arrange
            const int percentage = 67;
            var category = HealthCategory.Healthy;

            // Act
            var tooltip = new TooltipData
            {
                Text = string.Format("Soil Health: {0}% ({1})", percentage, category),
                Position = new Vector2(100f, 200f),
                BackgroundColor = new Color(0, 0, 0, 180),
                TextColor = new Color(255, 255, 255, 255)
            };

            // Assert
            Assert.Equal("Soil Health: 67% (Healthy)", tooltip.Text);
            Assert.Equal(new Vector2(100f, 200f), tooltip.Position);
            Assert.Equal(new Color(0, 0, 0, 180), tooltip.BackgroundColor);
            Assert.Equal(new Color(255, 255, 255, 255), tooltip.TextColor);
        }
    }
}
