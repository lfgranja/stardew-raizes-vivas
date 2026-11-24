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

        [Fact]
        public void ApplyMulch_Unbound_ReturnsFalse()
        {
            var api = new LivingRootsModApi();

            Assert.False(api.ApplyMulch("Farm", 10, 10));
        }

        [Fact]
        public void ApplyMulch_BlankLocation_ReturnsFalseWithoutInvokingBoundDelegate()
        {
            var api = new LivingRootsModApi();
            var calls = 0;
            api.BindMulchApplication((location, x, y) =>
            {
                calls++;
                return true;
            });

            Assert.False(api.ApplyMulch("   ", 10, 10));
            Assert.Equal(0, calls);
        }

        [Fact]
        public void ApplyMulch_Bound_ForwardsArgumentsAndResult()
        {
            var api = new LivingRootsModApi();
            string? seenLocation = null;
            var seenX = -1;
            var seenY = -1;
            api.BindMulchApplication((location, x, y) =>
            {
                seenLocation = location;
                seenX = x;
                seenY = y;
                return true;
            });

            Assert.True(api.ApplyMulch("Greenhouse", 4, 9));
            Assert.Equal("Greenhouse", seenLocation);
            Assert.Equal(4, seenX);
            Assert.Equal(9, seenY);
        }

        [Fact]
        public void ApplyMulch_ThrowingDelegate_ReturnsFalse()
        {
            var api = new LivingRootsModApi();
            api.BindMulchApplication((location, x, y) => throw new InvalidOperationException("boom"));

            Assert.False(api.ApplyMulch("Farm", 10, 10));
        }

        [Fact]
        public void ApplyMulch_AfterDispose_ReturnsFalseWithoutInvokingDelegate()
        {
            var api = new LivingRootsModApi();
            var calls = 0;
            api.BindMulchApplication((location, x, y) =>
            {
                calls++;
                return true;
            });
            api.Dispose();

            Assert.False(api.ApplyMulch("Farm", 10, 10));
            Assert.Equal(0, calls);
        }

        [Fact]
        public void BindMulchApplication_Null_Throws()
        {
            var api = new LivingRootsModApi();

            Assert.Throws<ArgumentNullException>(() => api.BindMulchApplication(null!));
        }
    }
}
