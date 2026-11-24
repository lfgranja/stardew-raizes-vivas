using System.Reflection;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierDayEndingRollbackTests
    {
        [Fact]
        public void OnDayEnding_Handler_Field_Exists()
        {
            var f = typeof(ModController).GetField("_onDayEndingHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(f);
        }
    }
}
