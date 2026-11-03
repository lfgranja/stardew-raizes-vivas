using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HostAuthoritativeRollTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void Decide_SameInputs_ReturnsDeterministicDecision()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };

            var first = _service.Decide(75f, inputs);
            var second = _service.Decide(75f, inputs);

            Assert.Equal(first.Tier, second.Tier);
            Assert.Equal(first.QualityBoost, second.QualityBoost);
        }
    }
}
