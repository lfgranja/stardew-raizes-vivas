using System;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;
using LivingRoots.Domain;

namespace LivingRoots.Tests
{
    public class ReservedNameHandlerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IPathTraversalValidator> _mockPathTraversalValidator;
        private readonly Mock<IFileNameSanitizer> _mockFileNameSanitizer;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly ReservedNameHandler _reservedNameHandler;

        public ReservedNameHandlerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockPathTraversalValidator = new Mock<IPathTraversalValidator>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizer>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Create a real ReservedNameHandler instance with mocked UnicodeNormalizationService dependency
            _reservedNameHandler = new ReservedNameHandler(_mockUnicodeNormalizationService.Object);
            
            // Configure the file name sanitizer to return the input as-is for these tests
            _mockFileNameSanitizer.Setup(x => x.Sanitize(It.IsAny<string>())).Returns<string>(input => input);
            
            // Configure the path traversal validator to not throw for valid paths in these tests
            _mockPathTraversalValidator.Setup(x => x.Validate(It.IsAny<string>())).Verifiable();
            
            // Setup the mock UnicodeNormalizationService to return the input by default
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);
        }

        [Theory]
        [InlineData("CON", "CON_")]
        [InlineData("PRN", "PRN_")]
        [InlineData("AUX", "AUX_")]
        [InlineData("NUL", "NUL_")]
        [InlineData("COM1", "COM1_")]
        [InlineData("LPT1", "LPT1_")]
        [InlineData("con", "con_")]
        [InlineData("CoN", "CoN_")]
        public void Handle_WithReservedWindowsName_AddsUnderscore(string reservedName, string expectedName)
        {
            // Arrange
            // Setup mock to return the same string for normalization (no change for basic reserved names)
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(reservedName)).Returns(reservedName);

            // Act
            string? result = _reservedNameHandler.Handle(reservedName);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Theory]
        [InlineData("CON.log", "CON_.log")]
        [InlineData("PRN.log", "PRN_.log")]
        [InlineData("COM1.xml", "COM1_.xml")]
        public void Handle_WithReservedNameAndExtension_HandlesCorrectly(string reservedName, string expectedName)
        {
            // Arrange
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);

            // Act
            string? result = _reservedNameHandler.Handle(reservedName);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Fact]
        public void Handle_WithNonReservedName_DoesNotAddUnderscore()
        {
            // Arrange
            string input = "normal_name";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithNonReservedNameWithComplexExtensions_DoesNotAddUnderscore()
        {
            // Arrange
            string input1 = "CONSOLE.log";
            string input2 = "ACOM123.xml";
            
            // Setup mock to return the same strings for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input1)).Returns(input1);
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input2)).Returns(input2);

            // Act
            string? result1 = _reservedNameHandler.Handle(input1);
            string? result2 = _reservedNameHandler.Handle(input2);

            // Assert
            Assert.Equal(input1, result1);
            Assert.Equal(input2, result2);
        }

        [Fact]
        public void Handle_WithUnicodeHomoglyphOfReservedName_AddsUnderscore()
        {
            // Arrange
            string input = "CОN"; // Cyrillic O should normalize to Latin O
            string normalized = "CON";
            
            // Setup mock to return the normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal("CОN_", result); // The original name with underscore added
        }

        [Fact]
        public void Handle_WithReservedNameWithSpaces_HandlesProperly()
        {
            // Arrange
            string input = " CON ";
            string trimmed = "CON";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(trimmed)).Returns(trimmed);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // After fix: Trailing spaces should not be re-applied since they're insignificant in Windows
            Assert.Equal(" CON_", result); // The original string with underscore added after core name, but trailing spaces removed
        }

        [Fact]
        public void Handle_WithReservedNameWithTrailingDotsAndSpaces_HandlesProperly()
        {
            // Arrange
            string input = "CON   ...";
            string baseName = "CON"; // After trimming
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(baseName)).Returns(baseName);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // After fix: Trailing insignificant characters should not be re-applied
            Assert.Equal("CON_", result); // The original string with underscore added to the reserved part, but trailing spaces/dots removed
        }

        [Fact]
        public void Handle_WithReservedNameWithTrailingSpaces_HandlesProperly()
        {
            // Arrange
            string input = "CON   "; // CON with trailing spaces
            string baseName = "CON"; // After trimming
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(baseName)).Returns(baseName);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // After fix: Trailing spaces should not be re-applied since they're insignificant in Windows
            Assert.Equal("CON_", result); // The original string with underscore added to the reserved part, but trailing spaces removed
        }

        [Fact]
        public void Handle_WithReservedNameWithTrailingDots_HandlesProperly()
        {
            // Arrange
            string input = "CON..."; // CON with trailing dots
            string baseName = "CON"; // After trimming
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(baseName)).Returns(baseName);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // After fix: Trailing dots should not be re-applied since they're insignificant in Windows
            Assert.Equal("CON_", result); // The original string with underscore added to the reserved part, but trailing dots removed
        }

        [Fact]
        public void Handle_WithReservedNameWithDiacritics_AddsUnderscore()
        {
            // Arrange
            string input = "CÓÑ"; // With diacritics
            string normalized = "CON"; // Should normalize to CON
            
            // Setup mock to return the normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal("CÓÑ_", result); // The original name with underscore added
        }

        [Fact]
        public void Handle_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithNullString_ReturnsNullString()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = _reservedNameHandler.Handle(input!); // Use null-forgiving operator to indicate intentional null usage

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Handle_WithWhitespaceOnlyString_ReturnsWhitespaceOnlyString()
        {
            // Arrange
            string input = "   ";

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithValidPath_ReturnsSamePath()
        {
            // Arrange
            string input = "path/to/file.txt";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithPathContainingReservedName_AddsUnderscoreToFileName()
        {
            // Arrange
            string input = "path/to/CON.txt";
            string fileNamePart = "CON";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal("path/to/CON_.txt", result);
        }

        [Fact]
        public void Handle_WithPathContainingNonReservedNameWithSimilarPattern_DoesNotChange()
        {
            // Arrange
            string input = "path/to/CONSOLE.txt";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithMultipleExtensionsAndReservedName_AddsUnderscore()
        {
            // Arrange & Act
            string? result = _reservedNameHandler.Handle("COM1.tar.gz");
            
            // Assert - This should add an underscore after COM1 to make it COM1_.tar.gz
            Assert.Equal("COM1_.tar.gz", result);
        }

        [Fact]
        public void Handle_WithLPTNumber_HandlesCorrectly()
        {
            // Arrange
            string input = "LPT9";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal("LPT9_", result);
        }

        [Fact]
        public void Handle_WithCOMNumber_HandlesCorrectly()
        {
            // Arrange
            string input = "COM9";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal("COM9_", result);
        }

        [Fact]
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimming_ReturnsOriginal()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, return the original filename to prevent malformed outputs

            // Arrange
            string input = "   "; // This becomes empty after trimming leading/trailing spaces

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input instead of causing malformed output
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimmingDotsAndSpaces_ReturnsOriginal()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, return the original filename to prevent malformed outputs

            // Arrange
            string input = "..."; // This becomes empty after trimming dots and spaces

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input instead of causing malformed output
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimmingMixedChars_ReturnsOriginal()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, return the original filename to prevent malformed outputs

            // Arrange
            string input = " . . "; // This becomes empty after trimming leading/trailing spaces and dots

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input instead of causing malformed output
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithDirectoryPathEndingWithSeparator_ReturnsOriginal()
        {
            // This test addresses the fourth issue: Avoid Mutating Directory-Only Paths
            // If Path.GetFileName results in an empty string (directory path ending with separator),
            // return the original path without modification

            // Arrange
            string input = "path/to/directory/"; // Path ending with directory separator

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input since Path.GetFileName would return empty string
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithAnotherDirectoryPathEndingWithSeparator_ReturnsOriginal()
        {
            // This test addresses the fourth issue: Avoid Mutating Directory-Only Paths
            // If Path.GetFileName results in an empty string (directory path ending with separator),
            // return the original path without modification

            // Arrange
            string input = "CON/"; // Directory path ending with separator, where base name is reserved

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input since it's a directory path (Path.GetFileName returns empty)
            Assert.Equal(input, result);
        }

        [Fact]
        public void Handle_WithDirectoryPathWithBackslashSeparator_ReturnsOriginal()
        {
            // This test addresses the fourth issue: Avoid Mutating Directory-Only Paths
            // If Path.GetFileName results in an empty string (directory path ending with separator),
            // return the original path without modification

            // Arrange
            string input = "path\\to\\directory\\"; // Path ending with backslash separator

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input since Path.GetFileName would return empty string
            Assert.Equal(input, result);
        }
        
        [Fact]
        public void Handle_WithReservedNameFollowedByTrailingCharacters_AvoidsRecreatingReservedNames()
        {
            // This test addresses the issue: Avoid recreating reserved names via trailing characters
            // After detecting a reserved name and adding an underscore, we need to sanitize
            // the trailing characters to prevent them from recreating the reserved name condition
            // This means sanitizing trailing characters before re-applying them
            
            // Arrange
            string input = "CON..."; // Reserved name with trailing dots
            string baseName = "CON"; // After trimming trailing dots and spaces for check
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(baseName)).Returns(baseName);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // After fix: The trailing characters should be sanitized to avoid recreating the issue
            // Trailing insignificant characters (dots and spaces) should not be re-applied
            // so "CON..." should become "CON_" (not "CON_...")
            Assert.Equal("CON_", result);
        }
        [Fact]
        public void Handle_WithFullyInsignificantName_ReturnsOriginal()
        {
            // This test addresses the expected behavior: Return original filename to prevent malformed outputs
            // If baseNameForCheck is empty after trimming, return the original filename to preserve user input
            
            // Arrange
            var testCases = new[] { "   ", "...", " . ", " . . ", "   ...   " };
            
            foreach (var input in testCases)
            {
                // Act
                string? result = _reservedNameHandler.Handle(input);
                
                // Assert - should return original instead of modifying the ambiguous filename
                Assert.Equal(input, result);
            }
        }
    }
}