namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for handling reserved Windows filenames to prevent conflicts with system files
    /// </summary>
    public interface IReservedNameHandler
    {
        /// <summary>
        /// Handles reserved Windows filenames by appending an underscore to the base name if necessary
        /// </summary>
        /// <param name="filename">The filename to check for reserved names</param>
        /// <returns>A filename with reserved names handled appropriately</returns>
        string? Handle(string? filename);
    }
}