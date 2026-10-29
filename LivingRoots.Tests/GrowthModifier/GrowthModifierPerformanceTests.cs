using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierPerformanceTests
    {
        [Fact]
        public void GetChunkStartIndices_CorrectCount()
        {
            var starts = ModController.GetChunkStartIndices(40000, 1000);
            Assert.Equal(40, starts.Count);
        }

        [Fact]
        public void GetChunkStartIndices_ThreeParam_Overload()
        {
            var starts = ModController.GetChunkStartIndices(10000, 1000, 61);
            Assert.Equal(10, starts.Count);
        }
    }
}
