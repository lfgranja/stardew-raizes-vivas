namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for normalizing Unicode characters to handle diacritics, homoglyphs, and other Unicode security issues
    /// </summary>
    public interface IUnicodeNormalizer
    {
        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns
        /// </summary>
        /// <param name="input">The input string to normalize</param>
        /// <returns>The normalized string</returns>
        string? Normalize(string? input);
    }
}
