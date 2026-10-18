using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// T017 — Property-based invariant tests for
    /// <see cref="HealthCategoryExtensions.FromHealthValue"/> (spec 005).
    /// Verifies the HealthCategory scale: Poor [0,30], Moderate [31,70],
    /// Healthy [71,100], with NaN/Infinity/out-of-bounds → Unknown.
    /// </summary>
    public class SoilHealthCategoryInvariantsTests
    {
        public SoilHealthCategoryInvariantsTests()
        {
            FsCheckFixture.Register();
        }

        [Property(MaxTest = 200)]
        public Property FromHealthValue_PoorRange_ReturnsPoor()
        {
            return Prop.ForAll(
                Arb.From(FsCheckFixture.HealthValues()),
                health =>
                {
                    if (health > 30f) return true; // Outside Poor range — skip
                    return HealthCategoryExtensions.FromHealthValue(health) == HealthCategory.Poor;
                });
        }

        [Property(MaxTest = 200)]
        public Property FromHealthValue_ModerateRange_ReturnsModerate()
        {
            return Prop.ForAll(
                Arb.From(FsCheckFixture.HealthValues()),
                health =>
                {
                    if (health < 31f || health > 70f) return true; // Outside Moderate range
                    return HealthCategoryExtensions.FromHealthValue(health) == HealthCategory.Moderate;
                });
        }

        [Property(MaxTest = 200)]
        public Property FromHealthValue_HealthyRange_ReturnsHealthy()
        {
            return Prop.ForAll(
                Arb.From(FsCheckFixture.HealthValues()),
                health =>
                {
                    if (health < 71f) return true; // Outside Healthy range
                    return HealthCategoryExtensions.FromHealthValue(health) == HealthCategory.Healthy;
                });
        }

        [Theory]
        [InlineData(0f, HealthCategory.Poor)]
        [InlineData(15f, HealthCategory.Poor)]
        [InlineData(30f, HealthCategory.Poor)]
        [InlineData(31f, HealthCategory.Moderate)]
        [InlineData(50f, HealthCategory.Moderate)]
        [InlineData(70f, HealthCategory.Moderate)]
        [InlineData(71f, HealthCategory.Healthy)]
        [InlineData(85f, HealthCategory.Healthy)]
        [InlineData(100f, HealthCategory.Healthy)]
        public void FromHealthValue_BoundaryValues_MapsCorrectly(float health, HealthCategory expected)
        {
            var result = HealthCategoryExtensions.FromHealthValue(health);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void FromHealthValue_NonFinite_ReturnsUnknown(float health)
        {
            var result = HealthCategoryExtensions.FromHealthValue(health);
            Assert.Equal(HealthCategory.Unknown, result);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(-100f)]
        [InlineData(101f)]
        [InlineData(1000f)]
        public void FromHealthValue_OutOfBounds_ReturnsUnknown(float health)
        {
            var result = HealthCategoryExtensions.FromHealthValue(health);
            Assert.Equal(HealthCategory.Unknown, result);
        }
    }
}
