using System;

namespace LivingRoots.Services;

/// <summary>
/// Deterministic crioulo seed save (spec 020 FR-009): exactly one seed per
/// completed heirloom harvest — no probability, no farming-level scaling.
/// The caller owns item creation (ItemRegistry) and the Junimo exemption
/// (Junimo harvests have no player inventory and never grant).
/// </summary>
public static class HeirloomSeedSaveService
{
    /// <summary>
    /// Attempts the deterministic grant. Returns false (without mutating) when the
    /// item cannot be accepted for capacity or inventory reasons; true on success.
    /// </summary>
    /// <param name="seedItem">The item to grant (created by the caller).</param>
    /// <param name="inventory">Host/farmhand player inventory; null means no player (Junimo) — never grants.</param>
    /// <param name="capacity">Optional dry-run capacity query; checked first and never mutates.</param>
    public static bool TryGrantSavedSeed(StardewValley.Item? seedItem, Domain.IPlayerInventory? inventory, Domain.IPlayerInventoryCapacity? capacity)
    {
        if (seedItem == null || inventory == null)
        {
            return false;
        }

        if (capacity != null && !capacity.HasCapacity(seedItem, 1))
        {
            return false;
        }

        return inventory.TryAddItem(seedItem);
    }
}
