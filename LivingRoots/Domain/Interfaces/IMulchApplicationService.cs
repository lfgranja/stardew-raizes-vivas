namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// Service for applying mulch to a tilled tile, following the DDD pattern
    /// (interface in Domain, implementation in Services).
    /// </summary>
    public interface IMulchApplicationService
    {
        /// <summary>
        /// Attempts to apply mulch to the given tile.
        /// </summary>
        /// <param name="who">The farmer applying mulch.</param>
        /// <param name="hoeDirt">The tilled soil tile.</param>
        /// <param name="location">The game location.</param>
        /// <param name="messageKey">The i18n message key for refusal, if any.</param>
        /// <returns>True if mulch was applied; false otherwise.</returns>
        bool TryApplyMulch(StardewValley.Farmer who, StardewValley.TerrainFeatures.HoeDirt hoeDirt, StardewValley.GameLocation location, out string messageKey);
    }
}