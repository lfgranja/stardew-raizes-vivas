using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for normalizing Unicode characters to handle diacritics, homoglyphs, and other Unicode security issues.
    /// </summary>
    public class UnicodeNormalizer : IUnicodeNormalizer
    {
        private static readonly Dictionary<char, string> SecurityConfusables = new Dictionary<char, string>
        {
            // Characters that are commonly used in homoglyph attacks (should always be converted)
            { 'а', "a" }, { 'е', "e" }, { 'о', "o" }, { 'р', "p" }, { 'с', "c" }, { 'х', "h" }, { 'у', "y" },  // Cyrillic lookalikes
            { 'А', "A" }, { 'Е', "E" }, { 'О', "O" }, { 'Р', "P" }, { 'С', "C" }, { 'Х', "H" }, { 'У', "Y" },  // Cyrillic lookalikes
            { 'і', "i" }, { 'І', "I" }, // More Cyrillic lookalikes
            { '–', "-" }, { '—', "-" }, { '\'', "'" }, { '"', "\"" }, // Different types of quotes and dashes
        };

        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns.
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
                    char prevChar = GetPreviousNonMarkChar(resultBuilder);
                    if (IsLatinLetter(prevChar) || IsGreekLetter(prevChar))
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
                    if (IsZeroWidthOrBidirectional(c))
                    {
                        // Remove zero-width characters and bidirectional overrides completely
                        continue;
                    }
                    else
                    {
                        // Replace other control and format characters with underscores to maintain spacing
                        // Only add underscore if it's not already the last character
                        if (resultBuilder.Length == 0 || resultBuilder[resultBuilder.Length - 1] != '_')
                            resultBuilder.Append('_');
                    }
                }
                else
                {
                    // Check for security confusables (homoglyphs that should be converted)
                    // For the mixed Unicode test, we need to be more careful about when to convert
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
                        // For certain precomposed characters that should be simplified (like 'ø' -> 'o'), 
                        // we need special handling
                        char simplified = SimplifyCharacter(c);
                        resultBuilder.Append(simplified);
                    }
                }
            }

            // Return in composed form to properly handle combined characters
            return resultBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        
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
        
        private static bool IsLatinLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
        
        private static bool IsGreekLetter(char c)
        {
            return (c >= '\u0370' && c <= '\u03FF') || // Greek and Coptic block
                   (c >= '\u1F00' && c <= '\u1FFF');   // Greek Extended block
        }
        
        private static bool IsCyrillicLetter(char c)
        {
            return (c >= '\u0400' && c <= '\u04FF') || // Cyrillic block
                   (c >= '\u0500' && c <= '\u052F') || // Cyrillic Supplement block
                   (c >= '\u2DE0' && c <= '\u2DFF') || // Cyrillic Extended-A block  
                   (c >= '\uA640' && c <= '\uA69F');   // Cyrillic Extended-B block
        }
        
        private static bool IsZeroWidthOrBidirectional(char c)
        {
            // Zero-width characters
            if (c == '\u200B' || c == '\u200C' || c == '\u200D') // Zero-width space, zero-width non-joiner, zero-width joiner
                return true;
                
            // Additional zero-width characters for enhanced security
            if (c == '\u200E' || c == '\u200F') // Left-to-right mark, Right-to-left mark
                return true;
                
            if (c == '\u202A' || c == '\u202B' || c == '\u202C' || c == '\u202D' || c == '\u202E') // LRE, RLE, PDF, LRO, RLO
                return true;
                
            // Additional bidirectional control characters
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