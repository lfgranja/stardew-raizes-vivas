using System.Reflection;
using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.GrowthModifier
{
    public class GrowthModifierAllLocationsTests
    {
        [Fact]
        public void LocationProvider_Field_Exists()
        {
            var f = typeof(ModController).GetField("_locationProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(f);
        }
    }
}
