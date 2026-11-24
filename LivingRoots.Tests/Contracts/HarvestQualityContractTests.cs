using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.Contracts
{
    public class HarvestQualityContractTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void SoilQualityHealthyBoost_IsAtLeast0_08()
        {
            Assert.True(ModConstants.SoilQualityHealthyBoost >= 0.08f);
        }

        [Fact]
        public void SoilExtraHarvestHealthyBoost_IsAtLeast0_10()
        {
            Assert.True(ModConstants.SoilExtraHarvestHealthyBoost >= 0.10f);
        }

        [Fact]
        public void Poor_ForcesQualityZero()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(30.0f, inputs);
            Assert.Equal(HarvestTier.Poor, decision.Tier);
            Assert.True(decision.ForceQualityZero);
            Assert.True(decision.IsInScope);
        }

        [Fact]
        public void Moderate_NoChange()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(50.0f, inputs);
            Assert.Equal(HarvestTier.Moderate, decision.Tier);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost);
            Assert.Equal(0.0f, decision.ExtraHarvestBoost);
        }

        [Fact]
        public void Healthy_AppliesBoost()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(75.0f, inputs);
            Assert.Equal(HarvestTier.Healthy, decision.Tier);
            Assert.False(decision.ForceQualityZero);
            Assert.True(decision.QualityBoost >= ModConstants.SoilQualityHealthyBoost);
            Assert.True(decision.ExtraHarvestBoost >= ModConstants.SoilExtraHarvestHealthyBoost);
        }
    }
}
