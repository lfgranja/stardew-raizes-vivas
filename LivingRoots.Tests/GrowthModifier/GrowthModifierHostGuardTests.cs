using System.Reflection;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierHostGuardTests
    {
        [Fact]
        public void OnDayEnding_Method_Exists()
        {
            var m = typeof(ModController).GetMethod("OnDayEnding", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(m);
        }
    }
}
