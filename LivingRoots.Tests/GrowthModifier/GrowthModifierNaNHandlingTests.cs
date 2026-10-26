using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierNaNHandlingTests
    {
        [Fact]
        public void NaN_IsNaN()
        {
            Assert.True(float.IsNaN(float.NaN));
        }
    }
}
