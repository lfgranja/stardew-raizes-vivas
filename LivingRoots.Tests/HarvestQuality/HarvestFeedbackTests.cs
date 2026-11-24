using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HarvestFeedbackTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void ResolveFeedback_PoorTierInScope_ReturnsPoor()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5 };
            var decision = _service.Decide(20.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedbackFlags.Poor, flags);
        }

        [Fact]
        public void ResolveFeedback_HealthyTier_EnhancedQuality_ReturnsLivingQuality()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags = _service.ResolveFeedback(decision, true, false);
            Assert.True(flags.HasFlag(HarvestFeedbackFlags.LivingQuality));
        }

        [Fact]
        public void ResolveFeedback_HealthyTier_ExtraHarvest_ReturnsExtraHarvest()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, true);
            Assert.True(flags.HasFlag(HarvestFeedbackFlags.ExtraHarvest));
        }

        [Fact]
        public void ResolveFeedback_HealthyTier_Neither_ReturnsNone()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedbackFlags.None, flags);
        }

        [Fact]
        public void ResolveFeedback_ModerateTier_ReturnsNone()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(50.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedbackFlags.None, flags);
        }

        [Fact]
        public void ResolveFeedback_UnknownTier_ReturnsNone()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(float.NaN, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedbackFlags.None, flags);
        }

        [Fact]
        public void ResolveFeedback_OutOfScopeTier_ReturnsNone()
        {
            var inputs = new HarvestQualityInputs { IsForageCrop = true, FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);
            var flags = _service.ResolveFeedback(decision, false, false);
            Assert.Equal(HarvestFeedbackFlags.None, flags);
        }
    }
}
