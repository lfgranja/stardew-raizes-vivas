using System;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class UnicodeNormalizationServiceTests
    {
        private readonly UnicodeNormalizationService _service;

        public UnicodeNormalizationServiceTests()
        {
            _service = new UnicodeNormalizationService();
        }

        [Fact]
        public void Normalize_WithNullInput_ReturnsNull()
        {
            // Act
            var result = _service.Normalize(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Normalize_WithEmptyInput_ReturnsEmpty()
        {
            // Act
            var result = _service.Normalize("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Normalize_WithWhitespaceInput_ReturnsWhitespace()
        {
            // Act
            var result = _service.Normalize("  ");

            // Assert
            Assert.Equal("  ", result);
        }

        [Fact]
        public void Normalize_WithBasicLatinText_ReturnsSameText()
        {
            // Act
            var result = _service.Normalize("test.txt");

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Normalize_WithDiacritics_RemovesDiacriticsFromLatin()
        {
            // Act
            var result = _service.Normalize("café.txt");

            // Assert
            Assert.Equal("cafe.txt", result);
        }

        [Fact]
        public void Normalize_WithCyrillicLookalikes_ConvertsToLatin()
        {
            // Act
            var result = _service.Normalize("pаssword.txt"); // "a" is Cyrillic

            // Assert
            Assert.Equal("password.txt", result);
        }

        [Fact]
        public void Normalize_WithCyrillicWord_PreservesInCyrillicContext()
        {
            // Act
            var result = _service.Normalize("тест"); // This should remain as "тест" in proper context

            // Assert
            Assert.Equal("тест", result);
        }

        [Fact]
        public void Normalize_WithMixedCyrillicLatin_ConvertsLookalikes()
        {
            // Act
            var result = _service.Normalize("pаsswordтест"); // "a" is Cyrillic but followed by Cyrillic

            // For this case, we expect the lookalike to be converted
            Assert.Equal("passwordтест", result);
        }

        [Fact]
        public void Normalize_WithZeroWidthCharacters_RemovesThem()
        {
            // Act
            var result = _service.Normalize("test\u200Bfile.txt"); // Zero-width character

            // Assert
            Assert.Equal("testfile.txt", result);
        }

        [Fact]
        public void Normalize_WithBidirectionalCharacters_RemovesThem()
        {
            // Act
            var result = _service.Normalize("test\u202Afile.txt"); // Bidirectional override

            // Assert
            Assert.Equal("testfile.txt", result);
        }

        [Fact]
        public void Normalize_WithControlCharacters_RemovesThem()
        {
            // Act
            var result = _service.Normalize("test\u0001file.txt"); // Control character

            // Assert
            Assert.Equal("testfile.txt", result);
        }

        [Fact]
        public void Normalize_WithPrecomposedCharacters_Simplifies()
        {
            // Act
            var result = _service.Normalize("testøfile.txt"); // o with stroke

            // Assert
            Assert.Equal("testofile.txt", result);
        }

        [Fact]
        public void Normalize_WithAEligature_Simplifies()
        {
            // Act
            var result = _service.Normalize("testæfile.txt"); // ae ligature

            // Assert
            Assert.Equal("testaefile.txt", result);
        }

        [Fact]
        public void Normalize_WithDifferentDashes_Normalizes()
        {
            // Act
            var result = _service.Normalize("test–file.txt"); // En dash

            // Assert
            Assert.Equal("test-file.txt", result);
        }

        [Fact]
        public void Normalize_WithEmoji_Preserves()
        {
            // Act
            var result = _service.Normalize("test😀file.txt"); // Emoji

            // Assert
            Assert.Equal("test😀file.txt", result);
        }

        [Fact]
        public void Normalize_WithGreekLetters_RemovesDiacritics()
        {
            // Greek letters with diacritics should have their diacritics removed
            // Greek combining diacritical marks (U+0300 to U+036F) should be removed
            var result = _service.Normalize("ἄέὶ.txt"); // Greek letters with diacritics (alpha with acute, epsilon with acute, iota with grave)

            // Assert that diacritics are removed from Greek letters
            Assert.Equal("αει.txt", result);
        }

        [Fact]
        public void Normalize_WithHebrewLetters_Preserves()
        {
            // Hebrew letters should be preserved
            var result = _service.Normalize("בדיקה.txt"); // Hebrew characters for "test"

            // Assert
            Assert.Equal("בדיקה.txt", result);
        }

        [Fact]
        public void Normalize_WithValidSurrogatePair_PreservesPair()
        {
            // Arrange: Create a string with a valid surrogate pair (emoji)
            var input = "test" + "\uD83D\uDE00" + "file.txt"; // "test😀file.txt"

            // Act
            var result = _service.Normalize(input);

            // Assert: The emoji should be preserved as a valid surrogate pair
            Assert.Equal("test😀file.txt", result);
        }

        [Fact]
        public void Normalize_WithDanglingHighSurrogate_ReplacesWithReplacementChar()
        {
            // Arrange: Create a string with a dangling high surrogate
            var input = "test" + "\uD83D" + "file.txt"; // Dangling high surrogate

            // Act
            var result = _service.Normalize(input);

            // Assert: The dangling high surrogate should be replaced with the replacement character
            Assert.Equal("test\uFFFDfile.txt", result);
        }

        [Fact]
        public void Normalize_WithDanglingLowSurrogate_ReplacesWithReplacementChar()
        {
            // Arrange: Create a string with a dangling low surrogate
            var input = "test" + "\uDE00" + "file.txt"; // Dangling low surrogate

            // Act
            var result = _service.Normalize(input);

            // Assert: The dangling low surrogate should be replaced with the replacement character
            Assert.Equal("test\uFFFDfile.txt", result);
        }

        [Fact]
        public void Normalize_WithSurrogatePairsAndDiacritics_HandlesBothCorrectly()
        {
            // Arrange: String with both surrogate pairs and diacritics
            var input = "café" + "\uD83D\uDE00" + "naïve.txt"; // Contains diacritics and an emoji

            // Act
            var result = _service.Normalize(input);

            // Assert: Diacritics should be removed, emoji should be preserved
            Assert.Equal("cafe😀naive.txt", result);
        }
    }
}
