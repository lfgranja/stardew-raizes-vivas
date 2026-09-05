using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HealthCategoryTests
    {
        // ──────────────────────────────────────────────
        // Enum Value Verification
        // ──────────────────────────────────────────────

        [Fact]
        public void Poor_ShouldHaveValueZero()
        {
            Assert.Equal(0, (int)HealthCategory.Poor);
        }

        [Fact]
        public void Moderate_ShouldHaveValueOne()
        {
            Assert.Equal(1, (int)HealthCategory.Moderate);
        }

        [Fact]
        public void Healthy_ShouldHaveValueTwo()
        {
            Assert.Equal(2, (int)HealthCategory.Healthy);
        }

        [Fact]
        public void Unknown_ShouldHaveValueThree()
        {
            Assert.Equal(3, (int)HealthCategory.Unknown);
        }

        // ──────────────────────────────────────────────
        // Helper: Map health value to category
        // ──────────────────────────────────────────────

        private static HealthCategory MapToCategory(int health)
        {
            if (health < 0)
                return HealthCategory.Poor;
            if (health > 100)
                return HealthCategory.Healthy;

            // Half-open intervals: [0, 34) → Poor, [34, 67) → Moderate, [67, 100] → Healthy
            if (health < 34)
                return HealthCategory.Poor;
            if (health < 67)
                return HealthCategory.Moderate;
            return HealthCategory.Healthy;
        }

        // ──────────────────────────────────────────────
        // Boundary Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToCategory_33_ShouldBePoor()
        {
            Assert.Equal(HealthCategory.Poor, MapToCategory(33));
        }

        [Fact]
        public void MapToCategory_34_ShouldBeModerate()
        {
            Assert.Equal(HealthCategory.Moderate, MapToCategory(34));
        }

        [Fact]
        public void MapToCategory_66_ShouldBeModerate()
        {
            Assert.Equal(HealthCategory.Moderate, MapToCategory(66));
        }

        [Fact]
        public void MapToCategory_67_ShouldBeHealthy()
        {
            Assert.Equal(HealthCategory.Healthy, MapToCategory(67));
        }

        // ──────────────────────────────────────────────
        // Clamping Tests
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToCategory_NegativeValue_ShouldClampToPoor()
        {
            Assert.Equal(HealthCategory.Poor, MapToCategory(-1));
            Assert.Equal(HealthCategory.Poor, MapToCategory(-100));
        }

        [Fact]
        public void MapToCategory_ValueAbove100_ShouldClampToHealthy()
        {
            Assert.Equal(HealthCategory.Healthy, MapToCategory(101));
            Assert.Equal(HealthCategory.Healthy, MapToCategory(1000));
        }

        // ──────────────────────────────────────────────
        // Additional Boundary Edge Cases
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToCategory_Zero_ShouldBePoor()
        {
            Assert.Equal(HealthCategory.Poor, MapToCategory(0));
        }

        [Fact]
        public void MapToCategory_100_ShouldBeHealthy()
        {
            Assert.Equal(HealthCategory.Healthy, MapToCategory(100));
        }
    }
}
