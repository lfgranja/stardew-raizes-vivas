using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for Unicode normalization service in the domain layer
    /// following the Dependency Inversion Principle
    /// </summary>
    public interface IUnicodeNormalizationService
    {
        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns.
        /// Security-focused normalization that converts potentially deceptive characters while preserving legitimate Unicode.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>The normalized string.</returns>
        string? Normalize(string? input);
    }
}