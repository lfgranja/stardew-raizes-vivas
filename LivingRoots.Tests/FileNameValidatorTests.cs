using System;
using System.IO;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class FileNameValidatorTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public FileNameValidatorTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }

        [Fact]
        public void GetFilePath_WithValidFileName_ReturnsValidPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "valid_filename");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/valid_filename.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithCrossPlatformValidChars_ReturnsValidPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "valid-filename_123");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/valid-filename_123.json", testData), Times.Once);

            service.SaveData(testData, "another.valid_filename");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/another.valid_filename.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithInvalidFileNameChars_ReplacesWithUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "invalid<char>s:here");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/invalid_char_s_here.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameTooLong_Truncates()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            var longFileName = new string('a', 300);
            var truncatedFileName = new string('a', 240);

            // Act
            service.SaveData(testData, longFileName);

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{truncatedFileName}.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithTrailingSpaces_TrimsSpaces()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file_with_spaces   ");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_spaces.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithLeadingSpaces_TrimsSpaces()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "   file_with_leading_spaces");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_leading_spaces.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameWithOnlySpaces_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "\t\t\t"));
        }

        [Fact]
        public void GetFilePath_WithFileNameStartingWithDot_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, ".hidden_file");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/.hidden_file.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameEndingWithDot_RemovesTrailingDot()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file_with_trailing_dot.");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_trailing_dot.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithMultipleDotsInFileName_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file.with.multiple.dots");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file.with.multiple.dots.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameContainingNullChar_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "file\0with_null"));
        }

        [Fact]
        public void GetFilePath_WithFileNameContainingControlChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file" + (char)0x01 + "with_control");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_control.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameWithUnicodeBidiChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file\u202Dtest"); // Left-to-right override

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/filetest.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameWithValidUnicode_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file_with_úñíçødé_chars");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_unicode_chars.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameWithSurrogatePairs_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            var emoji = char.ConvertFromUtf32(0x1F600);

            // Act
            service.SaveData(testData, $"file_with_{emoji}_emoji");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/file_with_{emoji}_emoji.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithFileNameWithWindowsForbiddenNames_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            service.SaveData(testData, "CON");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);

            service.SaveData(testData, "PRN.txt");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/PRN_.txt.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithMacForbiddenChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file:with:colons");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_colons.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithUnixForbiddenChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file/with/slashes");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_slashes.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithEmptyFileName_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ""));
        }

        [Fact]
        public void GetFilePath_WithFileNameWithOnlyInvalidChars_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "<>:\"|?*"));
        }

        [Fact]
        public void GetFilePath_WithUnicodeSecurityIssues_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "file\u202Etxet"); // Right-to-left override

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/filetxet.json", testData), Times.Once);
        }

        [Fact]
        public void GetFilePath_WithMixedValidInvalidChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act
            service.SaveData(testData, "valid<with>mixed:chars|and?wildcards*");

            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/valid_with_mixed_chars_and_wildcards.json", testData), Times.Once);
        }
    }
}