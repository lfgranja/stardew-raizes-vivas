using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class CropScopeHarvestTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Theory]
        [InlineData(0.0f)]
        [InlineData(30.0f)]
        [InlineData(50.0f)]
        [InlineData(75.0f)]
        [InlineData(100.0f)]
        [InlineData(-5.0f)]
        [InlineData(150.0f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void ForageCrop_IsOutOfScope_ForAnyHealth(float health)
        {
            var inputs = new HarvestQualityInputs
            {
                IsForageCrop = true,
                FarmingLevel = 10,
                FertilizerTier = 0
            };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(30.0f)]
        [InlineData(50.0f)]
        [InlineData(75.0f)]
        [InlineData(100.0f)]
        [InlineData(-5.0f)]
        [InlineData(150.0f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void FibreSeed_IsOutOfScope_ForAnyHealth(float health)
        {
            var inputs = new HarvestQualityInputs
            {
                IsFibreSeed = true,
                FarmingLevel = 10,
                FertilizerTier = 0
            };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(30.0f)]
        [InlineData(50.0f)]
        [InlineData(75.0f)]
        [InlineData(100.0f)]
        [InlineData(-5.0f)]
        [InlineData(150.0f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void QiFruit_IsOutOfScope_ForAnyHealth(float health)
        {
            var inputs = new HarvestQualityInputs
            {
                IsQiFruit = true,
                FarmingLevel = 10,
                FertilizerTier = 0
            };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.IsInScope);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void NonForageHealthy_AllFlagsFalse_IsInScope()
        {
            var inputs = new HarvestQualityInputs
            {
                IsForageCrop = false,
                IsFibreSeed = false,
                IsQiFruit = false,
                FarmingLevel = 10,
                FertilizerTier = 0
            };
            var decision = _service.Decide(80.0f, inputs);

            Assert.True(decision.IsInScope);
            Assert.NotEqual(HarvestTier.OutOfScope, decision.Tier);
        }
    }
}
