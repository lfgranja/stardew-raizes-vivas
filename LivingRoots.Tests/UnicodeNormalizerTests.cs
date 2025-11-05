using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class UnicodeNormalizerTests
    {
        private readonly UnicodeNormalizer _unicodeNormalizer;

        public UnicodeNormalizerTests()
        {
            // Create a real UnicodeNormalizer instance for testing
            _unicodeNormalizer = new UnicodeNormalizer();
        }

        [Fact]
        public void Normalize_WithUnicodeDiacritics_RemovesAccentsProperly()
        {
            // Arrange
            string input = "café";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            string input = "tеst"; // Cyrillic 'е'

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void Normalize_WithCombinedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "cafe\u0301"; // 'e' with combining acute accent

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithPrecomposedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "café";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("cafe", result);
        }

        [Fact]
        public void Normalize_WithZeroWidthUnicodeCharacters_RemovesProperly()
        {
            // Arrange
            string input = "test\u200Bzwsp\u200Czwnj\u200Dzwj";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("testzwspzwnjzwj", result);
        }

        [Fact]
        public void Normalize_WithHebrewText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test שלום";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("test שלום", result);
        }

        [Fact]
        public void Normalize_WithArabicText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test كتاب";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("test كتاب", result);
        }

        [Fact]
        public void Normalize_WithChineseText_PreservesValidUnicode()
        {
            // Arrange
            string input = "test 你好";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

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
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal($"test {emoji} smile", result);
        }

        [Fact]
        public void Normalize_WithMixedUnicodeAndDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "café тест naïve";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("cafe тecт naive", result);
        }

        [Fact]
        public void Normalize_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            string input = "a\u0300\u0301b"; // 'a' with grave and acute accents

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("ab", result);
        }

        [Fact]
        public void Normalize_WithGreekTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "μέντι";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("μεντι", result);
        }

        [Fact]
        public void Normalize_WithTurkishTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "göçmen";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("gocmen", result);
        }

        [Fact]
        public void Normalize_WithThaiTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            string input = "คํา";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("คํา", result);
        }

        [Fact]
        public void Normalize_WithDevanagariText_PreservesValidUnicode()
        {
            // Arrange
            string input = "नमस्ते";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

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
            string resultNfc = _unicodeNormalizer.Normalize(nfcForm);
            string resultNfd = _unicodeNormalizer.Normalize(nfdForm);

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
            string result1 = _unicodeNormalizer.Normalize(input1);
            string result2 = _unicodeNormalizer.Normalize(input2);

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
            string result1 = _unicodeNormalizer.Normalize(input1);
            string result2 = _unicodeNormalizer.Normalize(input2);

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
            string result = _unicodeNormalizer.Normalize(input);

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
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("OAO", result);
        }

        [Fact]
        public void Normalize_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            string result = _unicodeNormalizer.Normalize(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Normalize_WithNullString_ReturnsNullString()
        {
            // Arrange
            string input = null;

            // Act
            string result = _unicodeNormalizer.Normalize(input);

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
            string input6 = "х"; // Cyrillic 'h'
            string input7 = "у"; // Cyrillic 'y'

            // Act
            string result1 = _unicodeNormalizer.Normalize(input1);
            string result2 = _unicodeNormalizer.Normalize(input2);
            string result3 = _unicodeNormalizer.Normalize(input3);
            string result4 = _unicodeNormalizer.Normalize(input4);
            string result5 = _unicodeNormalizer.Normalize(input5);
            string result6 = _unicodeNormalizer.Normalize(input6);
            string result7 = _unicodeNormalizer.Normalize(input7);

            // Assert
            Assert.Equal("a", result1);
            Assert.Equal("e", result2);
            Assert.Equal("o", result3);
            Assert.Equal("p", result4);
            Assert.Equal("c", result5);
            Assert.Equal("h", result6);
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
            string result1 = _unicodeNormalizer.Normalize(input1);
            string result2 = _unicodeNormalizer.Normalize(input2);
            string result3 = _unicodeNormalizer.Normalize(input3);
            string result4 = _unicodeNormalizer.Normalize(input4);

            // Assert
            Assert.Equal("test-dash", result1);
            Assert.Equal("test-dash", result2);
            Assert.Equal("test'dquote", result3);
            Assert.Equal("test\"dquote", result4);
        }
    }
}