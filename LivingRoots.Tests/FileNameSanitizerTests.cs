using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class FileNameSanitizerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public FileNameSanitizerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Fact]
        public void SanitizeKey_WithValidString_ReturnsSanitizedString()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "valid_key_name");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/valid_key_name.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithInvalidFileNameChars_ReplacesWithUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file<name>with:invalid|chars");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_name_with_invalid_chars.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithDirectorySeparators_ReplacesWithUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "path/with/separators");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/path_with_separators.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleDots_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file....name");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file.name.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithLongFileName_Truncates()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            var longName = new string('a', 300);
            var truncatedName = new string('a', 240);

            // Act
            service.SaveData(testData, longName);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{truncatedName}.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithZeroWidthUnicodeCharacters_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test\u200Bzwsp\u200Czwnj\u200Dzwj");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/testzwspzwnjzwj.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithSurrogatePairs_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            var emoji = char.ConvertFromUtf32(0x1F600);

            // Act
            service.SaveData(testData, $"test{emoji}smile");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/test{emoji}smile.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithDiacritics_RemovesAccentsProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café");
            service.SaveData(testData, "naïve");
            service.SaveData(testData, "résumé");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naive.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/resume.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithUnicodeHomoglyphs_ConvertsToLatin()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "tеst"); // Cyrillic 'е'

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithFileExtension_PreservesExtension()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "document.txt");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/document.txt.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleExtensions_PreservesExtensions()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "archive.tar.gz");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/archive.tar.gz.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithLeadingDots_TreatsAsHiddenFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, ".config");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/.config.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithTrailingDots_RemovesTrailingDots()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file.");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithConsecutiveDots_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file..name");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file.name.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithEmptyStringAfterSanitization_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "<>:\"|?*"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "........."));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "___"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }

        [Fact]
        public void SanitizeKey_WithOnlyWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "\t\t\t"));
        }

        [Fact]
        public void SanitizeKey_WithValidUnicodeCharacters_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "tëst_ünïcödé");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_unicode.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMixedValidInvalidChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file<with>mixed:chars|and?wildcards*");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_mixed_chars_and_wildcards.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithControlCharacters_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test" + (char)0x01 + "control" + (char)0x1F + "chars");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_control_chars.json", testData), Times.Once);
        }
    }
}