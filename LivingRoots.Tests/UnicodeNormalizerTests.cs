using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;
using LivingRoots.Domain;

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
            string input = "café";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            string input = "tеst"; // Cyrillic 'е'

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void Normalize_WithCombinedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "cafe\u0301"; // 'e' with combining acute accent

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithPrecomposedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "café";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithZeroWidthUnicodeCharacters_RemovesProperly()
        {
            // Arrange
            string input = "test\u200Bzwsp\u200Czwnj\u200Dzwj";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("testzwspzwnjzwj", result);
        }

        [Fact]
        public void Normalize_WithHebrewText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test שלום";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test שלום", result);
        }

        [Fact]
        public void Normalize_WithArabicText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test كتاب";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test كتاب", result);
        }

        [Fact]
        public void Normalize_WithChineseText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test 你好";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("test 你好", result);
        }

        [Fact]
        public void Normalize_WithEmoji_PreservesValidUnicode()
        {
            // Arrange
            var emoji = char.ConvertFromUtf32(0x1F600);
            string input = $"test {emoji} smile";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal($"test {emoji} smile", result);
        }

        [Fact]
        public void Normalize_WithMixedUnicodeAndDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "café тест naïve";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("cafe тест naive", result);
        }

        [Fact]
        public void Normalize_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            string input = "a\u0300\u0301b"; // 'a' with grave and acute accents

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("ab", result);
        }

        [Fact]
        public void Normalize_WithGreekTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "μέντι";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("μεντι", result);
        }

        [Fact]
        public void Normalize_WithTurkishTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "göçmen";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("gocmen", result);
        }

        [Fact]
        public void Normalize_WithThaiTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "คํา";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("คํา", result);
        }

        [Fact]
        public void Normalize_WithDevanagariText_PreservesValidUnicode()
        {
            // Arrange
            string input = "नमस्ते";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("नमस्ते", result);
        }

        [Fact]
        public void Normalize_WithMultipleNormalizationForms_HandlesProperly()
        {
            // Arrange
            string nfcForm = "café";
            string nfdForm = "cafe\u0301";

            // Act
            string resultNfc = _unicodeNormalizationService.Normalize(nfcForm)!;
            string resultNfd = _unicodeNormalizationService.Normalize(nfdForm)!;

            // Assert
            Assert.Equal("cafe", resultNfc);
            Assert.Equal("cafe", resultNfd);
        }

        [Fact]
        public void Normalize_WithHomoglyphAndDiacriticsTogether_HandlesProperly()
        {
            // Arrange
            string input1 = "tеst\u0301"; // Cyrillic 'е' with combining acute
            string input2 = "cafe\u0435\u0301"; // 'e' with Cyrillic 'e' and combining acute

            // Act
            string result1 = _unicodeNormalizationService.Normalize(input1)!;
            string result2 = _unicodeNormalizationService.Normalize(input2)!;

            // Assert
            Assert.Equal("test", result1);
            Assert.Equal("cafeé", result2);
        }

        [Fact]
        public void Normalize_WithControlAndFormatUnicodeChars_HandlesProperly()
        {
            // Arrange
            string input1 = "test\u200Estart"; // Left-to-right mark
            string input2 = "test\u200Fend"; // Right-to-left mark

            // Act
            string result1 = _unicodeNormalizationService.Normalize(input1)!;
            string result2 = _unicodeNormalizationService.Normalize(input2)!;

            // Assert
            Assert.Equal("teststart", result1);
            Assert.Equal("testend", result2);
        }

        [Fact]
        public void Normalize_WithVeryLongUnicodeString_HandlesProperly()
        {
            // Arrange
            string input = new string('a', 100) + "café" + new string('b', 100) + "naïve" + new string('c', 100);

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            string expected = new string('a', 100) + "cafe" + new string('b', 100) + "naive" + new string('c', 100);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_WithPrecomposedCharacters_SimplifiesProperly()
        {
            // Arrange
            string input = "ØÆŒ"; // Precomposed characters

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("OAEOE", result); // Updated to reflect ligature expansion: Ø->O, Æ->AE, Œ->OE
        }

        [Fact]
        public void Normalize_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Normalize_WithNullString_ReturnsNullString()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Normalize_WithCyrillicLookalikes_ConvertsToLatin()
        {
            // Arrange
            string input1 = "а"; // Cyrillic 'a'
            string input2 = "е"; // Cyrillic 'e'
            string input3 = "о"; // Cyrillic 'o'
            string input4 = "р"; // Cyrillic 'p'
            string input5 = "с"; // Cyrillic 'c'
            string input6 = "х"; // Cyrillic 'x' (was incorrectly mapped to 'h', now correctly mapped to 'x')
            string input7 = "у"; // Cyrillic 'y'

            // Act
            string result1 = _unicodeNormalizationService.Normalize(input1)!;
            string result2 = _unicodeNormalizationService.Normalize(input2)!;
            string result3 = _unicodeNormalizationService.Normalize(input3)!;
            string result4 = _unicodeNormalizationService.Normalize(input4)!;
            string result5 = _unicodeNormalizationService.Normalize(input5)!;
            string result6 = _unicodeNormalizationService.Normalize(input6)!;
            string result7 = _unicodeNormalizationService.Normalize(input7)!;

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
            string input1 = "test–dash"; // en dash
            string input2 = "test—dash"; // em dash
            string input3 = "test'dquote"; // single quote
            string input4 = "test\"dquote"; // double quote

            // Act
            string result1 = _unicodeNormalizationService.Normalize(input1)!;
            string result2 = _unicodeNormalizationService.Normalize(input2)!;
            string result3 = _unicodeNormalizationService.Normalize(input3)!;
            string result4 = _unicodeNormalizationService.Normalize(input4)!;

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
            string input = "тест"; // Contains Cyrillic 'е' in the middle of a Cyrillic word - should be preserved
            string expected = "тест"; // Should remain as Cyrillic, not converted to "тecт"

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_WithLigatures_ExpandsToTwoCharacterEquivalents()
        {
            // Arrange
            string input1 = "Cæsar"; // Contains æ ligature
            string input2 = "Œdipus"; // Contains Œ ligature
            string input3 = "coöperation"; // Contains ö with diaeresis (not a ligature, should remain as is)
            string input4 = "naïve"; // Contains ï with diaeresis (not a ligature, should remain as is)
            string input5 = "æon"; // Contains æ ligature at the beginning
            string input6 = "cœur"; // Contains œ ligature

            // Act
            string result1 = _unicodeNormalizationService.Normalize(input1)!;
            string result2 = _unicodeNormalizationService.Normalize(input2)!;
            string result3 = _unicodeNormalizationService.Normalize(input3)!;
            string result4 = _unicodeNormalizationService.Normalize(input4)!;
            string result5 = _unicodeNormalizationService.Normalize(input5)!;
            string result6 = _unicodeNormalizationService.Normalize(input6)!;

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
            string input = "test\u200Eformat\u200Ftest\u0001control\u0002test"; // LRM, RLM, control chars

            // Act
            string? result = _unicodeNormalizationService.Normalize(input);

            // Assert - format and control characters should be removed completely, not replaced with underscores
            Assert.Equal("testformattestcontroltest", result);
        }
    }
}
