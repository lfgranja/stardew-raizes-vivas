using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HealthyTierHarvestQualityTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Property]
        public Property Healthy_TierAndBoosts(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 71.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);

            return (decision.Tier == HarvestTier.Healthy
                && decision.QualityBoost >= 0.08f
                && decision.ExtraHarvestBoost >= 0.10f).ToProperty();
        }

        [Property]
        public Property Healthy_BoostQualityProbability_AddsBoost(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 71.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);
            var boosted = _service.BoostQualityProbability(0.1f, decision);

            return (boosted >= 0.1f + 0.08f).ToProperty();
        }

        [Fact]
        public void BoostQualityProbability_AddsBoost()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(80.0f, inputs);
            var boosted = _service.BoostQualityProbability(0.1f, decision);

            Assert.True(boosted >= 0.18f);
        }
    }
}
