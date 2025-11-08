using System;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Adapter implementation for IUnicodeNormalizer that uses domain service
    /// </summary>
    public class UnicodeNormalizer : IUnicodeNormalizer
    {
        private readonly IUnicodeNormalizationService _unicodeNormalizationService;

        public UnicodeNormalizer(IUnicodeNormalizationService unicodeNormalizationService)
        {
            _unicodeNormalizationService = unicodeNormalizationService ?? throw new ArgumentNullException(nameof(unicodeNormalizationService));
        }

        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns.
        /// Security-focused normalization that converts potentially deceptive characters while preserving legitimate Unicode.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>The normalized string.</returns>
        public string? Normalize(string? input)
        {
            return _unicodeNormalizationService.Normalize(input);
        }
    }
}