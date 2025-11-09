using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for normalizing Unicode characters to handle diacritics, homoglyphs, and other Unicode security issues.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
    /// </summary>
    /// <remarks>
    /// Conversion Rules:
    /// 1. Diacritics: Removed from Latin and Greek letters, preserved for other scripts
    /// 2. Security Confusables: Cyrillic lookalikes are converted to Latin equivalents unless they appear in legitimate non-Latin script contexts
    /// 3. Zero-width and bidirectional characters: Removed completely for security
    /// 4. Control characters: Removed completely to avoid creating false word boundaries
    /// 5. Precomposed characters: Simplified to base forms (e.g., 'ø' → 'o')
    /// </remarks>
    public class UnicodeNormalizationService : IUnicodeNormalizationService
    {
        private static readonly ImmutableDictionary<char, string> SecurityConfusables = ImmutableDictionary.CreateRange<char, string>(new Dictionary<char, string>
        {
            // Characters that are commonly used in homoglyph attacks (always converted for security)
            // Cyrillic lookalikes that can be used to disguise Latin text
            { 'а', "a" }, { 'е', "e" }, { 'о', "o" }, { 'р', "p" }, { 'с', "c" }, { 'х', "h" }, { 'у', "y" },  // Cyrillic lowercase lookalikes
            { 'А', "A" }, { 'Е', "E" }, { 'О', "O" }, { 'Р', "P" }, { 'С', "C" }, { 'Х', "H" }, { 'У', "Y" }, // Cyrillic uppercase lookalikes
            { 'і', "i" }, { 'І', "I" }, // Additional Cyrillic lookalikes
            // Other confusable characters
            { '–', "-" }, { '—', "-" }, // Different types of dashes (en dash and em dash to regular hyphen)
        });
        
        
        /// <summary>
        /// Normalizes Unicode characters by handling diacritics, homoglyphs, and other Unicode security concerns.
        /// Security-focused normalization that converts potentially deceptive characters while preserving legitimate Unicode.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>The normalized string.</returns>
        public string? Normalize(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Apply decomposition normalization to separate base characters from diacritics first
            string decomposed = input.Normalize(NormalizationForm.FormD);
            
            var resultBuilder = new StringBuilder();
            char lastBaseChar = '\0'; // Track the last base character for diacritic processing

            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];

                // Handle surrogate pairs (needed for emojis and other characters outside BMP)
                if (char.IsHighSurrogate(c) && i + 1 < decomposed.Length && char.IsLowSurrogate(decomposed[i + 1]))
                {
                    // For surrogate pairs (like emojis), preserve them as-is
                    resultBuilder.Append(c);
                    resultBuilder.Append(decomposed[i + 1]);
                    // For surrogate pairs, use the high surrogate as the base character (imperfect but functional)
                    lastBaseChar = c;
                    i++; // Skip low surrogate since we've processed it
                    continue;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category == UnicodeCategory.NonSpacingMark)
                {
                    // This is a combining mark (like a diacritic)
                    // For security, remove diacritics from Latin and Greek letters
                    // But preserve diacritics for other scripts like Hebrew, Arabic, etc.
                    // Use the last known base character to determine if diacritic should be removed
                    if (IsLatinLetter(lastBaseChar) || IsGreekLetter(lastBaseChar))
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
                    // Check if this is a zero-width or bidirectional character that should be removed completely
                    if (IsZeroWidthOrBidirectional(c))
                    {
                        // Remove zero-width characters and bidirectional overrides completely
                        continue;
                    }
                    else
                    {
                        // Remove other control and format characters completely to avoid creating false word boundaries
                        // This prevents format characters from being replaced with underscores which could alter string semantics
                        continue;
                    }
                }
                else
                {
                    // Update the last base character for any non-combining, non-control/format character
                    lastBaseChar = c;
                    
                    // Check for security confusables (homoglyphs that should be converted)
                    // Apply context-aware conversion for security homoglyphs
                    // The goal is to convert confusable characters when they're used to disguise other scripts
                    if (SecurityConfusables.TryGetValue(c, out var replacement))
                    {
                        // For context-aware conversion, we need to check if this confusable character is being used in a context
                        // where it might be disguising another script. If it's surrounded by other non-Latin characters that are
                        // part of the same script (like Cyrillic), we should preserve it.
                        
                        bool shouldConvert = ShouldConvertConfusable(c, decomposed, i);
                        
                        if (shouldConvert)
                        {
                            // Convert confusable characters when they're being used to disguise other scripts
                            resultBuilder.Append(replacement);
                        }
                        else
                        {
                            // Preserve the original character when it's in a legitimate script context
                            resultBuilder.Append(c);
                        }
                    }
                    else
                    {
                        // Special handling for non-confusable characters
                        string simplified = SimplifyCharacter(c);
                        resultBuilder.Append(simplified);
                    }
                }
            }

            // Return in composed form to properly handle combined characters
            return resultBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Simplifies specific precomposed characters that should be converted to simpler forms.
        /// </summary>
        /// <param name="c">The character to simplify.</param>
        /// <returns>The simplified character or string.</returns>
        private static string SimplifyCharacter(char c)
        {
            // Handle specific precomposed characters that should be simplified
            switch (c)
            {
                case 'ø': // Latin small letter o with stroke
                    return "o";
                case 'Ø': // Latin capital letter O with stroke
                    return "O";
                case 'æ': // Latin small letter ae
                    return "ae";
                case 'Æ': // Latin capital letter AE
                    return "AE";
                case 'œ': // Latin small letter oe
                    return "oe";
                case 'Œ': // Latin capital letter OE
                    return "OE";
                default:
                    return c.ToString();
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
        /// Determines whether a confusable character should be converted based on its context.
        /// </summary>
        /// <param name="c">The confusable character</param>
        /// <param name="text">The full text being processed</param>
        /// <param name="index">The index of the character in the text</param>
        /// <returns>True if the character should be converted, false if it should be preserved</returns>
        private static bool ShouldConvertConfusable(char c, string text, int index)
        {
            // Check if this character is part of a legitimate non-Latin script context
            // For example, if a Cyrillic 'е' is surrounded by other Cyrillic characters, preserve it
            // But if it's surrounded by Latin characters, convert it (as it's likely a homoglyph attack)
            
            bool prevIsCyrillic = index > 0 && IsCyrillicLetter(text[index - 1]);
            bool nextIsCyrillic = index < text.Length - 1 && IsCyrillicLetter(text[index + 1]);
            
            // If the character is surrounded by Cyrillic letters on BOTH sides, preserve it as part of legitimate Cyrillic text
            if (prevIsCyrillic && nextIsCyrillic)
            {
                return false; // Don't convert - it's part of legitimate Cyrillic text
            }
            
            // If only one neighbor is Cyrillic or if there are no Cyrillic neighbors on both sides,
            // convert the confusable character to prevent spoofing at script boundaries
            // This strengthens security by requiring BOTH sides to be Cyrillic to preserve the character
            return true;
        }
    }
}