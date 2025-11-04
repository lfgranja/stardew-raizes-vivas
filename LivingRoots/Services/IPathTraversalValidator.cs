namespace LivingRoots.Services
{
    /// <summary>
    /// Interface for validating and preventing path traversal attacks
    /// </summary>
    public interface IPathTraversalValidator
    {
        /// <summary>
        /// Validates that a path does not contain path traversal patterns
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected</exception>
        void Validate(string path);
    }
}