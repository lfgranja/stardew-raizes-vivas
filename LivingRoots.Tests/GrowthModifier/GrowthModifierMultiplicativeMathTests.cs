using LivingRoots;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierMultiplicativeMathTests
    {
        [Fact]
        public void TierMultipliers_MatchHealthCategory()
        {
            Assert.Equal(1.15f, ModConstants.SoilGrowthPoorMultiplier);
            Assert.Equal(0.85f, ModConstants.SoilGrowthHealthyMultiplier);
        }

        [Theory]
        [InlineData(10, 1.15f)]
        [InlineData(50, 1.0f)]
        [InlineData(80, 0.85f)]
        public void HigherHealth_NeverSlower(int health, float expectedMultiplier)
        {
            float mult = health < 34 ? ModConstants.SoilGrowthPoorMultiplier
                       : health < 67 ? 1.0f
                       : ModConstants.SoilGrowthHealthyMultiplier;
            Assert.Equal(expectedMultiplier, mult);
        }
    }
}
