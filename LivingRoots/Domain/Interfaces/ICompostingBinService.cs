using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Service for managing composting bin machines.
    /// Handles adding waste, collecting compost, state transitions, maturation, and persistence.
    /// </summary>
    public interface ICompostingBinService
    {
        /// <summary>
        /// Adds an organic waste item to the specified composting bin if it is empty and the item is valid.
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        /// <param name="item">The item to add as waste.</param>
        void AddWaste(string locationName, Vector2 tile, StardewValley.Item item);

        /// <summary>
        /// Collects finished compost from the specified bin if it is ready.
        /// The compost is granted through the injected <see cref="IPlayerInventory"/>.
        /// One batch yields exactly one compost item (exactly-one batch yield, FR-005a).
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        /// <returns>The number of compost items produced (exactly one).</returns>
        int CollectCompost(string locationName, Vector2 tile);

        /// <summary>
        /// Gets the current state of the composting bin at the specified tile.
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        /// <returns>The current CompostingBinState (Empty, Processing, or Ready).</returns>
        CompostingBinState GetBinState(string locationName, Vector2 tile);

        /// <summary>
        /// Processes day start for all bins in a location. Exactly-once per day, per location: a repeat call for an already-processed day is skipped (duplicate DayStarted deliveries carry no new day). Checks processing completion and maturation resets.
        /// </summary>
        /// <param name="locationName">The location containing the bins.</param>
        void ProcessDayStart(string locationName);

        /// <summary>
        /// Called when a composting bin object is removed/broken. Clears tracked state only and does not drop items on the ground (progress preservation is via CreateDislodgedBinItem, dropped by the caller).
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        void OnObjectRemoved(string locationName, Vector2 tile);

        /// <summary>
        /// Loads composting bin data from persistent storage.
        /// </summary>
        /// <param name="saveId">The save game ID to load data for.</param>
        void LoadData(string saveId);

        /// <summary>
        /// Creates a dislodged bin item carrying progress (modData preserved) when removed by tool/explosion.
        /// </summary>
        /// <param name="locationName">The location where the bin was removed.</param>
        /// <param name="tile">The tile coordinates of the removed bin.</param>
        /// <returns>The dislodged item, or null if no progress to preserve.</returns>
        StardewValley.Item? CreateDislodgedBinItem(string locationName, Vector2 tile);

        /// <summary>
        /// Called when a composting bin is placed in the world, registering it in the runtime cache.
        /// If the item carries progress modData, restores the batch progress.
        /// </summary>
        /// <param name="locationName">The location where the bin was placed.</param>
        /// <param name="tile">The tile coordinates of the placed bin.</param>
        /// <param name="binItem">The bin item being placed, which may carry progress modData.</param>
        void OnBinPlaced(string locationName, Vector2 tile, StardewValley.Item? binItem = null);

        /// <summary>
        /// Saves composting bin data to persistent storage.
        /// </summary>
        /// <param name="saveId">The save game ID to save data for.</param>
        void SaveData(string saveId);
    }
}
