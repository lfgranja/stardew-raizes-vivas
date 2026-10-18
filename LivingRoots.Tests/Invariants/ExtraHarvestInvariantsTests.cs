using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// Property-based invariant tests for spec 005 (FR-003): per-tile extra harvest
    /// probability. Verifies that <see cref="ModController.ComputeExtraHarvestProb"/>
    /// satisfies the following invariants for arbitrary inputs:
    /// - Healthy tier (health in [71,100]) always returns baseProb + 0.10
    /// - Non-Healthy tier (health in [0,70]) always returns baseProb unchanged
    /// - Result is always a valid probability in [0, 1.0] for baseProb in [0, 0.9]
    /// - Result is always >= baseProb (never reduces probability)
    /// - The boost is exactly 0.10 for the Healthy tier
    /// </summary>
    public class ExtraHarvestInvariantsTests
    {
        public ExtraHarvestInvariantsTests()
        {
            FsCheckFixture.Register();
        }

        // ------------------------------------------------------------------
        // Healthy tier: always base + 0.10
        // ------------------------------------------------------------------

        /// <summary>
        /// For all health in [71,100] and any baseProb, the result equals
        /// baseProb + SoilExtraHarvestHealthyBoost.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_HealthyTier_AlwaysReturnsBasePlusBoost()
        {
            return Prop.ForAll(
                Arb.From(HealthyHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    return Math.Abs(result - (baseProb + ModConstants.SoilExtraHarvestHealthyBoost)) < 0.0001f;
                });
        }

        // ------------------------------------------------------------------
        // Non-Healthy tier: always base unchanged
        // ------------------------------------------------------------------

        /// <summary>
        /// For all health in [0,70] and any baseProb, the result equals baseProb
        /// (no boost applied).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_NonHealthyTier_AlwaysReturnsBaseUnchanged()
        {
            return Prop.ForAll(
                Arb.From(NonHealthyHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    return Math.Abs(result - baseProb) < 0.0001f;
                });
        }

        // ------------------------------------------------------------------
        // Result is always a valid probability in [0, 1.0]
        // ------------------------------------------------------------------

        /// <summary>
        /// For all health in [0,100] and baseProb in [0, 0.9], the result is
        /// always in [0, 1.0] (valid probability range).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_ResultAlwaysValidProbability()
        {
            return Prop.ForAll(
                Arb.From(AllHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    return result >= 0f && result <= 1.0f;
                });
        }

        // ------------------------------------------------------------------
        // Result is always >= baseProb (never reduces probability)
        // ------------------------------------------------------------------

        /// <summary>
        /// For all health in [0,100] and any baseProb, the result is always
        /// greater than or equal to baseProb (the mechanism never reduces
        /// the extra harvest probability).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_ResultNeverLessThanBase()
        {
            return Prop.ForAll(
                Arb.From(AllHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    return result >= baseProb;
                });
        }

        // ------------------------------------------------------------------
        // Boost is exactly 0.10 for Healthy tier
        // ------------------------------------------------------------------

        /// <summary>
        /// For all health in [71,100] and any baseProb, the difference between
        /// the result and baseProb is exactly SoilExtraHarvestHealthyBoost (0.10).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_BoostIsExactlyModConstant()
        {
            return Prop.ForAll(
                Arb.From(HealthyHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    var boost = result - baseProb;
                    return Math.Abs(boost - ModConstants.SoilExtraHarvestHealthyBoost) < 0.0001f;
                });
        }

        // ------------------------------------------------------------------
        // Generators
        // ------------------------------------------------------------------

        /// <summary>Health values in [71, 100] (Healthy tier) with centesimal granularity.</summary>
        private static Gen<float> HealthyHealthValues()
        {
            return Gen.Choose(7100, 10000)
                .Select(h => h / 100f);
        }

        /// <summary>Health values in [0, 70] (Poor + Moderate tiers) with centesimal granularity.</summary>
        private static Gen<float> NonHealthyHealthValues()
        {
            return Gen.Choose(0, 7000)
                .Select(h => h / 100f);
        }

        /// <summary>Health values in [0, 100] (all tiers) with centesimal granularity.</summary>
        private static Gen<float> AllHealthValues()
        {
            return Gen.Choose(0, 10000)
                .Select(h => h / 100f);
        }

        /// <summary>Base probability values in [0, 0.9] with centesimal granularity.</summary>
        private static Gen<float> BaseProbValues()
        {
            return Gen.Choose(0, 90)
                .Select(i => i / 100f);
        }
    }
}
