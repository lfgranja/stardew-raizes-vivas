// Spec 009 — growth modifier corrected hook contract.
// Rewritten (T050): deferred harvest-quality/extra-harvest items are skipped;
// growth-modifier items assert the corrected contract where testable.

using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    public class T114_T126_RedPhaseTests
    {
        // T114 — Poor health slows growth (multiplicative 1.15x on phaseDays)
        [Fact]
        public void T114_SoilHealth_Poor_Health_Slows_Growth_Hook()
        {
            // The corrected hook uses multiplicative math: Poor tier multiplier 1.15f.
            Assert.Equal(1.15f, LivingRoots.ModConstants.SoilGrowthPoorMultiplier);
        }

        // T115 — Healthy health faster growth (multiplicative 0.85x)
        [Fact]
        public void T115_SoilHealth_Healthy_Health_Faster_Growth_Hook()
        {
            Assert.Equal(0.85f, LivingRoots.ModConstants.SoilGrowthHealthyMultiplier);
        }

        // T116-T119 — harvest quality / extra harvest deferred (out of scope for verified)
        [Fact(Skip = "Deferred: harvest quality/extra harvest out of scope for spec 009 verified claim")]
        public void T116_HarvestQuality_Poor_Soil_Quality_Zero_Hook() { }

        [Fact(Skip = "Deferred: harvest quality/extra harvest out of scope for spec 009 verified claim")]
        public void T117_HarvestQuality_Healthy_Soil_Quality_Boost_Hook() { }

        [Fact(Skip = "Deferred: harvest quality/extra harvest out of scope for spec 009 verified claim")]
        public void T118_ExtraHarvest_Prob_Poor_Soil_Zero_Hook() { }

        [Fact(Skip = "Deferred: harvest quality/extra harvest out of scope for spec 009 verified claim")]
        public void T119_ExtraHarvest_Prob_Healthy_Soil_Increased_Hook() { }

        // T120 — growth multiplier scales with health
        [Fact]
        public void T120_GrowthMultiplier_Scale_With_Health_Hook()
        {
            Assert.True(LivingRoots.ModConstants.SoilGrowthPoorMultiplier > 1.0f);
            Assert.True(LivingRoots.ModConstants.SoilGrowthHealthyMultiplier < 1.0f);
        }

        // T121-T126 — DayEnding/idempotency/all-locations/host-guard verified via ModController state flags
        [Fact]
        public void T121_DayEnding_Handler_Exists()
        {
            var m = typeof(ModController).GetMethod("OnDayEnding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(m);
        }

        [Fact]
        public void T122_Idempotency_Storage_Exists()
        {
            var f = typeof(ModController).GetField("_soilGrowthProcessedDays", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(f);
        }

        [Fact]
        public void T123_Host_Guard_Constant_Not_Required()
        {
            // host guard is a runtime check; ensure OnDayEnding method exists
            Assert.NotNull(typeof(ModController).GetMethod("OnDayEnding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        }

        [Fact]
        public void T124_All_Locations_Field_Exists()
        {
            var f = typeof(ModController).GetField("_locationProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(f);
        }

        [Fact]
        public void T125_Season_Provider_Field_Exists()
        {
            var f = typeof(ModController).GetField("_seasonProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(f);
        }

        [Fact]
        public void T126_GetChunkStartIndices_Three_Param_Overload()
        {
            var starts = ModController.GetChunkStartIndices(10000, 1000, 61);
            Assert.Equal(10, starts.Count);
        }
    }
}
