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
            throw new NotImplementedException("T121 implements the inventory-delta targeting.");
        }

        /// <summary>Returns the slots whose stack count grew, in slot order.</summary>
        public static IReadOnlyList<HarvestInventoryChange> Diff(
            IReadOnlyList<Item?> before,
            IReadOnlyList<Item?> after)
        {
            throw new NotImplementedException("T121 implements the inventory-delta targeting.");
        }

        /// <summary>Returns the primary added slot, or -1 when nothing was added.</summary>
        public static int FindPrimaryAddedSlot(IReadOnlyList<HarvestInventoryChange> changes)
        {
            throw new NotImplementedException("T121 implements the inventory-delta targeting.");
        }

        /// <summary>True when the inventory has no free slot and every stack is at max.</summary>
        public static bool IsFull(IReadOnlyList<Item?>? items, int maxItems)
        {
            throw new NotImplementedException("T121 implements the inventory-delta targeting.");
        }

        /// <summary>
        /// Selects the acting inventory: the Junimo harvester's when present,
        /// otherwise the triggering farmer's.
        /// </summary>
        public static IReadOnlyList<Item?>? SelectTargetInventory(
            IReadOnlyList<Item?>? junimoInventory,
            IReadOnlyList<Item?>? playerInventory)
        {
            throw new NotImplementedException("T121 implements the inventory-delta targeting.");
        }
    }
}
