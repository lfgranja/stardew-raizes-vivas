using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class PatternTypeTests
    {
        [Fact]
        public void EnumValues_AreExplicitlyDefined()
        {
            // Assert
            Assert.Equal(0, (int)PatternType.None);
            Assert.Equal(1, (int)PatternType.Stripes);
            Assert.Equal(2, (int)PatternType.Dots);
            Assert.Equal(3, (int)PatternType.Solid);
        }

        [Theory]
        [InlineData(HealthCategory.Poor, PatternType.Stripes)]
        [InlineData(HealthCategory.Moderate, PatternType.Dots)]
        [InlineData(HealthCategory.Healthy, PatternType.Solid)]
        [InlineData(HealthCategory.Unknown, PatternType.None)]
        public void FromHealthCategory_ReturnsCorrectPattern(HealthCategory category, PatternType expected)
        {
            // Act
            var result = PatternTypeExtensions.FromHealthCategory(category);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
