using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Moq;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HarvestInventoryTargetingTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        private static Item MakeItem(string id, int stack, int quality = 0)
        {
            var item = new Mock<Item>();
            item.SetupGet(i => i.QualifiedItemId).Returns(id.StartsWith("(") ? id : $"(O){id}");
            item.SetupGet(i => i.ItemId).Returns(id);
            item.SetupProperty(i => i.Stack, stack);
            item.SetupProperty(i => i.Quality, quality);
            return item.Object;
        }

        private static List<Item?> MakeFullInventory(int count, int stack)
        {
            var items = new List<Item?>();
            for (int i = 0; i < count; i++)
            {
                items.Add(MakeItem("24", stack));
            }

            return items;
        }

        [Fact]
        public void Diff_DetectsNewAndStackedItems()
        {
            var before = new List<Item?>
            {
                MakeItem("24", 1),
                null,
            };

            var after = new List<Item?>
            {
                MakeItem("24", 3),
                MakeItem("24", 1),
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
            var before = new List<Item?> { MakeItem("24", 1) };
            var after = new List<Item?> { MakeItem("24", 1) };

            var changes = HarvestInventoryDelta.Diff(before, after);

            Assert.Empty(changes);
            Assert.Equal(-1, HarvestInventoryDelta.FindPrimaryAddedSlot(changes));
        }

        [Fact]
        public void FindPrimaryAddedSlot_ReturnsLowestChangedSlot()
        {
            var before = new List<Item?> { MakeItem("24", 1), MakeItem("24", 1) };
            var after = new List<Item?> { MakeItem("24", 1), MakeItem("24", 3) };

            var changes = HarvestInventoryDelta.Diff(before, after);

            Assert.Equal(1, HarvestInventoryDelta.FindPrimaryAddedSlot(changes));
        }

        [Property]
        public Property Diff_SnapshotOfSameInventory_IsEmpty()
        {
            var inventory = new List<Item?> { MakeItem("24", 2), null, MakeItem("24", 1) };
            var before = HarvestInventoryDelta.Snapshot(inventory);
            var after = HarvestInventoryDelta.Snapshot(inventory);

            var changes = HarvestInventoryDelta.Diff(before, after);

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
            var inventory = new List<Item?> { MakeItem("24", 1), null };

            Assert.False(HarvestInventoryDelta.IsFull(inventory, 36));
        }

        [Fact]
        public void SelectTargetInventory_PrefersJunimoThenPlayer()
        {
            var player = new List<Item?> { MakeItem("24", 1) };
            var junimo = new List<Item?> { MakeItem("24", 2) };

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
