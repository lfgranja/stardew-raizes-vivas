namespace LivingRoots.Domain
{
    /// <summary>
    /// Validates whether an item qualifies as organic waste for composting.
    /// Validates against vanilla category IDs, custom context tags, and exclusion tags.
    /// </summary>
    public interface IOrganicWasteValidator
    {
        /// <summary>
        /// Determines if the specified item is valid organic waste for composting.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <returns>True if the item is valid organic waste, false otherwise.</returns>
        bool IsValidOrganicWaste(StardewValley.Item item);
    }
}
