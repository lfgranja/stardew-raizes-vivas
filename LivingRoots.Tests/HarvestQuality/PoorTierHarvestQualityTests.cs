using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class PoorTierHarvestQualityTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Property]
        public Property PoorTier_HarvestQualityIsAlwaysZero(float health, int farmingLevel, int fertilizerTier)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0.0f || health >= 31.0f)
            {
                return true.ToProperty();
            }

            if (farmingLevel < 0 || farmingLevel > 10)
            {
                return true.ToProperty();
            }

            if (fertilizerTier < 0 || fertilizerTier > 3)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs
            {
                FarmingLevel = farmingLevel,
                FertilizerTier = fertilizerTier
            };
            var decision = _service.Decide(health, inputs);

            return (decision.Tier == HarvestTier.Poor
                && decision.ForceQualityZero
                && decision.IsInScope).ToProperty();
        }

        [Fact]
        public void ForageCrop_Excluded_IsOutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsForageCrop = true, FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(75.0f, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void MeasuredZero_IsPoor()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 0 };
            var decision = _service.Decide(0.0f, inputs);

            Assert.Equal(HarvestTier.Poor, decision.Tier);
            Assert.True(decision.ForceQualityZero);
            Assert.True(decision.IsInScope);
        }
    }
}
