using StardewValley;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Grants items to a player's inventory.
    /// Exists so services that hand items to the player can be tested without a booted game:
    /// <c>Farmer</c> cannot be constructed in a unit-test host and its
    /// <c>addItemToInventoryBool</c> member is not virtual, so neither Moq nor reflection can
    /// substitute it. Mirrors the extraction already done for <see cref="IPlayerProvider"/>.
    /// </summary>
    public interface IPlayerInventory
    {
        /// <summary>
        /// Adds an item to the player's inventory.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <returns>True when the item was accepted; false when the inventory refused it.</returns>
        bool TryAddItem(Item item);
    }
}
