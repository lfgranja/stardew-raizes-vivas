using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class ModerateTierHarvestQualityTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Property(MaxTest = 200)]
        public Property ModerateRange_Health_MapsToModerate(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health))
            {
                return true.ToProperty();
            }

            var h = 31f + (Math.Abs(health) % 40f);

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(h, inputs);

            return (decision.Tier == HarvestTier.Moderate
                && !decision.ForceQualityZero
                && decision.QualityBoost == 0f
                && decision.ExtraHarvestBoost == 0f).ToProperty();
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void Decide_NonFinite_ReturnsUnknown(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);
            Assert.Equal(HarvestTier.Unknown, decision.Tier);
        }

        [Property]
        public Property NonFinite_ReturnsUnknown_NoModification(float health)
        {
            if (!float.IsNaN(health) && !float.IsInfinity(health))
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);

            return (decision.Tier == HarvestTier.Unknown && !decision.ForceQualityZero).ToProperty();
        }
    }
}
