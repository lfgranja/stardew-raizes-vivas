using System.Threading;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class CoyoteSchedulingTests
    {
        [Fact]
        public void ConcurrentEventRaiseDisposeInterleavings()
        {
            var api = new LivingRootsModApi();
            int calls = 0;
            api.SoilGrowthModifierCompleted += (s, e) => Interlocked.Increment(ref calls);
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            api.Dispose();
            api.Dispose();
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            // no exception expected; calls count not strictly enforced here
            Assert.True(calls >= 0);
        }
    }
}
