using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    public class UnicodeNormalizerTests
    {
        private readonly UnicodeNormalizationService _unicodeNormalizationService;

        public UnicodeNormalizerTests()
        {
            // Create a real UnicodeNormalizationService instance for testing
            _unicodeNormalizationService = new UnicodeNormalizationService();
        }

        [Fact]
        public void SecurityConfusables_DoesNotContainRedundantMappings()
        {
            // Test that the SecurityConfusables dictionary does not contain mappings
            // where a character maps to itself (redundant mappings)
            var confusablesField = typeof(UnicodeNormalizationService)
                .GetField("SecurityConfusables", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (confusablesField == null)
            {
                Assert.Fail("SecurityConfusables field not found");
                return;
            }

            var confusables = confusablesField.GetValue(null) as System.Collections.Generic.IDictionary<char, string>;

            // Check for any mappings where the value is a single character and it's the same as the key
            if (confusables != null)
            {
                foreach (var kvp in confusables)
                {
                    if (kvp.Value.Length == 1 && kvp.Value[0] == kvp.Key)
                    {
                        Assert.Fail($"Found redundant mapping: {{ '{kvp.Key}', \"{kvp.Value}\" }} maps character to itself");
                    }
                }
            }
        }

        [Fact]
        public void Normalize_WithUnicodeDiacritics_RemovesAccentsProperly()
        {
            // Arrange
            var input = "café";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            var input = "tеst"; // Cyrillic 'е'

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void Normalize_WithCombinedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            var input = "cafe\u0301"; // 'e' with combining acute accent

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithZeroWidthUnicodeCharacters_RemovesProperly()
        {
            // Arrange
            var input = "test\u200Bzwsp\u200Czwnj\u200Dzwj";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("testzwspzwnjzwj", result);
        }

        [Fact]
        public void Normalize_WithHebrewText_PreservesValidUnicode()
        {
            // Arrange
            var input = "test שלום";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test שלום", result);
        }

        [Fact]
        public void Normalize_WithArabicText_PreservesValidUnicode()
        {
            // Arrange
            var input = "test كتاب";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test كتاب", result);
        }

        [Fact]
        public void Normalize_WithChineseText_PreservesValidUnicode()
        {
            // Arrange
            var input = "test 你好";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test 你好", result);
        }

        [Fact]
        public void Normalize_WithEmoji_PreservesValidUnicode()
        {
            // Arrange
            var emoji = char.ConvertFromUtf32(0x1F600);
            var input = $"test {emoji} smile";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal($"test {emoji} smile", result);
        }

        [Fact]
        public void Normalize_WithMixedUnicodeAndDiacritics_RemovesAccents()
        {
            // Arrange
            var input = "café тест naïve";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe тест naive", result);
        }

        [Fact]
        public void Normalize_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            var input = "a\u0300\u0301b"; // 'a' with grave and acute accents

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("ab", result);
        }

        [Fact]
        public void Normalize_WithGreekTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var input = "μέντι";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("μεντι", result);
        }

        [Fact]
        public void Normalize_WithTurkishTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var input = "göçmen";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("gocmen", result);
        }

        [Fact]
        public void Normalize_WithThaiTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var input = "คํา";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("คํา", result);
        }

        [Fact]
        public void Normalize_WithDevanagariText_PreservesValidUnicode()
        {
            // Arrange
            var input = "नमस्ते";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("नमस्ते", result);
        }

        [Fact]
        public void Normalize_WithMultipleNormalizationForms_HandlesProperly()
        {
            // Arrange
            var nfcForm = "café";
            var nfdForm = "cafe\u0301";

            // Act
            var resultNfc = _unicodeNormalizationService.Normalize(nfcForm)!;
            var resultNfd = _unicodeNormalizationService.Normalize(nfdForm)!;

            // Assert
            Assert.Equal("cafe", resultNfc);
            Assert.Equal("cafe", resultNfd);
        }

        [Fact]
        public void Normalize_WithHomoglyphAndDiacriticsTogether_HandlesProperly()
        {
            // Arrange
            var input1 = "tеst\u0301"; // Cyrillic 'е' with combining acute
            var input2 = "cafe\u0435\u0301"; // 'e' with Cyrillic 'e' and combining acute

            // Act
            var result1 = _unicodeNormalizationService.Normalize(input1)!;
            var result2 = _unicodeNormalizationService.Normalize(input2)!;

            // Assert
            Assert.Equal("test", result1);
            Assert.Equal("cafeé", result2);
        }

        [Fact]
        public void Normalize_WithControlAndFormatUnicodeChars_HandlesProperly()
        {
            // Arrange
            var input1 = "test\u200Estart"; // Left-to-right mark
            var input2 = "test\u200Fend"; // Right-to-left mark

            // Act
            var result1 = _unicodeNormalizationService.Normalize(input1)!;
            var result2 = _unicodeNormalizationService.Normalize(input2)!;

            // Assert
            Assert.Equal("teststart", result1);
            Assert.Equal("testend", result2);
        }

        [Fact]
        public void Normalize_WithVeryLongUnicodeString_HandlesProperly()
        {
            // Arrange
            var input = new string('a', 100) + "café" + new string('b', 100) + "naïve" + new string('c', 100);

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            var expected = new string('a', 100) + "cafe" + new string('b', 100) + "naive" + new string('c', 100);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_WithPrecomposedCharacters_SimplifiesProperly()
        {
            // Arrange
            var input = "ØÆŒ"; // Precomposed characters

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("OAEOE", result); // Updated to reflect ligature expansion: Ø->O, Æ->AE, Œ->OE
        }

        [Fact]
        public void Normalize_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var input = "";

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Normalize_WithNullString_ReturnsNullString()
        {
            // Arrange
            string? input = null;

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Normalize_WithCyrillicLookalikes_ConvertsToLatin()
        {
            // Arrange
            var input1 = "а"; // Cyrillic 'a'
            var input2 = "е"; // Cyrillic 'e'
            var input3 = "о"; // Cyrillic 'o'
            var input4 = "р"; // Cyrillic 'p'
            var input5 = "с"; // Cyrillic 'c'
            var input6 = "х"; // Cyrillic 'x' (was incorrectly mapped to 'h', now correctly mapped to 'x')
            var input7 = "у"; // Cyrillic 'y'

            // Act
            var result1 = _unicodeNormalizationService.Normalize(input1)!;
            var result2 = _unicodeNormalizationService.Normalize(input2)!;
            var result3 = _unicodeNormalizationService.Normalize(input3)!;
            var result4 = _unicodeNormalizationService.Normalize(input4)!;
            var result5 = _unicodeNormalizationService.Normalize(input5)!;
            var result6 = _unicodeNormalizationService.Normalize(input6)!;
            var result7 = _unicodeNormalizationService.Normalize(input7)!;

            // Assert
            Assert.Equal("a", result1);
            Assert.Equal("e", result2);
            Assert.Equal("o", result3);
            Assert.Equal("p", result4);
            Assert.Equal("c", result5);
            Assert.Equal("x", result6); // Updated from "h" to "x" - Cyrillic 'х' should map to Latin 'x', not 'h'
            Assert.Equal("y", result7);
        }

        [Fact]
        public void Normalize_WithDifferentDashesAndQuotes_ConvertsToStandard()
        {
            // Arrange
            var input1 = "test–dash"; // en dash
            var input2 = "test—dash"; // em dash
            var input3 = "test'dquote"; // single quote
            var input4 = "test\"dquote"; // double quote

            // Act
            var result1 = _unicodeNormalizationService.Normalize(input1)!;
            var result2 = _unicodeNormalizationService.Normalize(input2)!;
            var result3 = _unicodeNormalizationService.Normalize(input3)!;
            var result4 = _unicodeNormalizationService.Normalize(input4)!;

            // Assert
            Assert.Equal("test-dash", result1);
            Assert.Equal("test-dash", result2);
            Assert.Equal("test'dquote", result3);
            Assert.Equal("test\"dquote", result4);
        }

        [Fact]
        public void Normalize_WithCyrillicWordContainingConfusableInMiddle_PreservesCyrillic()
        {
            // Arrange
            var input = "тест"; // Contains Cyrillic 'е' in the middle of a Cyrillic word - should be preserved
            var expected = "тест"; // Should remain as Cyrillic, not converted to "тecт"

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_WithLigatures_ExpandsToTwoCharacterEquivalents()
        {
            // Arrange
            var input1 = "Cæsar"; // Contains æ ligature
            var input2 = "Œdipus"; // Contains Œ ligature
            var input3 = "coöperation"; // Contains ö with diaeresis (not a ligature, should remain as is)
            var input4 = "naïve"; // Contains ï with diaeresis (not a ligature, should remain as is)
            var input5 = "æon"; // Contains æ ligature at the beginning
            var input6 = "cœur"; // Contains œ ligature

            // Act
            var result1 = _unicodeNormalizationService.Normalize(input1)!;
            var result2 = _unicodeNormalizationService.Normalize(input2)!;
            var result3 = _unicodeNormalizationService.Normalize(input3)!;
            var result4 = _unicodeNormalizationService.Normalize(input4)!;
            var result5 = _unicodeNormalizationService.Normalize(input5)!;
            var result6 = _unicodeNormalizationService.Normalize(input6)!;

            // Assert
            Assert.Equal("Caesar", result1); // æ should expand to "ae"
            Assert.Equal("OEdipus", result2); // Œ should expand to "OE"
            Assert.Equal("cooperation", result3); // ö diaeresis should become o after diacritic removal
            Assert.Equal("naive", result4); // ï diaeresis should become i after diacritic removal
            Assert.Equal("aeon", result5); // æ should expand to "ae"
            Assert.Equal("coeur", result6); // œ should expand to "oe"
        }

        [Fact]
        public void Normalize_WithFormatAndControlCharacters_RemovesInsteadOfReplacingWithUnderscores()
        {
            // Arrange
            var input = "test\u200Eformat\u200Ftest\u0001control\u0002test"; // LRM, RLM, control chars

            // Act
            var result = _unicodeNormalizationService.Normalize(input);

            // Assert - format and control characters should be removed completely, not replaced with underscores
            Assert.Equal("testformattestcontroltest", result);
        }
    }
}
