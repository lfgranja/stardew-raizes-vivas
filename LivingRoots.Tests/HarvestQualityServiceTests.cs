using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests
{
    public class HarvestQualityServiceTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void Decide_PoorHealth_ForcesQualityZero()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(30.0f, inputs);
            Assert.Equal(HarvestTier.Poor, decision.Tier);
            Assert.True(decision.ForceQualityZero);
            Assert.True(decision.IsInScope);
        }

        [Fact]
        public void Decide_ModerateHealth_NoModification()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(50.0f, inputs);
            Assert.Equal(HarvestTier.Moderate, decision.Tier);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
            Assert.Equal(0.0f, decision.ExtraHarvestBoost);
        }

        [Fact]
        public void Decide_HealthyHealth_AppliesBoosts()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(75.0f, inputs);
            Assert.Equal(HarvestTier.Healthy, decision.Tier);
            Assert.True(decision.QualityBoost >= 0.08f);
            Assert.True(decision.ExtraHarvestBoost >= 0.10f);
        }

        [Fact]
        public void Decide_MeasuredZero_Poor_ForcesZero()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 0 };
            var decision = _service.Decide(0.0f, inputs);
            Assert.Equal(HarvestTier.Poor, decision.Tier);
            Assert.True(decision.ForceQualityZero);
        }

        [Fact]
        public void Decide_NaN_Unknown_NoModification()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(float.NaN, inputs);
            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.ForceQualityZero);
            Assert.False(decision.IsInScope);
        }

        [Fact]
        public void Decide_Infinity_Unknown_NoModification()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(float.PositiveInfinity, inputs);
            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void Decide_ForageCrop_OutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsForageCrop = true, FarmingLevel = 10 };
            var decision = _service.Decide(75.0f, inputs);
            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void Decide_FibreSeed_OutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsFibreSeed = true, FarmingLevel = 10 };
            var decision = _service.Decide(75.0f, inputs);
            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
        }

        [Fact]
        public void Decide_QiFruit_OutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsQiFruit = true, FarmingLevel = 10 };
            var decision = _service.Decide(75.0f, inputs);
            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
        }

        [Fact]
        public void ResolveFeedback_PoorAlways_WhenInScope()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5 };
            var decision = _service.Decide(20.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.True(flags.HasFlag(HarvestFeedback.Poor));
        }

        [Fact]
        public void ResolveFeedback_LivingQuality_OnlyOnEnhancedOrExtra()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags1 = _service.ResolveFeedback(decision, true, false);
            Assert.True(flags1.HasFlag(HarvestFeedback.LivingQuality));
            var flags2 = _service.ResolveFeedback(decision, false, true);
            Assert.True(flags2.HasFlag(HarvestFeedback.LivingQuality));
            var flags3 = _service.ResolveFeedback(decision, false, false);
            Assert.False(flags3.HasFlag(HarvestFeedback.LivingQuality));
        }

        [Fact]
        public void ResolveFeedback_BothFlags_WhenBothOccur()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags = _service.ResolveFeedback(decision, true, true);
            Assert.True(flags.HasFlag(HarvestFeedback.LivingQuality));
            Assert.True(flags.HasFlag(HarvestFeedback.ExtraHarvest));
        }

        [Fact]
        public void ResolveFeedback_Moderate_NoFeedback()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(50.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedback.None, flags);
        }

        [Fact]
        public void BoostQualityProbability_Healthy_UsesBoost()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var boosted = _service.BoostQualityProbability(0.1f, decision);
            Assert.True(boosted >= 0.18f);
        }

        [Fact]
        public void BoostQualityProbability_Moderate_Unchanged()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(50.0f, inputs);
            var boosted = _service.BoostQualityProbability(0.1f, decision);
            Assert.Equal(0.1f, boosted);
        }

        [Property]
        public Property Poor_Always_Forces_Zero(float health)
        {
            var h = health % 31.0f;
            if (float.IsNaN(h) || float.IsInfinity(h) || h < 0) h = 0;
            if (h >= 31) h = 30;
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(h, inputs);
            return (decision.ForceQualityZero && decision.Tier == HarvestTier.Poor).ToProperty();
        }

        [Property]
        public Property Unknown_Returns_NoModification(float health)
        {
            // NaN or Infinity
            if (!float.IsNaN(health) && !float.IsInfinity(health)) return true.ToProperty();
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(health, inputs);
            return (decision.Tier == HarvestTier.Unknown && !decision.ForceQualityZero).ToProperty();
        }
    }
}
