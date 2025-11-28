using System;
using System.IO;
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
        public void Handle_WithUnicodeHomoglyphOfReservedName_AddsUnderscore_Safely()
        {
            // Arrange
            string input = "CОN"; // Contains Cyrillic 'О'
            string normalized = "CON";
            
            // Setup mock to return the normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert: underscore must be appended based on normalized reserved name, and the returned value must not preserve spoofing
            Assert.NotNull(result);
            Assert.EndsWith("_", result);
            // Ensure the base part equals the normalized safe form, not the homoglyph-containing original
            Assert.Equal("CON_", result);
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
        public void Handle_WithReservedNameWithDiacritics_AddsUnderscore_Safely()
        {
            // Arrange
            string input = "CÓÑ"; // With diacritics
            string normalized = "CON"; // Should normalize to CON
            
            // Setup mock to return the normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert: For security, return the normalized version with underscore, not the original with diacritics
            Assert.Equal("CON_", result); // The normalized name with underscore added
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
        public void Handle_WithWhitespaceOnlyString_ReplacesWithSafePlaceholder()
        {
            // Arrange
            string input = "   ";
            string expected = "_";

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(expected, result);
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
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimming_ReplacesWithSafePlaceholder()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, replace with safe placeholder to prevent malformed outputs

            // Arrange
            string input = "   "; // This becomes empty after trimming leading/trailing spaces
            string expected = "_"; // Safe placeholder

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return safe placeholder instead of causing malformed output
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimmingDotsAndSpaces_ReplacesWithSafePlaceholder()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, replace with safe placeholder to prevent malformed outputs

            // Arrange
            string input = "..."; // This becomes empty after trimming dots and spaces
            string expected = "_"; // Safe placeholder

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return safe placeholder instead of causing malformed output
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Handle_WithFilenameThatBecomesEmptyAfterTrimmingMixedChars_ReplacesWithSafePlaceholder()
        {
            // This test addresses the third issue: Prevent Malformed Names After Trimming
            // If baseNameForCheck is empty after trimming, replace with safe placeholder to prevent malformed outputs

            // Arrange
            string input = " . "; // This becomes empty after trimming leading/trailing spaces and dots
            string expected = "_"; // Safe placeholder

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return safe placeholder instead of causing malformed output
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Handle_WithDirectoryPathEndingWithSeparator_ReturnsOriginal()
        {
            // This test addresses the fourth issue: Avoid Mutating Directory-Only Paths
            // If Path.GetFileName results in an empty string (directory path ending with separator),
            // return the original path without modification

            // Arrange - Use platform-agnostic separator
            string input = "path/to/directory" + Path.DirectorySeparatorChar; // Path ending with directory separator

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

            // Arrange - Use platform-agnostic separator
            string input = "CON" + Path.DirectorySeparatorChar; // Directory path ending with separator, where base name is reserved

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert - Should return the original input since it's a directory path (Path.GetFileName returns empty)
            Assert.Equal(input, result);
        }
        
        [Fact]
        public void Handle_WithPlatformSpecificDirectoryPathEndingWithSeparator_ReturnsOriginal()
        {
            // This test addresses the fourth issue: Avoid Mutating Directory-Only Paths
            // Tests platform-specific behavior with correct separators
            
            // Arrange - Test both forward slash and platform-specific separator
            string inputForwardSlash = "path/to/directory/";
            string inputPlatformSpecific = "path/to/directory" + Path.DirectorySeparatorChar;

            // Act
            string? resultForwardSlash = _reservedNameHandler.Handle(inputForwardSlash);
            string? resultPlatformSpecific = _reservedNameHandler.Handle(inputPlatformSpecific);

            // Assert - Should return the original input since Path.GetFileName would return empty string
            Assert.Equal(inputForwardSlash, resultForwardSlash);
            Assert.Equal(inputPlatformSpecific, resultPlatformSpecific);
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
        public void Handle_WithFullyInsignificantName_ReplacesWithSafePlaceholder()
        {
            // This test addresses the expected behavior: Replace with safe placeholder to prevent malformed outputs
            // If baseNameForCheck is empty after trimming, replace with safe placeholder to preserve user input
            
            // Arrange
            var testCases = new[] { "   ", "...", " . ", " . . ", "   ...   " };
            string expected = "_";
            
            foreach (var input in testCases)
            {
                // Act
                string? result = _reservedNameHandler.Handle(input);
                
                // Assert - should return safe placeholder instead of ambiguous filename
                Assert.Equal(expected, result);
            }
        }
        
        [Theory]
        [InlineData("   ", "_")]
        [InlineData("...", "_")]
        [InlineData(" . ", "_")]
        [InlineData(" . . ", "_")]
        [InlineData("   ...   ", "_")]
        public void Handle_WithInsignificantName_ReplacesWithSafePlaceholder(string input, string expected)
        {
            // This test verifies that filenames consisting only of insignificant characters 
            // (dots/spaces) are replaced with a safe placeholder instead of returning the original
            
            // Arrange & Act
            string? result = _reservedNameHandler.Handle(input);
            
            // Assert - should return safe placeholder instead of original ambiguous filename
            Assert.Equal(expected, result);
        }
        
        [Theory]
        [InlineData("path/to/   ", "path/to/_")]
        [InlineData("path/to/...", "path/to/_")]
        [InlineData("path/to/ . ", "path/to/_")]
        public void Handle_WithPathWithInsignificantFileName_ReplacesWithSafePlaceholder(string input, string expected)
        {
            // This test verifies that paths with filenames consisting only of insignificant characters 
            // are replaced with a safe placeholder while preserving the directory path
            
            // Arrange & Act
            string? result = _reservedNameHandler.Handle(input);
            
            // Assert - should return path with safe placeholder instead of original ambiguous filename
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void Handle_WithRootedPathAndReservedName_ProcessesFileNameComponent()
        {
            // This test verifies that rooted paths with reserved names are handled properly
            // The directory path should be preserved, but the filename component should have reserved name handling applied
            // Uses platform-agnostic approach
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "CON.txt"); // Rooted path with reserved name
            string fileNamePart = "CON";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(Path.Combine("C:", "CON_.txt"), result);
        }
        
        [Fact]
        public void Handle_WithRootedPathAndReservedNameWithExtension_ProcessesFileNameComponent()
        {
            // This test verifies that rooted paths with reserved names and extensions are handled properly
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "COM1.xml"); // Rooted path with reserved name and extension
            string fileNamePart = "COM1";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(Path.Combine("C:", "COM1_.xml"), result);
        }
        
        [Fact]
        public void Handle_WithUNCPathAndReservedName_ProcessesFileNameComponent()
        {
            // This test verifies that UNC paths with reserved names are handled properly
            // Note: UNC paths use forward slashes on some systems, backslashes on Windows
            // The implementation should handle both
            
            // Arrange
            string input = @"\\server\share\PRN.log"; // UNC path with reserved name
            string fileNamePart = "PRN";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

            // Act
            string? result = _reservedNameHandler.Handle(input);
            
            // Assert - The result should have the reserved name processed (PRN_ instead of PRN)
            // Expected result should be \\server\share\PRN_.log
            Assert.Equal(@"\\server\share\PRN_.log", result);
        }
        
        [Fact]
        public void Handle_WithRootedPathAndNonReservedName_DoesNotChange()
        {
            // This test verifies that rooted paths with non-reserved names are not changed
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "normal_file.txt"); // Rooted path with non-reserved name
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(Path.Combine("C:", "normal_file.txt"), result);
        }
        
        [Fact]
        public void Handle_WithRootedPathAndNonReservedNameSimilarToReserved_DoesNotChange()
        {
            // This test verifies that rooted paths with names similar to reserved but not exact matches are not changed
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "CONSOLE.txt"); // Rooted path with name similar to reserved but not exact
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(input => input);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(Path.Combine("C:", "CONSOLE.txt"), result);
        }
        
        [Fact]
        public void Handle_WithRootedPathEndingWithSeparator_ReturnsOriginal()
        {
            // This test verifies that rooted paths ending with a separator (directory paths) return unchanged
            // This is important because Path.GetFileName returns empty for such paths
            // Uses platform-agnostic approach
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "some", "directory") + Path.DirectorySeparatorChar; // Rooted directory path ending with separator

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(input, result);
        }
        
        [Fact]
        public void Handle_WithRootedPathContainingSubdirectoriesAndReservedName_ProcessesFileName()
        {
            // This test verifies that complex rooted paths with reserved names in the final component are handled
            
            // Arrange - Use platform-agnostic path construction
            string input = Path.Combine("C:", "path", "to", "AUX", "file", "LPT1.dat"); // Rooted path with reserved name in final component
            string fileNamePart = "LPT1";
            
            // Setup mock to return the same string for normalization
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(fileNamePart)).Returns(fileNamePart);

            // Act
            string? result = _reservedNameHandler.Handle(input);

            // Assert
            Assert.Equal(Path.Combine("C:", "path", "to", "AUX", "file", "LPT1_.dat"), result);
        }
        
        [Fact]
        public void Handle_WithHomoglyphReservedName_UsesNormalizedOutput()
        {
            // Arrange
            string input = "CОN"; // Uses Cyrillic 'О' instead of Latin 'O' - a homoglyph attack
            string normalized = "CON"; // After normalization, it becomes the reserved name CON
            
            // Setup mock to return the normalized version
            _mockUnicodeNormalizationService.Setup(x => x.Normalize(input)).Returns(normalized);
            
            // Act
            string? result = _reservedNameHandler.Handle(input);
            
            // Assert: The result should use the normalized form (CON_) rather than preserving the original homoglyph
            Assert.Equal("CON_", result); // Ensures the normalized form is used in the output to prevent spoofing
        }
    }
}