using System;
using System.Collections.Generic;
using StardewValley;

namespace LivingRoots.Domain
{
    /// <summary>A slot whose item count grew between two inventory snapshots.</summary>
    public readonly record struct HarvestInventoryChange(int SlotIndex, int AddedCount);

    /// <summary>
    /// Snapshot/diff helpers for locating the item vanilla actually harvested.
    /// The postfix must not assume the last slot holds the harvest: stacking,
    /// multi-yield crops, Junimo harvests, and blocked full-inventory harvests
    /// all break that assumption (FR-017/A1).
    /// </summary>
    public static class HarvestInventoryDelta
    {
        /// <summary>Captures the item references and stack counts of an inventory.</summary>
        public static IReadOnlyList<Item?> Snapshot(IReadOnlyList<Item?>? items)
        {
            if (items == null)
            {
                return Array.Empty<Item?>();
            }

            var copy = new List<Item?>(items.Count);
            foreach (var item in items)
            {
                copy.Add(item);
            }

            return copy;
        }

        /// <summary>Returns the slots whose stack count grew, in slot order.</summary>
        public static IReadOnlyList<HarvestInventoryChange> Diff(
            IReadOnlyList<Item?> before,
            IReadOnlyList<Item?> after)
        {
            var result = new List<HarvestInventoryChange>();
            int beforeCount = before == null ? 0 : before.Count;
            int afterCount = after == null ? 0 : after.Count;
            int slots = Math.Max(beforeCount, afterCount);

            for (int i = 0; i < slots; i++)
            {
                Item? beforeItem = i < beforeCount ? before![i] : null;
                Item? afterItem = i < afterCount ? after![i] : null;

                int added = 0;
                if (beforeItem == null && afterItem != null)
                {
                    added = afterItem.Stack > 0 ? afterItem.Stack : 1;
                }
                else if (beforeItem != null && afterItem != null &&
                         (beforeItem.QualifiedItemId != afterItem.QualifiedItemId ||
                          beforeItem.Quality != afterItem.Quality))
                {
                    added = afterItem.Stack > 0 ? afterItem.Stack : 1;
                }
                else if (beforeItem != null && afterItem != null && afterItem.Stack > beforeItem.Stack)
                {
                    added = afterItem.Stack - beforeItem.Stack;
                }

                if (added > 0)
                {
                    result.Add(new HarvestInventoryChange(i, added));
                }
            }

            return result;
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

            foreach (var item in items)
            {
                if (item == null)
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
