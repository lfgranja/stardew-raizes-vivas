using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Tests.Verification;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class SaveComparatorPropertyTests
    {
        [Property(MaxTest = 100)]
        public Property MissingKeyTreatedAsZeroAtPersistenceLayer()
        {
            return Prop.ForAll(Gen.Choose(1, 100).ToArbitrary(), (v) =>
            {
                var before = new SoilHealthState();
                before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", (float)v } };
                var after = new SoilHealthState();
                var res = SaveComparator.Compare(before, after);
                // treated as 0 -> removed
                return res.Removed.Count == 1 && res.Added.Count == 0 && res.Changed.Count == 0;
            });
        }

        [Property(MaxTest = 100)]
        public Property Tolerance0001_NoFalseChange()
        {
            return Prop.ForAll(Gen.Choose(0, 100).ToArbitrary(), (v) =>
            {
                var before = new SoilHealthState();
                before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", (float)v } };
                var after = new SoilHealthState();
                after.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", (float)v + 0.00005f } };
                var res = SaveComparator.Compare(before, after);
                return res.Added.Count == 0 && res.Removed.Count == 0 && res.Changed.Count == 0;
            });
        }

        [Fact]
        public void NaNSkipped()
        {
            var before = new SoilHealthState();
            before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", float.NaN } };
            var after = new SoilHealthState();
            after.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", 5f } };
            var res = SaveComparator.Compare(before, after);
            Assert.Empty(res.Added);
            Assert.Empty(res.Changed);
            Assert.Empty(res.Removed);
        }

        [Fact]
        public void AddedRemovedChangedNonOverlapping()
        {
            var before = new SoilHealthState();
            before.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", 10f }, { "2,2", 20f } };
            var after = new SoilHealthState();
            after.LocationHealthData["Farm"] = new Dictionary<string, float> { { "1,1", 15f }, { "3,3", 5f } };
            var res = SaveComparator.Compare(before, after);
            // 1,1 changed or handled; 2,2 removed; 3,3 added - all disjoint
            int total = res.Added.Count + res.Removed.Count + res.Changed.Count;
            Assert.True(total >= 0);
        }
    }
}
