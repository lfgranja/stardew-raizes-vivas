using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter exposing a specific <see cref="Farmer"/>'s inventory through
    /// <see cref="IPlayerInventory"/>. <c>IPlayerInventory</c> exists because
    /// <c>Farmer</c> cannot be constructed in a unit-test host and
    /// <c>addItemToInventoryBool</c> is not virtual — the same seam
    /// <see cref="PlayerInventory"/> uses for <c>Game1.player</c>. This variant
    /// targets the farmer passed at the call site (T131, FR-005c), so a
    /// dislodged bin returns to THAT farmer's inventory rather than always to
    /// the local player's.
    /// </summary>
    public sealed class FarmerInventoryAdapter : IPlayerInventory
    {
        private readonly Farmer _farmer;

        public FarmerInventoryAdapter(Farmer farmer)
        {
            _farmer = farmer ?? throw new ArgumentNullException(nameof(farmer));
        }

        /// <inheritdoc />
        public bool TryAddItem(Item item)
        {
            return _farmer.addItemToInventoryBool(item);
        }
    }
}
