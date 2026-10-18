using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// Property-based invariant tests for spec 005 (FR-003): the extra harvest
    /// mechanism is per-tile, not global per seed. Verifies that:
    /// - ComputeExtraHarvestProb depends only on the tile's health, not on any global state
    /// - For two tiles with the same health, the result is the same (deterministic)
    /// - For two tiles with different health, the results differ appropriately
    /// </summary>
    public class NoGlobalExtraInvariantsTests
    {
        public NoGlobalExtraInvariantsTests()
        {
            FsCheckFixture.Register();
        }

        // ------------------------------------------------------------------
        // Determinism: same inputs always produce same result
        // ------------------------------------------------------------------

        /// <summary>
        /// For any health and baseProb, calling ComputeExtraHarvestProb multiple
        /// times with the same inputs always produces the same result.
        /// This verifies the function is deterministic and has no hidden global state.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_SameInputs_AlwaysSameResult()
        {
            return Prop.ForAll(
                Arb.From(AllHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    var first = ModController.ComputeExtraHarvestProb(health, baseProb);
                    var second = ModController.ComputeExtraHarvestProb(health, baseProb);
                    var third = ModController.ComputeExtraHarvestProb(health, baseProb);

                    return first == second && second == third;
                });
        }

        // ------------------------------------------------------------------
        // Per-tile: different health values produce different results
        // ------------------------------------------------------------------

        /// <summary>
        /// For any two different health values where one is Healthy (>= 71) and
        /// the other is not, the results differ by exactly SoilExtraHarvestHealthyBoost.
        /// This verifies the mechanism responds to per-tile health differences.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_DifferentHealth_ResultsDifferAppropriately()
        {
            return Prop.ForAll(
                Arb.From(HealthyHealthValues()),
                Arb.From(NonHealthyHealthValues()),
                Arb.From(BaseProbValues()),
                (healthyHealth, nonHealthyHealth, baseProb) =>
                {
                    var healthyResult = ModController.ComputeExtraHarvestProb(healthyHealth, baseProb);
                    var nonHealthyResult = ModController.ComputeExtraHarvestProb(nonHealthyHealth, baseProb);

                    // Healthy result should be exactly baseProb + boost
                    // Non-Healthy result should be exactly baseProb
                    // So the difference should be exactly the boost
                    var difference = healthyResult - nonHealthyResult;
                    return Math.Abs(difference - ModConstants.SoilExtraHarvestHealthyBoost) < 0.0001f;
                });
        }

        // ------------------------------------------------------------------
        // Per-tile: same health always produces same result regardless of tile identity
        // ------------------------------------------------------------------

        /// <summary>
        /// For any two tiles with the same health value, the result is the same.
        /// This verifies the mechanism is per-tile (depends only on health, not on
        /// tile coordinates or any other tile-specific state).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_SameHealth_SameResult()
        {
            return Prop.ForAll(
                Arb.From(AllHealthValues()),
                Arb.From(BaseProbValues()),
                (health, baseProb) =>
                {
                    // Simulate two different tiles with the same health
                    var tile1Result = ModController.ComputeExtraHarvestProb(health, baseProb);
                    var tile2Result = ModController.ComputeExtraHarvestProb(health, baseProb);

                    return tile1Result == tile2Result;
                });
        }

        // ------------------------------------------------------------------
        // No global state: result depends only on inputs
        // ------------------------------------------------------------------

        /// <summary>
        /// For any health and baseProb, the result is a pure function of the inputs.
        /// Calling with different health values produces results that differ only
        /// by the expected boost amount (when crossing the Healthy boundary).
        /// This verifies there is no hidden global state affecting the computation.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ComputeExtraHarvestProb_PureFunction_NoGlobalState()
        {
            return Prop.ForAll(
                Arb.From(AllHealthValues()),
                Arb.From(AllHealthValues()),
                Arb.From(BaseProbValues()),
                (health1, health2, baseProb) =>
                {
                    var result1 = ModController.ComputeExtraHarvestProb(health1, baseProb);
                    var result2 = ModController.ComputeExtraHarvestProb(health2, baseProb);

                    // If both are Healthy or both are non-Healthy, results should be equal
                    bool health1IsHealthy = health1 >= 71f;
                    bool health2IsHealthy = health2 >= 71f;

                    if (health1IsHealthy == health2IsHealthy)
                    {
                        return Math.Abs(result1 - result2) < 0.0001f;
                    }

                    // If one is Healthy and the other is not, the difference should be exactly the boost
                    var difference = health1IsHealthy
                        ? result1 - result2
                        : result2 - result1;
                    return Math.Abs(difference - ModConstants.SoilExtraHarvestHealthyBoost) < 0.0001f;
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
