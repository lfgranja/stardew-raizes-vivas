using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class PatternTypeTests
    {
        // ──────────────────────────────────────────────
        // Enum Value Verification
        // ──────────────────────────────────────────────

        [Fact]
        public void None_ShouldHaveValueZero()
        {
            Assert.Equal(0, (int)PatternType.None);
        }

        [Fact]
        public void Stripes_ShouldHaveValueOne()
        {
            Assert.Equal(1, (int)PatternType.Stripes);
        }

        [Fact]
        public void Dots_ShouldHaveValueTwo()
        {
            Assert.Equal(2, (int)PatternType.Dots);
        }

        [Fact]
        public void Solid_ShouldHaveValueThree()
        {
            Assert.Equal(3, (int)PatternType.Solid);
        }

        // ──────────────────────────────────────────────
        // Helper: Map HealthCategory to PatternType
        // ──────────────────────────────────────────────

        private static PatternType MapToPattern(HealthCategory category)
        {
            return category switch
            {
                HealthCategory.Poor => PatternType.Stripes,
                HealthCategory.Moderate => PatternType.Dots,
                HealthCategory.Healthy => PatternType.Solid,
                HealthCategory.Unknown => PatternType.None,
                _ => PatternType.None
            };
        }

        // ──────────────────────────────────────────────
        // Accessibility Mapping Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToPattern_Poor_ShouldMapToStripes()
        {
            Assert.Equal(PatternType.Stripes, MapToPattern(HealthCategory.Poor));
        }

        [Fact]
        public void MapToPattern_Moderate_ShouldMapToDots()
        {
            Assert.Equal(PatternType.Dots, MapToPattern(HealthCategory.Moderate));
        }

        [Fact]
        public void MapToPattern_Healthy_ShouldMapToSolid()
        {
            Assert.Equal(PatternType.Solid, MapToPattern(HealthCategory.Healthy));
        }

        [Fact]
        public void MapToPattern_Unknown_ShouldMapToNone()
        {
            Assert.Equal(PatternType.None, MapToPattern(HealthCategory.Unknown));
        }
    }
}
