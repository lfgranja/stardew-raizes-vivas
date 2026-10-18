using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.ContractTests
{
    /// <summary>
    /// Contract tests for <see cref="ModController.ComputeHarvestQualityRoll"/> (FR-002).
    /// Validates the harvest quality roll contract: Poor forces 0, Moderate keeps
    /// vanilla, Healthy adds <see cref="ModConstants.SoilQualityHealthyBoost"/>.
    /// Compositional (A2): the roll adds to vanilla, never replaces it.
    /// </summary>
    public class HarvestQualityContractTests
    {
        [Theory]
        [InlineData(0f, 0.5f, 0f)]
        [InlineData(15f, 0.5f, 0f)]
        [InlineData(30f, 0.5f, 0f)]
        public void ComputeHarvestQualityRoll_PoorHealth_ReturnsZero(float health, float vanillaRoll, float expected)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(expected, result, 5);
        }

        [Theory]
        [InlineData(31f, 0.5f, 0.5f)]
        [InlineData(50f, 0.5f, 0.5f)]
        [InlineData(70f, 0.5f, 0.5f)]
        public void ComputeHarvestQualityRoll_ModerateHealth_ReturnsVanillaRoll(float health, float vanillaRoll, float expected)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(expected, result, 5);
        }

        [Theory]
        [InlineData(71f, 0.5f, 0.58f)]
        [InlineData(85f, 0.5f, 0.58f)]
        [InlineData(100f, 0.5f, 0.58f)]
        public void ComputeHarvestQualityRoll_HealthyHealth_ReturnsVanillaPlusBoost(float health, float vanillaRoll, float expected)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(expected, result, 5);
        }

        [Fact]
        public void ComputeHarvestQualityRoll_BoundaryValues_MatchSpec()
        {
            const float vanilla = 0.5f;
            var boost = ModConstants.SoilQualityHealthyBoost;

            // Boundary: 0 → 0 (Poor)
            Assert.Equal(0f, ModController.ComputeHarvestQualityRoll(0f, vanilla), 5);
            // Boundary: 30 → 0 (Poor)
            Assert.Equal(0f, ModController.ComputeHarvestQualityRoll(30f, vanilla), 5);
            // Boundary: 31 → vanilla (Moderate)
            Assert.Equal(vanilla, ModController.ComputeHarvestQualityRoll(31f, vanilla), 5);
            // Boundary: 70 → vanilla (Moderate)
            Assert.Equal(vanilla, ModController.ComputeHarvestQualityRoll(70f, vanilla), 5);
            // Boundary: 71 → vanilla + boost (Healthy)
            Assert.Equal(vanilla + boost, ModController.ComputeHarvestQualityRoll(71f, vanilla), 5);
            // Boundary: 100 → vanilla + boost (Healthy)
            Assert.Equal(vanilla + boost, ModController.ComputeHarvestQualityRoll(100f, vanilla), 5);
        }

        [Fact]
        public void ComputeHarvestQualityRoll_IsCompositional_AddsToVanillaNeverReplaces()
        {
            var vanillaRoll = 0.3f;
            var result = ModController.ComputeHarvestQualityRoll(100f, vanillaRoll);
            Assert.Equal(vanillaRoll + ModConstants.SoilQualityHealthyBoost, result, 5);
        }
    }
}
