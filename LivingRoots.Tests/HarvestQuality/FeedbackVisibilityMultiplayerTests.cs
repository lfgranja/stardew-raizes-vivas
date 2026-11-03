using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class FeedbackVisibilityMultiplayerTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void ResolveFeedback_SameDecisionAndFlags_ReturnsDeterministicResult()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(20.0f, inputs);

            var first = _service.ResolveFeedback(decision, false, false);
            var second = _service.ResolveFeedback(decision, false, false);

            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveFeedback_SameDecisionAndFlags_TrueFlags_ReturnsDeterministicResult()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var first = _service.ResolveFeedback(decision, true, true);
            var second = _service.ResolveFeedback(decision, true, true);

            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveFeedback_PoorFeedbackPresent_WhenPoorTierInScope()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5 };
            var decision = _service.Decide(20.0f, inputs);

            var flags = _service.ResolveFeedback(decision, false, false);

            Assert.True(flags.HasFlag(HarvestFeedbackFlags.Poor));
        }
    }
}
