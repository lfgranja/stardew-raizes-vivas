using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class PatternTypeTests
    {
        [Fact]
        public void EnumValues_AreCorrect()
        {
            Assert.Equal(0, (int)PatternType.None);
            Assert.Equal(1, (int)PatternType.Stripes);
            Assert.Equal(2, (int)PatternType.Dots);
            Assert.Equal(3, (int)PatternType.Solid);
        }
    }
}
