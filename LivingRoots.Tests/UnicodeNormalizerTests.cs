using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class UnicodeNormalizerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public UnicodeNormalizerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Fact]
        public void SanitizeKey_WithUnicodeDiacritics_RemovesAccentsProperly()
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
            service.SaveData(testData, "аpple"); // Cyrillic 'а'
            service.SaveData(testData, "соmputer"); // Cyrillic 'с' and 'о'
            service.SaveData(testData, "рeach"); // Cyrillic 'р'
            service.SaveData(testData, "хtml"); // Cyrillic 'х'

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/apple.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/computer.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/peach.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/html.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithCombinedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "cafe\u0301");
            service.SaveData(testData, "naif\u0308");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naif.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithPrecomposedUnicodeDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café");
            service.SaveData(testData, "naïve");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/naive.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithZeroWidthUnicodeCharacters_RemovesProperly()
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
        public void SanitizeKey_WithHebrewText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test שלום");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test שלום.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithArabicText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test كتاب");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test كتاب.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithChineseText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test 你好");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test 你好.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithEmoji_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            var emoji = char.ConvertFromUtf32(0x1F600);

            // Act
            service.SaveData(testData, $"test {emoji} smile");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/test {emoji} smile.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMixedUnicodeAndDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "café тест naïve");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe тест naive.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "a\u0300\u0301b");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/ab.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithGreekTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "μέντι");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/μεντι.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithTurkishTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "göçmen");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/gocmen.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithThaiTextWithDiacritics_RemovesAccents()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "คํา");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/คํา.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithDevanagariText_PreservesValidUnicode()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "नमस्ते");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/नमस्ते.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleNormalizationForms_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            string nfcForm = "café";
            string nfdForm = "cafe\u0301";

            service.SaveData(testData, nfcForm);
            service.SaveData(testData, nfdForm);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe.json", testData), Times.Exactly(2));
        }

        [Fact]
        public void SanitizeKey_WithHomoglyphAndDiacriticsTogether_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "tеst\u0301");
            service.SaveData(testData, "cafe\u0435\u0301");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafee.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithControlAndFormatUnicodeChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "test\u200Estart");
            service.SaveData(testData, "test\u200Fend");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/teststart.json", testData), Times.Once);
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/testend.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithVeryLongUnicodeString_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            string input = new string('a', 100) + "café" + new string('b', 100) + "naïve" + new string('c', 100);
            service.SaveData(testData, input);

            // Assert
            string expected = new string('a', 100) + "cafe" + new string('b', 100) + "naive" + new string('c', 96);
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expected}.json", testData), Times.Once);
        }
    }
}