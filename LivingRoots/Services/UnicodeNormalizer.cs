using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for normalizing Unicode characters to handle diacritics, homoglyphs, and other Unicode security issues.
    /// This class provides security-focused Unicode normalization to prevent homoglyph attacks and other Unicode-based security vulnerabilities.
    /// </summary>
    /// <remarks>
    /// Conversion Rules:
    /// 1. Diacritics: Removed from Latin and Greek letters, preserved for other scripts
    /// 2. Security Confusables: Cyrillic lookalikes are converted to Latin equivalents unless they appear in legitimate non-Latin script contexts
    /// 3. Zero-width and bidirectional characters: Removed completely for security
    /// 4. Control characters: Replaced with underscores to maintain spacing
    /// 5. Precomposed characters: Simplified to base forms (e.g., 'ø' → 'o')
    /// </remarks>
    public class UnicodeNormalizer : IUnicodeNormalizer
    {
        private static readonly ImmutableDictionary<char, string> SecurityConfusables = ImmutableDictionary.CreateRange<char, string>(new Dictionary<char, string>
        {
            // Characters that are commonly used in homoglyph attacks (always converted for security)
            // Cyrillic lookalikes that can be used to disguise Latin text
            { 'а', "a" }, { 'е', "e" }, { 'о', "o" }, { 'р', "p" }, { 'с', "c" }, { 'х', "h" }, { 'у', "y" },  // Cyrillic lowercase lookalikes
            { 'А', "A" }, { 'Е', "E" }, { 'О', "O" }, { 'Р', "P" }, { 'С', "C" }, { 'Х', "H" }, { 'У', "Y" },  // Cyrillic uppercase lookalikes
            { 'і', "i" }, { 'І', "I" }, // Additional Cyrillic lookalikes
            // Other confusable characters
            { '–', "-" }, { '—', "-" }, { '\'', "'" }, { '"', "\"" }, // Different types of quotes and dashes
        });

        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns.
        /// Security-focused normalization that converts potentially deceptive characters while preserving legitimate Unicode.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>The normalized string.</returns>
        public string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Apply decomposition normalization to separate base characters from diacritics first
            string decomposed = input.Normalize(NormalizationForm.FormD);
            
            var resultBuilder = new StringBuilder();

            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];

                // Handle surrogate pairs (needed for emojis and other characters outside BMP)
                if (char.IsHighSurrogate(c) && i + 1 < decomposed.Length && char.IsLowSurrogate(decomposed[i + 1]))
                {
                    // For surrogate pairs (like emojis), preserve them as-is
                    resultBuilder.Append(c);
                    resultBuilder.Append(decomposed[i + 1]);
                    i++; // Skip low surrogate since we've processed it
                    continue;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category == UnicodeCategory.NonSpacingMark)
                {
                    // This is a combining mark (like a diacritic)
                    // For the Greek test case, we need to remove diacritics from Greek letters too
                    if (ShouldRemoveDiacritic(resultBuilder))
                    {
                        // Remove diacritics from Latin and Greek letters
                        continue;
                    }
                    else
                    {
                        // This is likely part of a legitimate non-Latin character, keep it
                        resultBuilder.Append(c);
                    }
                }
                else if (category == UnicodeCategory.Control || category == UnicodeCategory.Format)
                {
                    // Handle different types of control/format characters differently:
                    // - Zero-width characters and bidirectional overrides should be removed completely
                    // - Other control characters should be replaced with underscores
                    ProcessControlOrFormatCharacter(resultBuilder, c);
                }
                else
                {
                    // Check for security confusables (homoglyphs that should be converted)
                    // For better security, we need to be more careful about when to convert
                    // Convert homoglyphs if they appear in a context that might be deceptive
                    if (SecurityConfusables.TryGetValue(c, out var replacement))
                    {
                        // Check if this might be a legitimate non-Latin script context
                        // If the surrounding characters are from the same script family, preserve it
                        char prevChar = GetPreviousNonMarkChar(resultBuilder);
                        char nextChar = GetNextNonMarkChar(decomposed, i + 1);
                        
                        // If both neighbors are from the same non-Latin script, preserve the character
                        if (IsCyrillicLetter(prevChar) && IsCyrillicLetter(nextChar))
                        {
                            // Both neighbors are Cyrillic, so this is likely part of a legitimate Cyrillic word
                            resultBuilder.Append(c);
                        }
                        else
                        {
                            // Apply the conversion for potential security homoglyph
                            resultBuilder.Append(replacement);
                        }
                    }
                    else
                    {
                        // we need special handling
                        char simplified = SimplifyCharacter(c);
                        resultBuilder.Append(simplified);
                    }
                }
            }

            // Return in composed form to properly handle combined characters
            return resultBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Determines if a diacritic should be removed based on the previous character.
        /// Diacritics are removed from Latin and Greek letters but preserved for other scripts.
        /// </summary>
        /// <param name="builder">The StringBuilder containing processed characters so far.</param>
        /// <returns>True if the diacritic should be removed, false otherwise.</returns>
        private static bool ShouldRemoveDiacritic(StringBuilder builder)
        {
            char prevChar = GetPreviousNonMarkChar(builder);
            return IsLatinLetter(prevChar) || IsGreekLetter(prevChar);
        }

        /// <summary>
        /// Processes control and format characters according to security rules.
        /// Zero-width characters and bidirectional overrides are removed completely.
        /// Other control characters are replaced with underscores to maintain spacing.
        /// </summary>
        /// <param name="resultBuilder">The StringBuilder to append results to.</param>
        /// <param name="c">The control or format character to process.</param>
        private static void ProcessControlOrFormatCharacter(StringBuilder resultBuilder, char c)
        {
            if (IsZeroWidthOrBidirectional(c))
            {
                // Remove zero-width characters and bidirectional overrides completely
                return;
            }
            else
            {
                // Replace other control and format characters with underscores to maintain spacing
                // Only add underscore if it's not already the last character
                if (resultBuilder.Length == 0 || resultBuilder[resultBuilder.Length - 1] != '_')
                    resultBuilder.Append('_');
            }
        }

        /// <summary>
        /// Gets the next non-mark character in the decomposed string.
        /// </summary>
        /// <param name="decomposed">The decomposed string to search.</param>
        /// <param name="startIndex">The index to start searching from.</param>
        /// <returns>The next non-mark character, or null character if none found.</returns>
        private static char GetNextNonMarkChar(string decomposed, int startIndex)
        {
            for (int i = startIndex; i < decomposed.Length; i++)
            {
                char c = decomposed[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    return c;
                }
            }
            return '\0'; // No next non-mark character found
        }

        /// <summary>
        /// Simplifies specific precomposed characters that should be converted to simpler forms.
        /// </summary>
        /// <param name="c">The character to simplify.</param>
        /// <returns>The simplified character.</returns>
        private static char SimplifyCharacter(char c)
        {
            // Handle specific precomposed characters that should be simplified
            switch (c)
            {
                case 'ø': // Latin small letter o with stroke
                    return 'o';
                case 'Ø': // Latin capital letter O with stroke
                    return 'O';
                case 'æ': // Latin small letter ae
                    return 'a';
                case 'Æ': // Latin capital letter AE
                    return 'A';
                case 'œ': // Latin small letter oe
                    return 'o';
                case 'Œ': // Latin capital letter OE
                    return 'O';
                default:
                    return c;
            }
        }

        /// <summary>
        /// Checks if a character is a Latin letter (A-Z, a-z).
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is a Latin letter, false otherwise.</returns>
        private static bool IsLatinLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Checks if a character is a Greek letter.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is a Greek letter, false otherwise.</returns>
        private static bool IsGreekLetter(char c)
        {
            return (c >= '\u0370' && c <= '\u03FF') || // Greek and Coptic block
                   (c >= '\u1F00' && c <= '\u1FFF');   // Greek Extended block
        }

        /// <summary>
        /// Checks if a character is a Cyrillic letter.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is a Cyrillic letter, false otherwise.</returns>
        private static bool IsCyrillicLetter(char c)
        {
            return (c >= '\u0400' && c <= '\u04FF') || // Cyrillic block
                   (c >= '\u0500' && c <= '\u052F') || // Cyrillic Supplement block
                   (c >= '\u2DE0' && c <= '\u2DFF') || // Cyrillic Extended-A block  
                   (c >= '\uA640' && c <= '\uA69F');   // Cyrillic Extended-B block
        }

        /// <summary>
        /// Checks if a character is a zero-width or bidirectional control character.
        /// These characters can be used for security attacks and should be removed.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character should be removed, false otherwise.</returns>
        private static bool IsZeroWidthOrBidirectional(char c)
        {
            // Zero-width characters
            if (c == '\u200B' || c == '\u200C' || c == '\u200D') // Zero-width space, zero-width non-joiner, zero-width joiner
                return true;
                
            // Bidirectional override characters
            if (c == '\u202A' || c == '\u202B' || c == '\u202C' || c == '\u202D' || c == '\u202E') // LRE, RLE, PDF, LRO, RLO
                return true;
                
            // Additional zero-width and bidirectional control characters for enhanced security
            if (c == '\u200E' || c == '\u200F') // Left-to-right mark, Right-to-left mark
                return true;
                
            if (c == '\u2066' || c == '\u2067' || c == '\u2068' || c == '\u2069') // First strong isolate, Left-to-right isolate, Right-to-left isolate, Pop directional isolate
                return true;
                
            // Zero-width no-break space (Byte Order Mark)
            if (c == '\uFEFF')
                return true;
                
            // Other format characters that should be removed
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.Format)
                return true;
                
            return false;
        }

        /// <summary>
        /// Gets the previous non-mark character from the StringBuilder.
        /// </summary>
        /// <param name="builder">The StringBuilder to search.</param>
        /// <returns>The previous non-mark character, or null character if none found.</returns>
        private static char GetPreviousNonMarkChar(StringBuilder builder)
        {
            for (int i = builder.Length - 1; i >= 0; i--)
            {
                char c = builder[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    return c;
                }
            }
            return '\0'; // No previous non-mark character found
        }
    }
}