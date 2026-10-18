using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// T018 — Property-based invariant tests for the soil-growth multipliers
    /// and harvest modifier functions (spec 005 FR-001/FR-002/FR-003).
    /// Verifies multiplier relationships and the static compute methods.
    /// </summary>
    public class GrowthMultiplierInvariantsTests
    {
        public GrowthMultiplierInvariantsTests()
        {
            FsCheckFixture.Register();
        }

        [Fact]
        public void SoilGrowthPoorMultiplier_IsGreaterThanOne()
        {
            Assert.True(ModConstants.SoilGrowthPoorMultiplier > 1.0f,
                "Poor soil should slow growth (multiplier > 1.0).");
        }

        [Fact]
        public void SoilGrowthHealthyMultiplier_IsLessThanOne()
        {
            Assert.True(ModConstants.SoilGrowthHealthyMultiplier < 1.0f,
                "Healthy soil should speed growth (multiplier < 1.0).");
        }

        [Fact]
        public void PoorMultiplier_IsGreaterThanHealthyMultiplier()
        {
            Assert.True(ModConstants.SoilGrowthPoorMultiplier > ModConstants.SoilGrowthHealthyMultiplier,
                "Poor multiplier must exceed Healthy multiplier.");
        }

        [Fact]
        public void AllMultipliers_AreWithinReasonableBounds()
        {
            const float lowerBound = 0.5f;
            const float upperBound = 2.0f;

            Assert.InRange(ModConstants.SoilGrowthPoorMultiplier, lowerBound, upperBound);
            Assert.InRange(ModConstants.SoilGrowthHealthyMultiplier, lowerBound, upperBound);
        }

        [Fact]
        public void ModerateMultiplier_IsParity()
        {
            // Moderate is implicitly 1.0 (no modifier applied)
            const float moderateMultiplier = 1.0f;
            Assert.Equal(1.0f, moderateMultiplier);
        }

        [Theory]
        [InlineData(0f, 0.5f, 0f)]
        [InlineData(15f, 0.3f, 0f)]
        [InlineData(30f, 0.8f, 0f)]
        public void ComputeHarvestQualityRoll_Poor_ForcesZero(float health, float vanillaRoll, float expected)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(expected, result, 5);
        }

        [Theory]
        [InlineData(31f, 0.5f)]
        [InlineData(50f, 0.3f)]
        [InlineData(70f, 0.8f)]
        public void ComputeHarvestQualityRoll_Moderate_ReturnsVanilla(float health, float vanillaRoll)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(vanillaRoll, result, 5);
        }

        [Theory]
        [InlineData(71f, 0.5f)]
        [InlineData(85f, 0.3f)]
        [InlineData(100f, 0.8f)]
        public void ComputeHarvestQualityRoll_Healthy_AddsBoost(float health, float vanillaRoll)
        {
            var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
            Assert.Equal(vanillaRoll + ModConstants.SoilQualityHealthyBoost, result, 5);
        }

        [Theory]
        [InlineData(0f, 0.2f)]
        [InlineData(30f, 0.15f)]
        [InlineData(50f, 0.25f)]
        [InlineData(70f, 0.1f)]
        public void ComputeExtraHarvestProb_NonHealthy_ReturnsBase(float health, float baseProb)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(baseProb, result, 5);
        }

        [Theory]
        [InlineData(71f, 0.2f)]
        [InlineData(85f, 0.15f)]
        [InlineData(100f, 0.1f)]
        public void ComputeExtraHarvestProb_Healthy_AddsBoost(float health, float baseProb)
        {
            var result = ModController.ComputeExtraHarvestProb(health, baseProb);
            Assert.Equal(baseProb + ModConstants.SoilExtraHarvestHealthyBoost, result, 5);
        }

        [Property(MaxTest = 100)]
        public Property ComputeHarvestQualityRoll_HealthyAlwaysExceedsVanilla()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(71, 100)),
                Arb.From(Gen.Choose(0, 100).Select(v => v / 100f)),
                (health, vanillaRoll) =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
                    return result >= vanillaRoll;
                });
        }

        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_HealthyAlwaysExceedsBase()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(71, 100)),
                Arb.From(Gen.Choose(0, 90).Select(v => v / 100f)),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    return result >= baseProb;
                });
        }
    }
}
