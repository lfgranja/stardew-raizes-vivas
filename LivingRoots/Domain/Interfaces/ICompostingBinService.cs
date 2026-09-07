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
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        /// <param name="player">The player collecting the compost.</param>
        /// <returns>The number of compost items produced (equal to maturation level).</returns>
        int CollectCompost(string locationName, Vector2 tile, StardewValley.Farmer player);

        /// <summary>
        /// Gets the current state of the composting bin at the specified tile.
        /// </summary>
        /// <param name="locationName">The location containing the bin.</param>
        /// <param name="tile">The tile coordinates of the bin.</param>
        /// <returns>The current CompostingBinState (Empty, Processing, or Ready).</returns>
        CompostingBinState GetBinState(string locationName, Vector2 tile);

        /// <summary>
        /// Processes day start for all bins in a location. Checks processing completion and maturation resets.
        /// </summary>
        /// <param name="locationName">The location containing the bins.</param>
        void ProcessDayStart(string locationName);

        /// <summary>
        /// Called when a composting bin object is removed/broken. Drops items on ground and clears state.
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
        /// Saves composting bin data to persistent storage.
        /// </summary>
        /// <param name="saveId">The save game ID to save data for.</param>
        void SaveData(string saveId);
    }
}
