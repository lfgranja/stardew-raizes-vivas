using FsCheck.Xunit;
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
        [InlineData(null, "measured", "unknown")]
        [InlineData(0d, "measured", "poor")]
        [InlineData(-1d, "measured", "poor")]
        [InlineData(0.1d, "measured", "poor")]
        [InlineData(30d, "measured", "poor")]
        [InlineData(30.999d, "measured", "poor")]
        [InlineData(31d, "measured", "moderate")]
        [InlineData(33d, "measured", "moderate")]
        [InlineData(33.999d, "measured", "moderate")]
        [InlineData(34d, "measured", "moderate")]
        [InlineData(66d, "measured", "moderate")]
        [InlineData(66.999d, "measured", "moderate")]
        [InlineData(67d, "measured", "moderate")]
        [InlineData(70d, "measured", "moderate")]
        [InlineData(70.999d, "measured", "moderate")]
        [InlineData(71d, "measured", "healthy")]
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

        [Fact]
        public void Classify_MeasuredZero_IsPoorNotNonParticipant()
        {
            Assert.Equal("poor", HealthTierClassifier.Classify(0d, "measured"));
        }

        [Property]
        public bool Classify_FiniteHealth_MapsToExpectedBand(double health)
        {
            if (double.IsNaN(health) || double.IsInfinity(health) || health < 0.0 || health > 100.0)
            {
                return true;
            }

            var expected = health < 31.0 ? "poor" : (health < 71.0 ? "moderate" : "healthy");
            return HealthTierClassifier.Classify(health, "measured") == expected;
        }
    }
}
