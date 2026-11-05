using System;
using System.Collections.Generic;
using StardewValley;

namespace LivingRoots.Domain
{
    /// <summary>A slot whose item count grew between two inventory snapshots.</summary>
    public readonly record struct HarvestInventoryChange(int SlotIndex, int AddedCount);

    /// <summary>Captured slot identity for diffing inventories without booted game data.</summary>
    public readonly record struct InventorySlotSnapshot(Item? Item, string? QualifiedItemId, int Stack, int Quality);

    /// <summary>
    /// Snapshot/diff helpers for locating the item vanilla actually harvested.
    /// The postfix must not assume the last slot holds the harvest: stacking,
    /// multi-yield crops, Junimo harvests, and blocked full-inventory harvests
    /// all break that assumption (FR-017/A1).
    /// </summary>
    public static class HarvestInventoryDelta
    {
        /// <summary>Captures slot identity and stack counts of an inventory.</summary>
        public static IReadOnlyList<InventorySlotSnapshot> Snapshot(IReadOnlyList<Item?>? items)
        {
            if (items == null)
            {
                return Array.Empty<InventorySlotSnapshot>();
            }

            var copy = new List<InventorySlotSnapshot>(items.Count);
            foreach (var item in items)
            {
                if (item == null)
                {
                    copy.Add(new InventorySlotSnapshot(null, null, 0, 0));
                    continue;
                }

                string? id = null;
                try
                {
                    id = item.QualifiedItemId;
                }
                catch
                {
                    id = null;
                }

                copy.Add(new InventorySlotSnapshot(item, id, item.Stack, item.Quality));
            }

            return copy;
        }

        /// <summary>Returns the slots whose stack count grew, in slot order.</summary>
        public static IReadOnlyList<HarvestInventoryChange> Diff(
            IReadOnlyList<InventorySlotSnapshot> before,
            IReadOnlyList<InventorySlotSnapshot> after)
        {
            var result = new List<HarvestInventoryChange>();
            int beforeCount = before == null ? 0 : before.Count;
            int afterCount = after == null ? 0 : after.Count;
            int slots = Math.Max(beforeCount, afterCount);

            for (int i = 0; i < slots; i++)
            {
                var beforeSlot = i < beforeCount ? before![i] : default;
                var afterSlot = i < afterCount ? after![i] : default;
                int added = ComputeAdded(beforeSlot, afterSlot);
                if (added > 0)
                {
                    result.Add(new HarvestInventoryChange(i, added));
                }
            }

            return result;
        }

        private static int ComputeAdded(InventorySlotSnapshot beforeSlot, InventorySlotSnapshot afterSlot)
        {
            if (afterSlot.Item == null)
            {
                return 0;
            }

            int stack = afterSlot.Stack > 0 ? afterSlot.Stack : 1;
            if (beforeSlot.Item == null)
            {
                return stack;
            }

            bool sameRef = ReferenceEquals(beforeSlot.Item, afterSlot.Item);
            bool sameId = string.Equals(beforeSlot.QualifiedItemId, afterSlot.QualifiedItemId, StringComparison.Ordinal)
                          && beforeSlot.Quality == afterSlot.Quality;
            if (!sameRef && !sameId)
            {
                return stack;
            }

            if (afterSlot.Stack > beforeSlot.Stack)
            {
                return afterSlot.Stack - beforeSlot.Stack;
            }

            return 0;
        }

        /// <summary>Returns the primary added slot, or -1 when nothing was added.</summary>
        public static int FindPrimaryAddedSlot(IReadOnlyList<HarvestInventoryChange> changes)
        {
            if (changes == null || changes.Count == 0)
            {
                return -1;
            }

            return changes[0].SlotIndex;
        }

        /// <summary>True when the inventory has no free slot and every stack is at max.</summary>
        public static bool IsFull(IReadOnlyList<Item?>? items, int maxItems)
        {
            if (items == null)
            {
                return true;
            }

            if (items.Count < maxItems)
            {
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Selects the acting inventory: the Junimo harvester's when present,
        /// otherwise the triggering farmer's.
        /// </summary>
        public static IReadOnlyList<Item?>? SelectTargetInventory(
            IReadOnlyList<Item?>? junimoInventory,
            IReadOnlyList<Item?>? playerInventory)
        {
            if (junimoInventory != null)
            {
                return junimoInventory;
            }

            return playerInventory;
        }
    }
}
