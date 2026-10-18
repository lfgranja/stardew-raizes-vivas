using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// Property-based invariant tests for <see cref="ModController.ComputeHarvestQualityRoll"/>
    /// (FR-002, spec 005). Uses FsCheck to verify the tier contract across the full
    /// health range: Poor [0,30] forces 0, Moderate [31,70] keeps vanilla, Healthy
    /// [71,100] adds <see cref="ModConstants.SoilQualityHealthyBoost"/>. The result is
    /// always a valid probability in [0, 1] and never negative.
    /// </summary>
    public class HarvestQualityInvariantsTests
    {
        public HarvestQualityInvariantsTests()
        {
            FsCheckFixture.Register();
        }

        /// <summary>
        /// Invariant: for all health in [0, 30], ComputeHarvestQualityRoll returns 0.
        /// </summary>
        [Property(MaxTest = 200)]
        public Property ComputeHarvestQualityRoll_PoorHealth_AlwaysReturnsZero()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 30)),
                health =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, 0.5f);
                    return result == 0f;
                });
        }

        /// <summary>
        /// Invariant: for all health in [31, 70], ComputeHarvestQualityRoll returns
        /// the vanilla roll unchanged.
        /// </summary>
        [Property(MaxTest = 200)]
        public Property ComputeHarvestQualityRoll_ModerateHealth_AlwaysReturnsVanilla()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(31, 70)),
                Arb.From(Gen.Choose(0, 100).Select(v => v / 100f)),
                (health, vanillaRoll) =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
                    return result == vanillaRoll;
                });
        }

        /// <summary>
        /// Invariant: for all health in [71, 100], ComputeHarvestQualityRoll returns
        /// vanilla roll + SoilQualityHealthyBoost.
        /// </summary>
        [Property(MaxTest = 200)]
        public Property ComputeHarvestQualityRoll_HealthyHealth_AlwaysReturnsVanillaPlusBoost()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(71, 100)),
                Arb.From(Gen.Choose(0, 100).Select(v => v / 100f)),
                (health, vanillaRoll) =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
                    var expected = Math.Min(vanillaRoll + ModConstants.SoilQualityHealthyBoost, 1.0f);
                    return Math.Abs(result - expected) < 0.0001f;
                });
        }

        /// <summary>
        /// Invariant: the result is always in [0, 1.0] (valid probability) for
        /// all valid inputs (health in [0, 100], vanilla roll in [0, 1]).
        /// </summary>
        [Property(MaxTest = 200)]
        public Property ComputeHarvestQualityRoll_ResultIsAlwaysValidProbability()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 100)),
                Arb.From(Gen.Choose(0, 100).Select(v => v / 100f)),
                (health, vanillaRoll) =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
                    return result >= 0f && result <= 1.0f;
                });
        }

        /// <summary>
        /// Invariant: for all valid inputs, the result is never negative.
        /// </summary>
        [Property(MaxTest = 200)]
        public Property ComputeHarvestQualityRoll_ResultIsNeverNegative()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 100)),
                Arb.From(Gen.Choose(0, 100).Select(v => v / 100f)),
                (health, vanillaRoll) =>
                {
                    var result = ModController.ComputeHarvestQualityRoll(health, vanillaRoll);
                    return result >= 0f;
                });
        }
    }
}
