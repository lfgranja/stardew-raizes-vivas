using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Temporary test to debug color values
    /// </summary>
    public class TestColorValues
    {
        [Fact]
        public void PrintColorValues()
        {
            var mockMonitor = new Mock<IMonitor>();
            var mockConfig = new Mock<IVisualizationConfig>();
            mockConfig.Setup(c => c.UseCustomColors).Returns(false);

            var colorMapper = new ColorMapper(mockMonitor.Object, mockConfig.Object);

            // Test poor health (10)
            Color poorColor = colorMapper.GetHealthColor(10f);
            Console.WriteLine($"Health 10: R={poorColor.R}, G={poorColor.G}, B={poorColor.B}");

            // Test moderate health (50)
            Color moderateColor = colorMapper.GetHealthColor(50f);
            Console.WriteLine($"Health 50: R={moderateColor.R}, G={moderateColor.G}, B={moderateColor.B}");

            // Test healthy health (85)
            Color healthyColor = colorMapper.GetHealthColor(85f);
            Console.WriteLine($"Health 85: R={healthyColor.R}, G={healthyColor.G}, B={healthyColor.B}");

            // Test boundaries
            Color zeroColor = colorMapper.GetHealthColor(0f);
            Console.WriteLine($"Health 0: R={zeroColor.R}, G={zeroColor.G}, B={zeroColor.B}");

            Color thirtyThreeColor = colorMapper.GetHealthColor(33f);
            Console.WriteLine($"Health 33: R={thirtyThreeColor.R}, G={thirtyThreeColor.G}, B={thirtyThreeColor.B}");

            Color sixtySixColor = colorMapper.GetHealthColor(66f);
            Console.WriteLine($"Health 66: R={sixtySixColor.R}, G={sixtySixColor.G}, B={sixtySixColor.B}");

            Color hundredColor = colorMapper.GetHealthColor(100f);
            Console.WriteLine($"Health 100: R={hundredColor.R}, G={hundredColor.G}, B={hundredColor.B}");

            // Assert that colors are valid (all channels should be between 0 and 255)
            Assert.InRange(poorColor.R, 0, 255);
            Assert.InRange(poorColor.G, 0, 255);
            Assert.InRange(poorColor.B, 0, 255);

            Assert.InRange(moderateColor.R, 0, 255);
            Assert.InRange(moderateColor.G, 0, 255);
            Assert.InRange(moderateColor.B, 0, 255);

            Assert.InRange(healthyColor.R, 0, 255);
            Assert.InRange(healthyColor.G, 0, 255);
            Assert.InRange(healthyColor.B, 0, 255);

            // Assert that colors are different for different health values
            Assert.NotEqual(poorColor, moderateColor);
            Assert.NotEqual(moderateColor, healthyColor);
            Assert.NotEqual(poorColor, healthyColor);
        }
    }
}
