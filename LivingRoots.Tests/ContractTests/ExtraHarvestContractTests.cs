using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.ContractTests
{
    /// <summary>
    /// Contract tests for spec 005 (FR-003): per-tile extra harvest probability.
    /// Verifies the <see cref="ModController.ComputeExtraHarvestProb"/> contract:
    /// Healthy tiles (health >= 71) get base + SoilExtraHarvestHealthyBoost;
    /// all other tiles keep base unchanged. The boost is additive, never multiplicative.
    /// </summary>
    public class ExtraHarvestContractTests
    {
        // ------------------------------------------------------------------
        // Healthy tier: base + 0.10
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(71f, 0.2f, 0.3f)]   // lower boundary of Healthy
        [InlineData(85f, 0.2f, 0.3f)]   // mid Healthy
        [InlineData(100f, 0.2f, 0.3f)]  // upper boundary of Healthy
        [InlineData(71f, 0.0f, 0.1f)]   // zero base
        [InlineData(100f, 0.5f, 0.6f)]  // high base
        [InlineData(71f, 0.9f, 1.0f)]   // base near ceiling
        public void ComputeExtraHarvestProb_HealthyTier_ReturnsBasePlusBoost(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(expected, result, 5);
        }

        // ------------------------------------------------------------------
        // Moderate tier: base unchanged
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(31f, 0.2f, 0.2f)]   // lower boundary of Moderate
        [InlineData(50f, 0.2f, 0.2f)]   // mid Moderate
        [InlineData(70f, 0.2f, 0.2f)]   // upper boundary of Moderate
        [InlineData(31f, 0.0f, 0.0f)]   // zero base
        [InlineData(70f, 0.5f, 0.5f)]   // high base
        public void ComputeExtraHarvestProb_ModerateTier_ReturnsBaseUnchanged(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(expected, result, 5);
        }

        // ------------------------------------------------------------------
        // Poor tier: base unchanged
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(0f, 0.2f, 0.2f)]    // lower boundary of Poor
        [InlineData(15f, 0.2f, 0.2f)]   // mid Poor
        [InlineData(30f, 0.2f, 0.2f)]   // upper boundary of Poor
        [InlineData(0f, 0.0f, 0.0f)]    // zero base
        [InlineData(30f, 0.5f, 0.5f)]   // high base
        public void ComputeExtraHarvestProb_PoorTier_ReturnsBaseUnchanged(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(expected, result, 5);
        }

        // ------------------------------------------------------------------
        // Boundary values: exact tier transitions
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(0f, 0.2f, 0.2f)]    // Poor lower boundary
        [InlineData(30f, 0.2f, 0.2f)]   // Poor upper boundary
        [InlineData(31f, 0.2f, 0.2f)]   // Moderate lower boundary
        [InlineData(70f, 0.2f, 0.2f)]   // Moderate upper boundary
        [InlineData(71f, 0.2f, 0.3f)]   // Healthy lower boundary
        [InlineData(100f, 0.2f, 0.3f)]  // Healthy upper boundary
        public void ComputeExtraHarvestProb_BoundaryValues_MatchExpectedTier(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(expected, result, 5);
        }

        // ------------------------------------------------------------------
        // Additive (not multiplicative) boost
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(71f, 0.0f, 0.1f)]   // 0.0 + 0.10 = 0.10
        [InlineData(71f, 0.1f, 0.2f)]   // 0.1 + 0.10 = 0.20
        [InlineData(71f, 0.5f, 0.6f)]   // 0.5 + 0.10 = 0.60
        [InlineData(100f, 0.3f, 0.4f)]  // 0.3 + 0.10 = 0.40
        public void ComputeExtraHarvestProb_BoostIsAdditive_NotMultiplicative(float health, float baseProb, float expected)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);

            // Additive: result = base + 0.10
            Assert.Equal(expected, result, 5);

            // NOT multiplicative: result != base * factor
            // If it were multiplicative with factor 1.5, 0.2 * 1.5 = 0.3 (same as additive here)
            // But 0.5 * 1.5 = 0.75 != 0.60, so this distinguishes additive from multiplicative
            if (baseProb > 0.01f)
            {
                float multiplicativeResult = baseProb * 1.5f;
                Assert.NotEqual(multiplicativeResult, result, 3);
            }
        }

        // ------------------------------------------------------------------
        // Boost magnitude is exactly SoilExtraHarvestHealthyBoost
        // ------------------------------------------------------------------

        [Fact]
        public void ComputeExtraHarvestProb_BoostMagnitude_MatchesModConstant()
        {
            const float baseProb = 0.25f;
            var result = ModController.ComputeExtraHarvestProb(85f, baseProb);
            var expectedBoost = ModConstants.SoilExtraHarvestHealthyBoost;

            Assert.Equal(baseProb + expectedBoost, result, 5);
        }

        // ------------------------------------------------------------------
        // Non-Healthy tiers never receive any boost
        // ------------------------------------------------------------------

        [Fact]
        public void ComputeExtraHarvestProb_NonHealthyTier_NeverBoosts()
        {
            var nonHealthyHealths = new[] { 0f, 15f, 30f, 31f, 50f, 70f };
            const float baseProb = 0.4f;

            foreach (var health in nonHealthyHealths)
            {
                var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                Assert.Equal(baseProb, result, 5);
            }
        }
    }
}
