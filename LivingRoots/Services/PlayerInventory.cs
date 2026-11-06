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
#pragma warning disable S2325 // S2325: implements IPlayerInventory; cannot be static
        public bool TryAddItem(Item item)
        {
            return Game1.player.addItemToInventoryBool(item);
        }

        public bool TryRemoveItem(Item item)
        {
            try
            {
                Game1.player.removeItemFromInventory(item);
                return true;
            }
            catch
            {
                return false;
            }
        }
#pragma warning restore S2325
    }
}
