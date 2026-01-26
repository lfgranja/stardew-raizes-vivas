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
    /// 4. Control characters: Removed completely
    /// 5. Precomposed characters: Simplified to base forms (e.g., 'ø' → 'o')
    /// </remarks>
    public class UnicodeNormalizationService : IUnicodeNormalizationService
    {
        private static readonly ImmutableDictionary<char, string> SecurityConfusables =
            ImmutableDictionary.CreateRange<char, string>(
                new Dictionary<char, string>
                {
                    // Characters that are commonly used in homoglyph attacks (always converted for security)
                    // Cyrillic lookalikes that can be used to disguise Latin text
                    { 'а', "a" },
                    { 'е', "e" },
                    { 'о', "o" },
                    { 'р', "p" },
                    { 'с', "c" },
                    { 'х', "x" },
                    { 'у', "y" }, // Cyrillic lowercase lookalikes
                    { 'А', "A" },
                    { 'Е', "E" },
                    { 'О', "O" },
                    { 'Р', "P" },
                    { 'С', "C" },
                    { 'Х', "X" },
                    { 'У', "Y" }, // Cyrillic uppercase lookalikes
                    { 'і', "i" },
                    { 'І', "I" }, // Additional Cyrillic lookalikes
                    // Extended Cyrillic lookalikes
                    { 'в', "b" },
                    { 'к', "k" },
                    { 'м', "m" },
                    { 'н', "n" },
                    { 'т', "t" }, // More Cyrillic lookalikes
                    { 'В', "B" },
                    { 'К', "K" },
                    { 'М', "M" },
                    { 'Н', "H" },
                    { 'Т', "T" }, // More Cyrillic lookalikes
                    // Greek lookalikes
                    { 'α', "a" },
                    { 'β', "b" },
                    { 'ε', "e" },
                    { 'ζ', "z" },
                    { 'η', "n" },
                    { 'ι', "i" },
                    { 'κ', "k" },
                    { 'μ', "m" },
                    { 'ν', "v" },
                    { 'ο', "o" },
                    { 'ρ', "p" },
                    { 'τ', "t" },
                    { 'υ', "y" },
                    { 'χ', "x" }, // Greek lowercase lookalikes
                    { 'Α', "A" },
                    { 'Β', "B" },
                    { 'Ε', "E" },
                    { 'Ζ', "Z" },
                    { 'Η', "N" },
                    { 'Ι', "I" },
                    { 'Κ', "K" },
                    { 'Μ', "M" },
                    { 'Ν', "N" },
                    { 'Ο', "O" },
                    { 'Ρ', "P" },
                    { 'Τ', "T" },
                    { 'Υ', "Y" },
                    { 'Χ', "X" }, // Greek uppercase lookalikes
                    // Other confusable characters
                    { '–', "-" },
                    { '—', "-" }, // Different types of dashes (en dash and em dash to regular hyphen)
                    { '‐', "-" },
                    { '‑', "-" }, // Hyphen and non-breaking hyphen to regular hyphen
                    { '′', "'" },
                    { '″', "\"" }, // Prime and double prime to regular quotes
                    { '‘', "'" },
                    { '’', "'" },
                    { '‚', "'" }, // Different single quotes to regular apostrophe
                    { '“', "\"" },
                    { '”', "\"" },
                    { '„', "\"" }, // Different double quotes to regular quotes
                    { '⁰', "0" },
                    { '¹', "1" },
                    { '²', "2" },
                    { '³', "3" },
                    { '⁴', "4" },
                    { '⁵', "5" },
                    { '⁶', "6" },
                    { '⁷', "7" },
                    { '⁸', "8" },
                    { '⁹', "9" }, // Superscript numbers
                    { '₀', "0" },
                    { '₁', "1" },
                    { '₂', "2" },
                    { '₃', "3" },
                    { '₄', "4" },
                    { '₅', "5" },
                    { '₆', "6" },
                    { '₇', "7" },
                    { '₈', "8" },
                    { '₉', "9" }, // Subscript numbers
                }
            );

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

            var sanitizedInput = SanitizeSurrogatePairs(input);
            var decomposed = sanitizedInput.Normalize(NormalizationForm.FormD);

            var resultBuilder = new StringBuilder();
            var lastBaseChar = '\0';

            var i = 0;
            while (i < decomposed.Length)
            {
                i = ProcessCharacter(decomposed, i, resultBuilder, ref lastBaseChar);
                i++;
            }

            var result = resultBuilder.ToString();
            return result.Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Processes a single character in the decomposed string.
        /// </summary>
        /// <param name="decomposed">The decomposed string being processed.</param>
        /// <param name="currentIndex">The current index in the string.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        /// <param name="lastBaseChar">The last base character encountered.</param>
        /// <returns>The new index to continue from (may be incremented for surrogate pairs).</returns>
        private static int ProcessCharacter(
            string decomposed,
            int currentIndex,
            StringBuilder resultBuilder,
            ref char lastBaseChar
        )
        {
            var c = decomposed[currentIndex];

            if (char.IsHighSurrogate(c))
            {
                return ProcessHighSurrogate(decomposed, currentIndex, resultBuilder);
            }

            if (char.IsLowSurrogate(c))
            {
                resultBuilder.Append('\uFFFD');
                return currentIndex;
            }

            return ProcessNonSurrogateCharacter(
                decomposed,
                currentIndex,
                resultBuilder,
                ref lastBaseChar
            );
        }

        /// <summary>
        /// Processes a high surrogate character.
        /// </summary>
        /// <param name="decomposed">The decomposed string being processed.</param>
        /// <param name="currentIndex">The current index in the string.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        /// <returns>The new index to continue from.</returns>
        private static int ProcessHighSurrogate(
            string decomposed,
            int currentIndex,
            StringBuilder resultBuilder
        )
        {
            if (
                currentIndex + 1 < decomposed.Length
                && char.IsLowSurrogate(decomposed[currentIndex + 1])
            )
            {
                resultBuilder.Append(decomposed[currentIndex]);
                resultBuilder.Append(decomposed[currentIndex + 1]);
                return currentIndex + 1;
            }

            resultBuilder.Append('\uFFFD');
            return currentIndex;
        }

        /// <summary>
        /// Processes a non-surrogate character.
        /// </summary>
        /// <param name="decomposed">The decomposed string being processed.</param>
        /// <param name="currentIndex">The current index in the string.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        /// <param name="lastBaseChar">The last base character encountered.</param>
        /// <returns>The same index (no increment needed).</returns>
        private static int ProcessNonSurrogateCharacter(
            string decomposed,
            int currentIndex,
            StringBuilder resultBuilder,
            ref char lastBaseChar
        )
        {
            var c = decomposed[currentIndex];
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                ProcessNonSpacingMark(c, lastBaseChar, resultBuilder);
                return currentIndex;
            }

            if (category == UnicodeCategory.Control || IsZeroWidthOrBidirectional(c))
            {
                return currentIndex;
            }

            lastBaseChar = c;
            ProcessRegularCharacter(c, decomposed, currentIndex, resultBuilder);
            return currentIndex;
        }

        /// <summary>
        /// Processes a non-spacing mark (diacritic).
        /// </summary>
        /// <param name="c">The non-spacing mark character.</param>
        /// <param name="lastBaseChar">The last base character encountered.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        private static void ProcessNonSpacingMark(
            char c,
            char lastBaseChar,
            StringBuilder resultBuilder
        )
        {
            var shouldRemoveDiacritic =
                lastBaseChar == '\0' || IsLatinLetter(lastBaseChar) || IsGreekLetter(lastBaseChar);
            if (!shouldRemoveDiacritic)
            {
                resultBuilder.Append(c);
            }
        }

        /// <summary>
        /// Processes a regular character (not a surrogate, control, or combining mark).
        /// </summary>
        /// <param name="c">The character to process.</param>
        /// <param name="decomposed">The decomposed string being processed.</param>
        /// <param name="index">The index of the character.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        private static void ProcessRegularCharacter(
            char c,
            string decomposed,
            int index,
            StringBuilder resultBuilder
        )
        {
            if (SecurityConfusables.TryGetValue(c, out var replacement))
            {
                var shouldConvert = ShouldConvertConfusable(c, decomposed, index);
                resultBuilder.Append(shouldConvert ? replacement : c.ToString());
            }
            else
            {
                var simplified = SimplifyCharacter(c);
                resultBuilder.Append(simplified);
            }
        }

        /// <summary>
        /// Sanitizes the input string by replacing invalid surrogate pairs with replacement characters
        /// before the main normalization process to prevent exceptions.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <returns>A string with invalid surrogate pairs replaced by replacement characters.</returns>
        private static string SanitizeSurrogatePairs(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var resultBuilder = new StringBuilder();
            var i = 0;

            while (i < input.Length)
            {
                var c = input[i];

                if (char.IsHighSurrogate(c))
                {
                    i = ProcessHighSurrogateInSanitization(input, i, resultBuilder);
                }
                else if (char.IsLowSurrogate(c))
                {
                    resultBuilder.Append('\uFFFD');
                    i++;
                }
                else
                {
                    resultBuilder.Append(c);
                    i++;
                }
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Processes a high surrogate during sanitization.
        /// </summary>
        /// <param name="input">The input string being processed.</param>
        /// <param name="currentIndex">The current index.</param>
        /// <param name="resultBuilder">The string builder for the result.</param>
        /// <returns>The new index to continue from.</returns>
        private static int ProcessHighSurrogateInSanitization(
            string input,
            int currentIndex,
            StringBuilder resultBuilder
        )
        {
            if (currentIndex + 1 < input.Length && char.IsLowSurrogate(input[currentIndex + 1]))
            {
                resultBuilder.Append(input[currentIndex]);
                resultBuilder.Append(input[currentIndex + 1]);
                return currentIndex + 2;
            }

            resultBuilder.Append('\uFFFD');
            return currentIndex + 1;
        }

        /// <summary>
        /// Simplifies specific precomposed characters that should be simplified.
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
            return (c >= '\u0370' && c <= '\u03FF')
                || // Greek and Coptic block
                (c >= '\u1F00' && c <= '\u1FFF'); // Greek Extended block
        }

        /// <summary>
        /// Checks if a character is a Cyrillic letter.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is a Cyrillic letter, false otherwise.</returns>
        private static bool IsCyrillicLetter(char c)
        {
            return (c >= '\u0400' && c <= '\u04FF')
                || // Cyrillic block
                (c >= '\u0500' && c <= '\u052F')
                || // Cyrillic Supplement block
                (c >= '\u2DE0' && c <= '\u2DFF')
                || // Cyrillic Extended-A block
                (c >= '\uA640' && c <= '\uA69F'); // Cyrillic Extended-B block
        }

        /// <summary>
        /// Checks if a character is a zero-width or bidirectional control character.
        /// These characters can be used for security attacks and should be removed.
        /// Added missing zero-width characters: U+200B (ZERO WIDTH SPACE), U+20C (ZERO WIDTH NON-JOINER), U+200D (ZERO WIDTH JOINER)
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character should be removed, false otherwise.</returns>
        private static bool IsZeroWidthOrBidirectional(char c)
        {
            // Zero-width characters - added the missing U+200B, U+200C, U+200D
            if (c == '\u200B' || c == '\u200C' || c == '\u200D') // ZERO WIDTH SPACE, ZERO WIDTH NON-JOINER, ZERO WIDTH JOINER
                return true;

            if (c == '\u202A' || c == '\u202B' || c == '\u202C' || c == '\u202D' || c == '\u202E') // LRE, RLE, PDF, LRO, RLO
                return true;

            // Additional zero-width and bidirectional control characters for enhanced security
            if (c == '\u202F') // Narrow no-break space
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
            var prevIndex = FindPreviousNonMark(text, index);
            var nextIndex = FindNextNonMark(text, index);
            var isCyrillicConfusable = IsCyrillicLookalike(c);
            var isGreekConfusable = IsGreekLookalike(c);

            if (
                ShouldPreserveAtBoundary(
                    prevIndex,
                    nextIndex,
                    text,
                    isCyrillicConfusable,
                    isGreekConfusable
                )
            )
            {
                return false;
            }

            if (
                ShouldPreserveInMiddle(
                    prevIndex,
                    nextIndex,
                    text,
                    isCyrillicConfusable,
                    isGreekConfusable,
                    index
                )
            )
            {
                return false;
            }

            if (ShouldPreserveGreekWithNeighbor(prevIndex, nextIndex, text, isGreekConfusable))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a confusable character at a string boundary should be preserved.
        /// </summary>
        private static bool ShouldPreserveAtBoundary(
            int prevIndex,
            int nextIndex,
            string text,
            bool isCyrillicConfusable,
            bool isGreekConfusable
        )
        {
            if (prevIndex == -1)
            {
                return ShouldPreserveAtStart(
                    nextIndex,
                    text,
                    isCyrillicConfusable,
                    isGreekConfusable
                );
            }

            if (nextIndex == -1)
            {
                return ShouldPreserveAtEnd(
                    prevIndex,
                    text,
                    isCyrillicConfusable,
                    isGreekConfusable
                );
            }

            return false;
        }

        /// <summary>
        /// Checks if a confusable character at the start of the string should be preserved.
        /// </summary>
        private static bool ShouldPreserveAtStart(
            int nextIndex,
            string text,
            bool isCyrillicConfusable,
            bool isGreekConfusable
        )
        {
            if (nextIndex == -1)
                return false;

            if (isCyrillicConfusable && IsCyrillicLetter(text[nextIndex]))
                return true;

            if (isGreekConfusable && IsGreekLetter(text[nextIndex]))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a confusable character at the end of the string should be preserved.
        /// </summary>
        private static bool ShouldPreserveAtEnd(
            int prevIndex,
            string text,
            bool isCyrillicConfusable,
            bool isGreekConfusable
        )
        {
            if (isCyrillicConfusable && IsCyrillicLetter(text[prevIndex]))
                return true;

            if (isGreekConfusable && IsGreekLetter(text[prevIndex]))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a confusable character in the middle of the string should be preserved.
        /// </summary>
        private static bool ShouldPreserveInMiddle(
            int prevIndex,
            int nextIndex,
            string text,
            bool isCyrillicConfusable,
            bool isGreekConfusable,
            int index
        )
        {
            // Guard against invalid indices
            if (
                prevIndex < 0
                || nextIndex < 0
                || prevIndex >= text.Length
                || nextIndex >= text.Length
            )
                return false;

            var prevIsCyrillic = IsCyrillicLetter(text[prevIndex]);
            var nextIsCyrillic = IsCyrillicLetter(text[nextIndex]);
            var prevIsGreek = IsGreekLetter(text[prevIndex]);
            var nextIsGreek = IsGreekLetter(text[nextIndex]);

            if (isCyrillicConfusable && prevIsCyrillic && nextIsCyrillic)
                return true;

            if (isGreekConfusable && prevIsGreek && nextIsGreek)
                return true;

            if (isCyrillicConfusable && nextIsCyrillic)
                return true;

            if (isCyrillicConfusable && HasMultipleCyrillicNeighbors(text, index))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a Greek confusable character with at least one Greek neighbor should be preserved.
        /// </summary>
        private static bool ShouldPreserveGreekWithNeighbor(
            int prevIndex,
            int nextIndex,
            string text,
            bool isGreekConfusable
        )
        {
            if (!isGreekConfusable)
                return false;

            if (prevIndex != -1 && IsGreekLetter(text[prevIndex]))
                return true;

            if (nextIndex != -1 && IsGreekLetter(text[nextIndex]))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if there are multiple Cyrillic characters in the vicinity of the index.
        /// </summary>
        private static bool HasMultipleCyrillicNeighbors(string text, int index)
        {
            var cyrillicCount = 0;
            var start = System.Math.Max(0, index - 5);
            var end = System.Math.Min(text.Length, index + 6);

            for (var i = start; i < end; i++)
            {
                if (i != index && IsCyrillicLetter(text[i]))
                {
                    cyrillicCount++;
                }
            }

            return cyrillicCount >= 2;
        }

        /// <summary>
        /// Checks if a character is a Cyrillic lookalike that maps to a Latin equivalent
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if the character is a Cyrillic lookalike</returns>
        private static bool IsCyrillicLookalike(char c)
        {
            return SecurityConfusables.ContainsKey(c)
                && (IsCyrillicLetter(c) || c == 'і' || c == 'І'); // Additional Cyrillic lookalikes
        }

        /// <summary>
        /// Checks if a character is a Greek lookalike that maps to a Latin equivalent
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if the character is a Greek lookalike</returns>
        private static bool IsGreekLookalike(char c)
        {
            return SecurityConfusables.ContainsKey(c) && IsGreekLetter(c);
        }

        /// <summary>
        /// Finds the previous non-combining mark character in the text.
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <param name="startIndex">The starting index to search backwards from</param>
        /// <returns>The index of the previous non-combining mark character, or -1 if not found</returns>
        private static int FindPreviousNonMark(string text, int startIndex)
        {
            for (var i = startIndex - 1; i >= 0; i--)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(text[i]);
                if (
                    category != UnicodeCategory.NonSpacingMark
                    && category != UnicodeCategory.SpacingCombiningMark
                    && category != UnicodeCategory.EnclosingMark
                )
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the next non-combining mark character in the text.
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <param name="startIndex">The starting index to search forwards from</param>
        /// <returns>The index of the next non-combining mark character, or -1 if not found</returns>
        private static int FindNextNonMark(string text, int startIndex)
        {
            for (var i = startIndex + 1; i < text.Length; i++)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(text[i]);
                if (
                    category != UnicodeCategory.NonSpacingMark
                    && category != UnicodeCategory.SpacingCombiningMark
                    && category != UnicodeCategory.EnclosingMark
                )
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
