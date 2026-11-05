using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HarvestInventoryTargetingTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        private static InventorySlotSnapshot Slot(string? id, int stack, int quality = 0)
        {
            return new InventorySlotSnapshot(new object() as Item, id == null ? null : (id.StartsWith('(') ? id : $"(O){id}"), stack, quality);
        }

        private static List<Item?> MakeFullInventory(int count, int stack)
        {
            var items = new List<Item?>();
            for (int i = 0; i < count; i++)
            {
                items.Add(null);
            }
            // Full means no free slot; represent occupied slots as non-null sentinels.
            for (int i = 0; i < count; i++)
            {
                items[i] = new object() as Item;
            }
            return items;
        }

        [Fact]
        public void Diff_DetectsNewAndStackedItems()
        {
            var item = new object() as Item;
            var before = new List<InventorySlotSnapshot>
            {
                new InventorySlotSnapshot(item, "(O)24", 1, 0),
                new InventorySlotSnapshot(null, null, 0, 0),
            };

            var after = new List<InventorySlotSnapshot>
            {
                new InventorySlotSnapshot(item, "(O)24", 3, 0),
                new InventorySlotSnapshot(new object() as Item, "(O)24", 1, 0),
            };

            var changes = HarvestInventoryDelta.Diff(before, after);

            Assert.Equal(2, changes.Count);
            Assert.Equal(0, changes[0].SlotIndex);
            Assert.Equal(2, changes[0].AddedCount);
            Assert.Equal(1, changes[1].SlotIndex);
            Assert.Equal(1, changes[1].AddedCount);
        }

        [Fact]
        public void Diff_BlockedHarvest_ReturnsNoChanges()
        {
            var item = new object() as Item;
            var before = new List<InventorySlotSnapshot> { new InventorySlotSnapshot(item, "(O)24", 1, 0) };
            var after = new List<InventorySlotSnapshot> { new InventorySlotSnapshot(item, "(O)24", 1, 0) };

            var changes = HarvestInventoryDelta.Diff(before, after);

            Assert.Empty(changes);
            Assert.Equal(-1, HarvestInventoryDelta.FindPrimaryAddedSlot(changes));
        }

        [Fact]
        public void FindPrimaryAddedSlot_ReturnsLowestChangedSlot()
        {
            var itemA = new object() as Item;
            var itemB = new object() as Item;
            var before = new List<InventorySlotSnapshot>
            {
                new InventorySlotSnapshot(itemA, "(O)24", 1, 0),
                new InventorySlotSnapshot(itemB, "(O)24", 1, 0),
            };
            var after = new List<InventorySlotSnapshot>
            {
                new InventorySlotSnapshot(itemA, "(O)24", 1, 0),
                new InventorySlotSnapshot(itemB, "(O)24", 3, 0),
            };

            var changes = HarvestInventoryDelta.Diff(before, after);

            Assert.Equal(1, HarvestInventoryDelta.FindPrimaryAddedSlot(changes));
        }

        [Property]
        public Property Diff_SnapshotOfSameInventory_IsEmpty()
        {
            var item = new object() as Item;
            var inventory = new List<InventorySlotSnapshot>
            {
                new InventorySlotSnapshot(item, "(O)24", 2, 0),
                new InventorySlotSnapshot(null, null, 0, 0),
                new InventorySlotSnapshot(new object() as Item, "(O)24", 1, 0),
            };

            var changes = HarvestInventoryDelta.Diff(inventory, inventory);

            return (changes.Count == 0).ToProperty();
        }

        [Theory]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(36)]
        public void IsFull_BackpackTiers(int maxItems)
        {
            var notFull = MakeFullInventory(maxItems - 1, 999);
            var full = MakeFullInventory(maxItems, 999);

            Assert.False(HarvestInventoryDelta.IsFull(notFull, maxItems));
            Assert.True(HarvestInventoryDelta.IsFull(full, maxItems));
        }

        [Fact]
        public void IsFull_FreeSlotExists_IsFalse()
        {
            var inventory = new List<Item?> { new object() as Item, null };

            Assert.False(HarvestInventoryDelta.IsFull(inventory, 36));
        }

        [Fact]
        public void SelectTargetInventory_PrefersJunimoThenPlayer()
        {
            var player = new List<Item?> { (Item?)null };
            var junimo = new List<Item?> { new object() as Item };

            var selectedJunimo = HarvestInventoryDelta.SelectTargetInventory(junimo, player);
            var selectedPlayer = HarvestInventoryDelta.SelectTargetInventory(null, player);
            var selectedNone = HarvestInventoryDelta.SelectTargetInventory(null, null);

            Assert.Same(junimo, selectedJunimo);
            Assert.Same(player, selectedPlayer);
            Assert.Null(selectedNone);
        }

        [Fact]
        public void QiFruit_IsOutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsQiFruit = true, FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void FibreSeed_IsOutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsFibreSeed = true, FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void ForageCrop_IsOutOfScope()
        {
            var inputs = new HarvestQualityInputs { IsForageCrop = true, FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            Assert.Equal(HarvestTier.OutOfScope, decision.Tier);
            Assert.False(decision.ForceQualityZero);
        }

        [Fact]
        public void RegularCrop_IsInScope()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            Assert.True(decision.IsInScope);
            Assert.NotEqual(HarvestTier.OutOfScope, decision.Tier);
        }

        [Fact]
        public void FullInventory_DecisionBlocksMainAndDropsExtra()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, IsInventoryFull = true };
            var decision = _service.Decide(80.0f, inputs);

            Assert.True(decision.MainItemBlocked);
            Assert.True(decision.ExtraItemDroppedAsDebris);
        }
    }
}
