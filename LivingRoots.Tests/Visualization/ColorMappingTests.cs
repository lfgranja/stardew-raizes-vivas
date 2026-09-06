using LivingRoots.Domain.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ColorMappingTests
    {
        [Fact]
        public void Properties_Work()
        {
            var mapping = new ColorMapping
            {
                Category = HealthCategory.Moderate,
                BaseColor = Color.Yellow,
                InterpolatedColor = Color.Orange,
                HealthValue = 50f
            };
            Assert.Equal(HealthCategory.Moderate, mapping.Category);
            Assert.Equal(Color.Yellow, mapping.BaseColor);
            Assert.Equal(50f, mapping.HealthValue);
        }
    }
}
