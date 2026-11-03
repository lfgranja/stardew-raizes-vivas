using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class UnknownTierHarvestTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void Decide_NaN_ReturnsUnknown()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(float.NaN, inputs);

            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
        }

        [Fact]
        public void Decide_PositiveInfinity_ReturnsUnknown()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(float.PositiveInfinity, inputs);

            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
        }

        [Fact]
        public void Decide_NegativeInfinity_ReturnsUnknown()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(float.NegativeInfinity, inputs);

            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void Decide_NonFinite_ReturnsUnknown_WithAllFields(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
        }

        [Fact]
        public void Decide_NegativeFive_ReturnsPoor()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(-5.0f, inputs);

            Assert.Equal(HarvestTier.Poor, decision.Tier);
        }

        [Fact]
        public void Decide_OneHundredFifty_ReturnsHealthy()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(150.0f, inputs);

            Assert.Equal(HarvestTier.Healthy, decision.Tier);
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

            return (decision.Tier == HarvestTier.Unknown
                && !decision.IsInScope
                && !decision.ForceQualityZero
                && decision.QualityBoost == 0.0f).ToProperty();
        }

        [Property]
        public Property NegativeFinite_MapsToPoor(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health >= 0.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);

            return (decision.Tier == HarvestTier.Poor).ToProperty();
        }

        [Property]
        public Property HighFinite_MapsToHealthy(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health <= 100.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);

            return (decision.Tier == HarvestTier.Healthy).ToProperty();
        }
    }
}
