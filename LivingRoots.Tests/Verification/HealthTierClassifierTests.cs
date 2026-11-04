using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class HealthTierClassifierTests
    {
        [Theory]
        [InlineData(null, "missing", "unknown")]
        [InlineData(null, "nan", "unknown")]
        [InlineData(double.NaN, "measured", "unknown")]
        [InlineData(0d, "measured", "nonParticipant")]
        [InlineData(-1d, "measured", "nonParticipant")]
        [InlineData(0.1d, "measured", "poor")]
        [InlineData(33.999d, "measured", "poor")]
        [InlineData(34d, "measured", "moderate")]
        [InlineData(66.999d, "measured", "moderate")]
        [InlineData(67d, "measured", "healthy")]
        [InlineData(100d, "measured", "healthy")]
        public void Classify_UsesTheFiveValueContractAndSpecBoundaries(double? health, string state, string expected)
        {
            Assert.Equal(expected, HealthTierClassifier.Classify(health, state));
        }

        [Fact]
        public void Classify_NullHealthWithMeasuredState_IsUnknownNotZero()
        {
            Assert.Equal("unknown", HealthTierClassifier.Classify(null, "measured"));
        }
    }
}
