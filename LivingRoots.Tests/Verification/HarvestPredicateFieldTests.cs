using System.Collections.Generic;
using System.Text.Json;
using LivingRoots.Verification.Runner.Evaluation;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class HarvestPredicateFieldTests
    {
        [Theory]
        [InlineData("poorHarvestQuality", 0)]
        [InlineData("moderateHarvestQuality", 1)]
        [InlineData("healthyHarvestQuality", 1)]
        [InlineData("poorExtraCount", 0)]
        [InlineData("moderateExtraCount", 0)]
        [InlineData("healthyExtraCount", 1)]
        [InlineData("healthyExtraQuality", 0)]
        [InlineData("poorVanillaQuality", 1)]
        [InlineData("moderateVanillaQuality", 1)]
        [InlineData("healthyVanillaQuality", 0)]
        public void Evaluate_TreatsHarvestOutcomeFieldsAsNumeric(string field, int value)
        {
            var actual = new Dictionary<string, object?> { [field] = value };
            var predicate = JsonSerializer.Deserialize<JsonElement>(
                "{\"op\":\"cmp\",\"left\":\"" + field + "\",\"operator\":\"eq\",\"right\":" + value + "}");

            Assert.True(PredicateEvaluator.Evaluate(predicate, actual, out var reason), reason);
        }

        [Theory]
        [InlineData("healthyQualityRoll")]
        [InlineData("healthyExtraRoll")]
        public void Evaluate_TreatsHarvestRollFieldsAsNumericBetween(string field)
        {
            var actual = new Dictionary<string, object?> { [field] = 0.25 };
            var predicate = JsonSerializer.Deserialize<JsonElement>(
                "{\"op\":\"between\",\"field\":\"" + field + "\",\"min\":0.0,\"max\":1.0,\"tolerance\":0.0}");

            Assert.True(PredicateEvaluator.Evaluate(predicate, actual, out var reason), reason);
        }
    }
}
