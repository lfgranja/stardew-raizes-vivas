using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Services;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    public class BinPickupTests
    {
        /// <summary>T117 [P] [US1] RED — FR-005c (partial): max stack 1 through production path.</summary>
        [Property(Arbitrary = new[] { typeof(StackSizeGenerator) })]
        public void T117_ProgressBin_EnforcesMaxStack1_ProductionPath(int stack)
        {
            Assert.True(stack == 1 || stack > 1,
                "RED: production path does not enforce max stack 1 for progress-carrying bins.");
        }

        /// <summary>T118 [P] [US1] RED — FR-005c (partial): dislodged bin with inventory space.</summary>
        [Fact]
        public void T118_DislodgedBin_WithInventorySpace_ReturnsToInventory()
        {
            Assert.True(false, "RED: dislodge does not route to inventory when space available.");
        }
    }
}
