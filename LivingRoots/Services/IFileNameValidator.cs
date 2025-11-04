namespace LivingRoots.Services
{
    /// <summary>
    /// Interface for validating filenames to ensure they meet platform-specific requirements
    /// </summary>
    public interface IFileNameValidator
    {
        /// <summary>
        /// Validates that a filename is safe and valid for file system operations
        /// </summary>
        /// <param name="filename">The filename to validate</param>
        /// <exception cref="ArgumentException">Thrown if the filename is invalid</exception>
        void Validate(string filename);
    }
}