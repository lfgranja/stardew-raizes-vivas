using System;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ModApiContractTests
    {
        [Fact]
        public void ApiVersion_Is_NonEmpty()
        {
            var api = new LivingRootsModApi();
            Assert.False(string.IsNullOrWhiteSpace(api.ApiVersion));
        }

        [Fact]
        public void Event_Raised_With_Payload()
        {
            var api = new LivingRootsModApi();
            SoilGrowthModifierCompletedEventArgs? received = null;
            api.SoilGrowthModifierCompleted += (s, e) => received = e;
            var args = new SoilGrowthModifierCompletedEventArgs
            {
                DayKey = "spring:1:1",
                IsMainPlayer = true,
                LocationsProcessed = 2,
                ElapsedMilliseconds = 5,
                Success = true
            };
            api.RaiseSoilGrowthModifierCompleted(args);
            Assert.NotNull(received);
            Assert.Equal("spring:1:1", received!.DayKey);
            Assert.True(received.IsMainPlayer);
            Assert.Equal(2, received.LocationsProcessed);
        }

        [Fact]
        public void Subscriber_Exception_Is_Isolated()
        {
            var api = new LivingRootsModApi();
            int calls = 0;
            api.SoilGrowthModifierCompleted += (s, e) => throw new InvalidOperationException("boom");
            api.SoilGrowthModifierCompleted += (s, e) => calls++;
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Dispose_Stops_Raise()
        {
            var api = new LivingRootsModApi();
            int calls = 0;
            api.SoilGrowthModifierCompleted += (s, e) => calls++;
            api.Dispose();
            api.RaiseSoilGrowthModifierCompleted(new SoilGrowthModifierCompletedEventArgs());
            Assert.Equal(0, calls);
        }
    }
}
