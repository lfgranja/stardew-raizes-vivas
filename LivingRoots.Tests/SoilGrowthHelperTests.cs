using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests
{
    public class SoilGrowthHelperTests
    {
        [Fact]
        public void ModConstants_SoilGrowthValues_MatchSpec()
        {
            Assert.Equal(1.15f, ModConstants.SoilGrowthPoorMultiplier);
            Assert.Equal(0.85f, ModConstants.SoilGrowthHealthyMultiplier);
            Assert.Equal(0.08f, ModConstants.SoilQualityHealthyBoost);
            Assert.Equal(0.10f, ModConstants.SoilExtraHarvestHealthyBoost);
        }

        [Theory]
        [InlineData(0f, 0.5f, 0f)]   // Poor forces 0
        [InlineData(30f, 0.5f, 0f)]  // Poor forces 0
        [InlineData(50f, 0.5f, 0.5f)] // Moderate keeps vanilla
        [InlineData(70f, 0.5f, 0.5f)] // Moderate keeps vanilla
        [InlineData(71f, 0.5f, 0.58f)] // Healthy adds 0.08
        [InlineData(100f, 0.5f, 0.58f)] // Healthy adds 0.08
        public void ComputeHarvestQualityRoll_MatchesSpec(float health, float vanilla, float expected)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanilla);
            Assert.Equal(expected, result, 5);
        }

        [Theory]
        [InlineData(0f, 0.2f, 0.2f)]   // non-healthy keeps base
        [InlineData(70f, 0.2f, 0.2f)]  // non-healthy keeps base
        [InlineData(71f, 0.2f, 0.3f)]  // healthy adds 0.10
        [InlineData(100f, 0.2f, 0.3f)] // healthy adds 0.10
        public void ComputeExtraHarvestProb_MatchesSpec(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(expected, result, 5);
        }
    }
}
