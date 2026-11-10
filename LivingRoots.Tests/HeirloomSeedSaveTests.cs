using LivingRoots;
using LivingRoots.Domain;
using Moq;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Spec 020 seed-save contract tests (FR-009, US2): exactly one saved seed per
/// completed heirloom harvest; deterministic; no probability; no farming-level
/// scaling; full or incapable inventory refuses without destroying anything;
/// Junimo harvests do not grant (no player inventory exists).
/// </summary>
public class HeirloomSeedSaveTests
{
    [Fact]
    public void SeedSave_NormalInventory_GrantsExactlyOneSeed()
    {
        var seedItem = new StardewValley.Object { ItemId = ModConstants.HeirloomItemId };
        var inventory = new Mock<IPlayerInventory>();
        inventory.Setup(i => i.TryAddItem(It.IsAny<StardewValley.Item>())).Returns(true);
        var granted = LivingRoots.Services.HeirloomSeedSaveService.TryGrantSavedSeed(seedItem, inventory.Object, capacity: null);

        Assert.True(granted);
        inventory.Verify(i => i.TryAddItem(seedItem), Times.Once);
    }

    [Fact]
    public void SeedSave_InventoryFull_ThrowsNothingAndReturnsFalse_SeedNotConsumed()
    {
        var seedItem = new StardewValley.Object { ItemId = ModConstants.HeirloomItemId };
        var inventory = new Mock<IPlayerInventory>();
        inventory.Setup(i => i.TryAddItem(It.IsAny<StardewValley.Item>())).Returns(false);
        var granted = LivingRoots.Services.HeirloomSeedSaveService.TryGrantSavedSeed(seedItem, inventory.Object, capacity: null);

        Assert.False(granted);
        inventory.Verify(i => i.TryAddItem(seedItem), Times.Once);
    }

    [Fact]
    public void SeedSave_CapacitySaysNo_TryAddItemNotCalled()
    {
        var seedItem = new StardewValley.Object { ItemId = ModConstants.HeirloomItemId };
        var inventory = new Mock<IPlayerInventory>(MockBehavior.Strict);
        var capacity = new Mock<LivingRoots.Domain.IPlayerInventoryCapacity>();
        capacity.Setup(c => c.HasCapacity(seedItem, 1)).Returns(false);
        var granted = LivingRoots.Services.HeirloomSeedSaveService.TryGrantSavedSeed(seedItem, inventory.Object, capacity.Object);

        Assert.False(granted);
        inventory.Verify(i => i.TryAddItem(It.IsAny<StardewValley.Item>()), Times.Never);
    }

    [Fact]
    public void SeedSave_JunimoHarvest_NoPlayerInventory_NoGrantAttempted()
    {
        var granted = LivingRoots.Services.HeirloomSeedSaveService.TryGrantSavedSeed(null, null, null);

        Assert.False(granted);
    }

    [Fact]
    public void SeedSave_DeterministicConstant_IsExactlyOne()
    {
        Assert.Equal(1, ModConstants.HeirloomSavedSeedYield);
    }
}
