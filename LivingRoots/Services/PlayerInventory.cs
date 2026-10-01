using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Production implementation of <see cref="IPlayerInventory"/>, granting items to
    /// <c>Game1.player</c>. This is the local player, matching the farmer the bin service
    /// previously received as a parameter.
    /// </summary>
    public class PlayerInventory : IPlayerInventory
    {
        /// <inheritdoc />
        public bool TryAddItem(Item item)
        {
            return Game1.player.addItemToInventoryBool(item);
        }
    }
}
