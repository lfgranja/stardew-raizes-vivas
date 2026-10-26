using System.Reflection;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierDayEndingTests
    {
        [Fact]
        public void ApplySoilGrowthModifier_AcceptsLocationName()
        {
            var m = typeof(ModController).GetMethod("ApplySoilGrowthModifier", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(m);
            Assert.Single(m!.GetParameters());
            Assert.Equal(typeof(string), m.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void OnDayEnding_Exists()
        {
            var m = typeof(ModController).GetMethod("OnDayEnding", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(m);
        }
    }
}
