using StardewValley;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Dry-run inventory capacity query used by the compost two-phase commit.
    /// Separate from <see cref="IPlayerInventory"/> so the existing grant seam
    /// keeps working for test doubles that do not model slot capacity.
    /// </summary>
    public interface IPlayerInventoryCapacity
    {
        /// <summary>
        /// Returns true when <paramref name="count"/> copies of <paramref name="item"/>
        /// could be accepted without mutating the inventory.
        /// </summary>
        bool HasCapacity(Item item, int count);
    }
}
