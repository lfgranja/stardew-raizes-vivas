using System;
using System.Collections.Generic;
using LivingRoots.Verification;
using Xunit;

namespace LivingRoots.Tests.Verification;

/// <summary>
/// Tests for <see cref="MulchVerificationRules"/> (018 T006/T007).
/// </summary>
/// <remarks>
/// <c>GrantMulch</c> mixed host/count gating and inventory-slot selection with
/// game-static reads, so neither decision could be tested without booting the
/// game. The rules are now pure inputs and outputs; these tests pin them
/// directly. The command handler keeps the game-state reads and delegates the
/// decisions here.
/// </remarks>
public class MulchVerificationRulesTests
{
    /// <summary>Granting requires both a positive count and host authority.</summary>
    [Theory]
    [InlineData(1, true, true)]
    [InlineData(7, true, true)]
    [InlineData(0, true, false)]
    [InlineData(-3, true, false)]
    [InlineData(1, false, false)]
    [InlineData(7, false, false)]
    [InlineData(0, false, false)]
    public void CanGrant_RequiresPositiveCountOnHost(int count, bool isMainPlayer, bool expected)
    {
        Assert.Equal(expected, MulchVerificationRules.CanGrant(count, isMainPlayer));
    }

    /// <summary>The first empty slot is returned so the granted stack lands predictably.</summary>
    [Fact]
    public void FindEmptySlot_ReturnsFirstEmptySlot()
    {
        var slot = MulchVerificationRules.FindEmptySlot(4, 4, index => index < 2);

        Assert.Equal(2, slot);
    }

    /// <summary>An empty inventory has no slot to fill.</summary>
    [Fact]
    public void FindEmptySlot_EmptyInventory_ReturnsMinusOne()
    {
        Assert.Equal(-1, MulchVerificationRules.FindEmptySlot(0, 12, _ => false));
    }

    /// <summary>A full inventory has no slot to fill.</summary>
    [Fact]
    public void FindEmptySlot_FullInventory_ReturnsMinusOne()
    {
        Assert.Equal(-1, MulchVerificationRules.FindEmptySlot(2, 2, _ => true));
    }

    /// <summary>Capacity caps the search, and slots past it are never probed.</summary>
    [Fact]
    public void FindEmptySlot_StopsAtMaxItems()
    {
        var probed = new List<int>();

        var slot = MulchVerificationRules.FindEmptySlot(3, 2, index =>
        {
            probed.Add(index);
            return true;
        });

        Assert.Equal(-1, slot);
        Assert.Equal(new[] { 0, 1 }, probed);
    }

    /// <summary>A slot count shorter than MaxItems is searched only as far as it exists.</summary>
    [Fact]
    public void FindEmptySlot_SlotCountShorterThanCapacity_ReturnsFirstEmptySlot()
    {
        var slot = MulchVerificationRules.FindEmptySlot(2, 12, index => index == 0);

        Assert.Equal(1, slot);
    }

    /// <summary>A non-positive capacity never selects a slot.</summary>
    [Fact]
    public void FindEmptySlot_NonPositiveCapacity_ReturnsMinusOne()
    {
        Assert.Equal(-1, MulchVerificationRules.FindEmptySlot(1, 0, _ => false));
        Assert.Equal(-1, MulchVerificationRules.FindEmptySlot(1, -2, _ => false));
    }

    /// <summary>A missing occupancy predicate is a caller error, reported rather than guessed around.</summary>
    [Fact]
    public void FindEmptySlot_NullPredicate_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => MulchVerificationRules.FindEmptySlot(12, 12, null!));
    }
}
