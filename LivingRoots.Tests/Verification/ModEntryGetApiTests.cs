using System;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ModEntryGetApiTests
    {
        [Fact]
        public void GetApi_ReturnsNonNullBeforeAndAfterEntry_SameInstance()
        {
            var modEntry = new ModEntry();
            // Before Entry
            var api1 = modEntry.GetApi();
            Assert.NotNull(api1);
            var api2 = modEntry.GetApi();
            Assert.Same(api1, api2);
            Assert.IsAssignableFrom<ILivingRootsModApi>(api1);
        }
    }
}
