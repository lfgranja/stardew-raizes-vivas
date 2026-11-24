using System.Reflection;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierIdempotencyTests
    {
        [Fact]
        public void IdempotencyStorage_Field_Exists()
        {
            var f = typeof(ModController).GetField("_soilGrowthProcessedDays", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(f);
        }

        [Fact]
        public void IdempotencyLock_Field_Exists()
        {
            var f = typeof(ModController).GetField("_soilGrowthLock", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(f);
        }
    }
}
