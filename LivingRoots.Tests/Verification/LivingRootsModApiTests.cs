using System;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class LivingRootsModApiTests
    {
        [Fact]
        public void Subscribe_Unsubscribe_Works()
        {
            var api = new LivingRootsModApi();
            EventHandler<SoilGrowthModifierCompletedEventArgs>? handler = (s, e) => { };
            api.SoilGrowthModifierCompleted += handler;
            api.SoilGrowthModifierCompleted -= handler;
            int calls = 0;
            api.SoilGrowthModifierCompleted += (s, e) => calls++;
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Idempotent_Dispose()
        {
            var api = new LivingRootsModApi();
            api.Dispose();
            api.Dispose(); // no throw
            Assert.True(true);
        }

        [Fact]
        public void No_Event_After_Dispose()
        {
            var api = new LivingRootsModApi();
            int calls = 0;
            api.SoilGrowthModifierCompleted += (s, e) => calls++;
            api.Dispose();
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            Assert.Equal(0, calls);
        }

        [Fact]
        public void Stable_Instance()
        {
            var a = new LivingRootsModApi();
            var b = a;
            Assert.Same(a, b);
        }
    }
}
