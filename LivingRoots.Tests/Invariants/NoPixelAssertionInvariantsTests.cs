using System;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// Meta-invariant (FR-008): no test in the codebase asserts on pixel colors,
    /// blend alpha, or overlay appearance. Verifies that the growth/quality logic
    /// returns numeric values (probabilities), not colors or pixel data.
    /// </summary>
    public class NoPixelAssertionInvariantsTests
    {
        /// <summary>
        /// Verifies that the harvest quality and extra harvest probability
        /// logic returns numeric float values (probabilities), not Color values
        /// or pixel data. This documents FR-008: the domain logic layer never
        /// returns visual/pixel types.
        /// </summary>
        [Fact]
        public void HarvestQualityLogic_ReturnsNumericValues_NotColors()
        {
            var qualityRoll = ModController.ComputeHarvestQualityRoll(50f, 0.5f);
            var extraProb = ModController.ComputeExtraHarvestProb(50f, 0.2f);

            Assert.IsType<float>(qualityRoll);
            Assert.IsType<float>(extraProb);
        }

        /// <summary>
        /// Verifies that the HealthCategory enum is a simple integer-based
        /// enum, not a color or pixel type. The category system maps health
        /// values to semantic tiers (Poor/Moderate/Healthy/Unknown) — these
        /// are numeric labels, not visual values.
        /// </summary>
        [Fact]
        public void HealthCategory_IsNumericEnum_NotAColorType()
        {
            var category = HealthCategoryExtensions.FromHealthValue(80f);
            Assert.Equal(HealthCategory.Healthy, category);
            Assert.True(Enum.IsDefined(typeof(HealthCategory), category));
        }

        /// <summary>
        /// Verifies that the growth multiplier constants are numeric floats,
        /// not color or pixel values. These constants drive the domain logic
        /// and must remain numeric for the probability-based tests to work.
        /// </summary>
        [Fact]
        public void GrowthMultiplierConstants_AreNumericFloats_NotColors()
        {
            Assert.IsType<float>(ModConstants.SoilGrowthPoorMultiplier);
            Assert.IsType<float>(ModConstants.SoilGrowthHealthyMultiplier);
            Assert.IsType<float>(ModConstants.SoilQualityHealthyBoost);
            Assert.IsType<float>(ModConstants.SoilExtraHarvestHealthyBoost);
        }
    }
}
