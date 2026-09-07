using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HealthCategoryTests
    {
        [Fact]
        public void EnumValues_AreExplicitlyDefined()
        {
            // Assert
            Assert.Equal(0, (int)HealthCategory.Poor);
            Assert.Equal(1, (int)HealthCategory.Moderate);
            Assert.Equal(2, (int)HealthCategory.Healthy);
            Assert.Equal(3, (int)HealthCategory.Unknown);
        }

        [Theory]
        [InlineData(0, HealthCategory.Poor)]
        [InlineData(33, HealthCategory.Poor)]
        [InlineData(34, HealthCategory.Moderate)]
        [InlineData(66, HealthCategory.Moderate)]
        [InlineData(67, HealthCategory.Healthy)]
        [InlineData(100, HealthCategory.Healthy)]
        public void FromHealthValue_ReturnsCorrectCategory(int healthValue, HealthCategory expected)
        {
            // Act
            var result = HealthCategoryExtensions.FromHealthValue(healthValue);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(-100)]
        [InlineData(1000)]
        public void FromHealthValue_OutOfRange_ReturnsUnknown(int healthValue)
        {
            // Act
            var result = HealthCategoryExtensions.FromHealthValue(healthValue);

            // Assert
            Assert.Equal(HealthCategory.Unknown, result);
        }

        [Fact]
        public void FromHealthValue_NaN_ReturnsUnknown()
        {
            // Act
            var result = HealthCategoryExtensions.FromHealthValue(float.NaN);

            // Assert
            Assert.Equal(HealthCategory.Unknown, result);
        }

        [Fact]
        public void FromHealthValue_Infinity_ReturnsUnknown()
        {
            // Act
            var resultPositive = HealthCategoryExtensions.FromHealthValue(float.PositiveInfinity);
            var resultNegative = HealthCategoryExtensions.FromHealthValue(float.NegativeInfinity);

            // Assert
            Assert.Equal(HealthCategory.Unknown, resultPositive);
            Assert.Equal(HealthCategory.Unknown, resultNegative);
        }
    }
}
