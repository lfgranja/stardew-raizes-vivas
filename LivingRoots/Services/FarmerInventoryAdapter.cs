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
    /// <para>
    /// The item is inserted into <see cref="Farmer.Items"/> directly rather
    /// than through <c>addItemToInventoryBool</c>: that method reads the
    /// process-global <c>Game1.player</c> for its local-player branch and
    /// throws in a headless host where <c>Game1.player</c> is unset. Direct
    /// insertion is the same list the game exposes, keeps the farmer-scoped
    /// contract, and remains testable.
    /// </para>
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
            if (item == null)
            {
                return false;
            }

            var items = _farmer.Items;
            if (items == null)
            {
                return false;
            }

            // Stack onto an existing compatible slot first, mirroring the
            // game's inventory behaviour.
            for (int i = 0; i < items.Count; i++)
            {
                var existing = items[i];
                if (existing == null ||
                    existing.QualifiedItemId != item.QualifiedItemId)
                {
                    continue;
                }

                int maxStack = existing.maximumStackSize();
                if (existing.Stack >= maxStack)
                {
                    continue;
                }

                int moved = System.Math.Min(maxStack - existing.Stack, item.Stack);
                existing.Stack += moved;
                item.Stack -= moved;
                if (item.Stack <= 0)
                {
                    return true;
                }
            }

            // Then the first empty slot.
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                {
                    items[i] = item;
                    return true;
                }
            }

            // Append only while the farmer's inventory has room.
            if (items.Count < Farmer.maxInventorySpace)
            {
                items.Add(item);
                return true;
            }

            return false;
        }
    }
}
