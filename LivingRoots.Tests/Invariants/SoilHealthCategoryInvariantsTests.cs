using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// T017 — Property-based invariant tests for
    /// <see cref="HealthCategoryExtensions.FromHealthValue"/> (spec 005,
    /// governed by spec 009).
    /// Verifies the HealthCategory scale: Poor [0,34), Moderate [34,67),
    /// Healthy [67,100], with NaN/Infinity → Unknown and finite
    /// out-of-range values clamped to the boundary category.
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
                    if (health < 0f || health >= 34f) return true; // Outside Poor range [0,34) — skip
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
                    if (health < 34f || health >= 67f) return true; // Outside Moderate range [34,67)
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
                    if (health < 67f || health > 100f) return true; // Outside Healthy range [67,100]
                    return HealthCategoryExtensions.FromHealthValue(health) == HealthCategory.Healthy;
                });
        }

        [Theory]
        [InlineData(0f, HealthCategory.Poor)]
        [InlineData(15f, HealthCategory.Poor)]
        [InlineData(30f, HealthCategory.Poor)]
        [InlineData(31f, HealthCategory.Poor)]
        [InlineData(33f, HealthCategory.Poor)]
        [InlineData(34f, HealthCategory.Moderate)]
        [InlineData(50f, HealthCategory.Moderate)]
        [InlineData(66f, HealthCategory.Moderate)]
        [InlineData(67f, HealthCategory.Healthy)]
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
        [InlineData(-1f, HealthCategory.Poor)]
        [InlineData(-100f, HealthCategory.Poor)]
        [InlineData(101f, HealthCategory.Healthy)]
        [InlineData(1000f, HealthCategory.Healthy)]
        public void FromHealthValue_OutOfBounds_ClampsToBoundaryCategory(float health, HealthCategory expected)
        {
            var result = HealthCategoryExtensions.FromHealthValue(health);
            Assert.Equal(expected, result);
        }
    }
}
