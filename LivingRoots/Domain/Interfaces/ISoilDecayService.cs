namespace LivingRoots.Domain
{
    /// <summary>
    /// Service responsible for applying daily soil health decay to bare tilled tiles.
    /// Iterates all tilled tiles at day start, applies seasonal-multiplied decay to bare tiles.
    /// Dead crop residue acts as mulch protection — decay does not apply to tiles with dead crops.
    /// Greenhouse is completely exempt from decay.
    /// </summary>
    public interface ISoilDecayService
    {
        /// <summary>
        /// Processes all tilled tiles in the specified location and applies daily decay
        /// to bare tiles (those with no living crop and no dead residue).
        /// </summary>
        /// <param name="locationName">The name of the game location to process.</param>
        void ProcessDayStart(string locationName);
    }
}
