using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class HealthCategoryTests
    {
        [Fact]
        public void EnumValues_AreCorrect()
        {
            Assert.Equal(0, (int)HealthCategory.Poor);
            Assert.Equal(1, (int)HealthCategory.Moderate);
            Assert.Equal(2, (int)HealthCategory.Healthy);
            Assert.Equal(3, (int)HealthCategory.Unknown);
        }
    }
}
