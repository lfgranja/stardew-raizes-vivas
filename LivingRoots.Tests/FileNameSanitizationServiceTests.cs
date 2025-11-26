using System;
using Moq;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class FileNameSanitizationServiceTests
    {
        private readonly Mock<IUnicodeNormalizationService> _mockUnicodeNormalizationService;
        private readonly Mock<IReservedNameHandler> _mockReservedNameHandler;
        private readonly FileNameSanitizationService _service;

        public FileNameSanitizationServiceTests()
        {
            _mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            _mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the ReservedNameHandler to return the input by default
            _mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
                
            _service = new FileNameSanitizationService(_mockUnicodeNormalizationService.Object, _mockReservedNameHandler.Object);
        }

        [Fact]
        public void Constructor_WithNullUnicodeNormalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileNameSanitizationService(null!, _mockReservedNameHandler.Object));
        }

        [Fact]
        public void Constructor_WithNullReservedNameHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileNameSanitizationService(_mockUnicodeNormalizationService.Object, null!));
        }

        [Fact]
        public void Sanitize_WithNullFilename_ReturnsNull()
        {
            // Act
            var result = _service.Sanitize(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Sanitize_WithEmptyFilename_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(""));
            Assert.Contains("Filename cannot be empty or whitespace-only", exception.Message);
        }

        [Fact]
        public void Sanitize_WithWhitespaceOnlyFilename_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("   "));
            Assert.Contains("Filename cannot be empty or whitespace-only", exception.Message);
        }

        [Fact]
        public void Sanitize_WithNullCharacter_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("test\0file"));
            Assert.Contains("Filename cannot contain null characters", exception.Message);
        }

        [Fact]
        public void Sanitize_WithHiddenNameThatWouldCollapseToDot_ThrowsArgumentException()
        {
            // This test addresses the first issue: Prevent Hidden-Name Collapsing to "."
            // A hidden filename like ".   " should be sanitized to ".", which is invalid
            // We need to add a check to throw an ArgumentException if the content of a hidden file becomes empty after sanitization

            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".   "))
                .Returns(".   ");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".   "));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithHiddenNameThatWouldCollapseToDotWithOtherChars_ThrowsArgumentException()
        {
            // Another variation of the first issue
            // A hidden filename like ".<>" should become "." after sanitization, which is invalid

            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<>"))
                .Returns(".<>");
    
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<>"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithPathTraversalSequences_HandlesProperly()
        {
            // Setup the mock to return a non-null value for path traversal inputs
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("../test"))
                .Returns("../test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..\\test"))
                .Returns("..\\test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..test"))
                .Returns("..test");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.."))
                .Returns("test..");
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.Is<string>(s => s != "../test" && s != "..\\test" && s != "..test" && s != "test..")))
                .Returns<string>(s => s);

            // Test ../ path traversal - this may result in an exception or a transformed result
            // Path traversal should be handled by PathValidationService at a higher level
            try
            {
                var result1 = _service.Sanitize("../test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("../test", result1); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test ..\ path traversal - this may result in an exception or a transformed result
            try
            {
                var result2 = _service.Sanitize("..\\test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("..\\test", result2); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test starts with .. - this may result in an exception or a transformed result
            try
            {
                var result3 = _service.Sanitize("..test");
                // If no exception, verify it's properly transformed
                Assert.NotEqual("..test", result3); // Should be transformed
            }
            catch (ArgumentException ex)
            {
                // Exception is acceptable for path traversal attempts
                Assert.Contains("sanitizes", ex.Message.ToLower());
            }

            // Test ends with .. - should work normally (results in "test")
            var result4 = _service.Sanitize("test..");
            Assert.Equal("test", result4);
        }

        [Fact]
        public void Sanitize_WithValidFilename_ReturnsSanitizedFilename()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.txt"))
                .Returns("test.txt");

            // Act
            var result = _service.Sanitize("test.txt");

            // Assert
            Assert.Equal("test.txt", result);
            _mockUnicodeNormalizationService.Verify(x => x.Normalize("test.txt"), Times.Once);
        }

        [Fact]
        public void Sanitize_WithInvalidCharacters_RemovesInvalidCharacters()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test<>.txt"))
                .Returns("test<>.txt");

            // Act
            var result = _service.Sanitize("test<>.txt");

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithConsecutiveDots_RemovesConsecutiveDots()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test...file.txt"))
                .Returns("test...file.txt");

            // Act
            var result = _service.Sanitize("test...file.txt");

            // Assert
            Assert.Equal("test.file.txt", result);
        }

        [Fact]
        public void Sanitize_WithHiddenFile_PreservesLeadingDot()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".hidden_file.txt"))
                .Returns(".hidden_file.txt");

            // Act
            var result = _service.Sanitize(".hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }

        [Fact]
        public void Sanitize_WithHiddenFileStartingWithInvalidChars_PreservesLeadingDot()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<hidden_file.txt"))
                .Returns(".<hidden_file.txt");

            // Act
            var result = _service.Sanitize(".<hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }

        [Fact]
        public void Sanitize_WithBlockedExtension_AddsBlockedIndicator()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.exe"))
                .Returns("test.exe");

            // Act
            var result = _service.Sanitize("test.exe");

            // Assert
            Assert.Equal("test.blocked", result); // Updated to reflect the fix
        }

        [Fact]
        public void Sanitize_WithPathTraversalShouldBeHandledByPathValidationService()
        {
            // Arrange - Path traversal should be handled by PathValidationService, not FileNameSanitizationService
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("normal_file.txt"))
                .Returns("normal_file.txt");

            // Act
            var result = _service.Sanitize("normal_file.txt");

            // Assert - Normal files should still be processed correctly
            Assert.Equal("normal_file.txt", result);
        }

        [Fact]
        public void Sanitize_WhenNormalizationReturnsNull_ThrowsArgumentException()
        {
            // Arrange
            var mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            var mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the Unicode normalization service to return null
            mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns((string?)null);
                
            // Setup the ReservedNameHandler to return the input by default
            mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            var service = new FileNameSanitizationService(
                mockUnicodeNormalizationService.Object, 
                mockReservedNameHandler.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.Sanitize("test.txt"));
            Assert.Contains("Normalized filename is null", exception.Message);
        }

        [Fact]
        public void Sanitize_WithBlockedExtension_RemovesDangerousExtensionInsteadOfAppendingBlocked()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.exe"))
                .Returns("test.exe");

            // Act
            var result = _service.Sanitize("test.exe");

            // Assert - The current implementation produces "test_blocked.exe" which is still dangerous
            // The fixed implementation should produce "test.blocked" or "test_safe"
            Assert.Equal("test.blocked", result); // This test will fail with current implementation
        }
        
        [Fact]
        public void Sanitize_WithMultipleBlockedExtensions_RemovesDangerousExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.dll"))
                .Returns("test.dll");

            // Act
            var result = _service.Sanitize("test.dll");

            // Assert - Should not keep the dangerous extension
            Assert.Equal("test.blocked", result); // This test will fail with current implementation
        }
        
        [Fact]
        public void Sanitize_WithBlockedExtensionInComplexName_RemovesDangerousExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("my_file.exe"))
                .Returns("my_file.exe");

            // Act
            var result = _service.Sanitize("my_file.exe");

            // Assert - Should not keep the dangerous extension
            Assert.Equal("my_file.blocked", result); // This test will fail with current implementation
        }
        
        [Fact]
        public void Sanitize_WithValidExtension_KeepsExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("test.txt"))
                .Returns("test.txt");

            // Act
            var result = _service.Sanitize("test.txt");

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithFileNameThatBecomesDot_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("."))
                .Returns(".");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("."));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithFileNameThatBecomesDotDot_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".."))
                .Returns("..");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".."));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }

        [Fact]
        public void Sanitize_WithLongFilename_TruncatesToMaxLength()
        {
            // Arrange
            var longFilename = new string('a', 300) + ".txt";
            // After truncation of the name part to 240 chars, adding ".txt" would exceed max length
            // So it should be further truncated to ensure total length <= 240
            var expected = new string('a', 236) + ".txt"; // 236 + 4 = 240 chars total
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);

            // Act
            var result = _service.Sanitize(longFilename);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_WithHiddenLongFilename_TruncatesToMaxLength()
        {
            // Arrange
            var longHiddenFilename = "." + new string('a', 300) + ".txt";  // .aaaaa... (300 a's) .txt
            // After truncation of the name part to 239 chars (leaving 1 char for the dot), adding ".txt" would exceed max length
            // So it should be further truncated to ensure total length <= 240
            var expected = "." + new string('a', 235) + ".txt"; // 1 + 235 + 4 = 240 chars total
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);

            // Act
            var result = _service.Sanitize(longHiddenFilename);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_WithUnicodeCharacters_AppliesNormalization()
        {
            // Arrange
            var input = "tëst.txt";
            var normalized = "test.txt";
            
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(input))
                .Returns(normalized);

            // Act
            var result = _service.Sanitize(input);

            // Assert
            Assert.Equal("test.txt", result);
        }

        [Fact]
        public void Sanitize_WithResultThatBecomesEmpty_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..."))
                .Returns("...");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("..."));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithReservedNameHandlerReturningNull_ThrowsArgumentException()
        {
            // Arrange
            var mockUnicodeNormalizationService = new Mock<IUnicodeNormalizationService>();
            var mockReservedNameHandler = new Mock<IReservedNameHandler>();
            
            // Setup the reserved name handler to return null
            mockReservedNameHandler
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns((string?)null);
            
            var service = new FileNameSanitizationService(
                mockUnicodeNormalizationService.Object, 
                mockReservedNameHandler.Object);
            
            // Setup Unicode normalization to return the input
            mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(input => input);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => service.Sanitize("test.txt"));
            Assert.Contains("Filename sanitizes to an empty string.", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithLongFilenameAndExtension_EnforcesMaxLengthAfterAddingExtension()
        {
            // Arrange: Create a filename that when combined with extension exceeds MaxFileNameLength
            var baseName = new string('a', 240); // This is at the max length limit
            var extension = ".txt";
            var longFilename = baseName + extension; // This exceeds the limit when extension is added back
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longFilename);
            
            // Assert: The result should be truncated to fit within MaxFileNameLength
            Assert.NotNull(result);
            Assert.True(result.Length <= 240);
            Assert.EndsWith(".txt", result);
        }
        
        [Fact]
        public void Sanitize_WithLongHiddenFilenameAndExtension_EnforcesMaxLengthAfterAddingExtension()
        {
            // Arrange: Create a hidden filename that when combined with extension exceeds MaxFileNameLength
            var hiddenBase = "." + new string('a', 239); // Max length - 1 for the dot
            var extension = ".txt";
            var longHiddenFilename = hiddenBase + extension; // This exceeds the limit when extension is added back
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longHiddenFilename);
            
            // Assert: The result should be truncated to fit within MaxFileNameLength
            Assert.NotNull(result);
            Assert.True(result.Length <= 240);
            Assert.StartsWith(".", result);
            Assert.EndsWith(".txt", result);
        }
        
        [Fact]
        public void Sanitize_WithLongFilenameThatExceedsMaxLengthAfterAddingExtension_EnforcesMaxLength()
        {
            // Arrange: Create a filename that is just under the limit without extension
            // Max length is 240, so create a base name of 236 chars + ".exe" (4 chars) = 240 chars
            // But what if we have a name that when truncated to 236 chars still results in exceeding the limit when extension is added?
            var baseName = new string('a', 238); // 238 chars
            var extension = ".exe"; // 4 chars, will be replaced with ".blocked"
            var longFilename = baseName + extension; // 238 + 4 = 242 chars total, which exceeds max
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longFilename);
            
            // Assert: The result should be truncated to fit within MaxFileNameLength
            Assert.NotNull(result);
            Assert.True(result.Length <= 240, $"Result length {result.Length} exceeds MaxFileNameLength of 240");
            Assert.EndsWith(".blocked", result); // Dangerous extension should be replaced
        }
        
        [Fact]
        public void Sanitize_WithMaxFilenameLengthThatBecomesTooLongAfterExtensionHandling_EnforcesMaxLength()
        {
            // Arrange: Create a filename that after all processing (including trimming) is exactly at the limit
            // Then adding an extension should still respect the max length
            var baseName = new string('a', 236); // 236 chars + ".txt" (4 chars) = 240 chars (at the limit)
            var extension = ".txt";
            var longFilename = baseName + "..." + extension; // Add trailing dots that will be trimmed, then extension added back
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longFilename);
            
            // Assert: The result should be truncated to fit within MaxFileNameLength
            Assert.NotNull(result);
            Assert.True(result.Length <= 240, $"Result length {result.Length} exceeds MaxFileNameLength of 240");
            Assert.EndsWith(".txt", result); // Extension should be preserved
        }
        
        [Fact]
        public void Sanitize_WithLongFilenameAndExtensionThatRequiresPostExtensionTruncation_DoesNotLeaveTrailingChars()
        {
            // Arrange: Create a filename that after extension addition requires truncation which could leave trailing chars
            // This would happen if the truncation happens in the middle of trailing characters that should be cleaned up
            var baseName = new string('a', 237) + "   "; // 237 + 3 = 240 chars, then add extension
            var extension = ".txt";
            var longFilename = baseName + extension; // This will exceed the limit when extension is added back after processing
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longFilename);
            
            // Assert: The result should be truncated to fit within MaxFileNameLength and not have trailing spaces/dots
            Assert.NotNull(result);
            Assert.True(result.Length <= 240, $"Result length {result.Length} exceeds MaxFileNameLength of 240");
            Assert.EndsWith(".txt", result); // Extension should be preserved
            // Should not end with spaces, dots or underscores after extension
            Assert.False(result.EndsWith(" "), "Result should not end with spaces");
            Assert.False(result.EndsWith("."), "Result should not end with dots unless part of extension");
            Assert.False(result.EndsWith("_"), "Result should not end with underscores");
        }
        
        [Fact]
        public void Sanitize_WithExtensionAddedAfterTruncation_DoesFinalCleanup()
        {
            // Arrange: Create a filename that will be truncated after extension is added
            // This tests the specific scenario where truncation happens after extension addition
            // and ensures that final cleanup is performed after that truncation
            var baseName = new string('a', 238) + ".."; // This will become 240 chars + extension exceeds limit
            var extension = ".txt";
            var longFilename = baseName + extension; // Total exceeds MaxFileNameLength
            
            // Setup the mock to return the input unchanged for normalization
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Act
            var result = _service.Sanitize(longFilename);
            
            // Assert: The result should not end with trailing dots (other than in extension)
            Assert.NotNull(result);
            Assert.True(result.Length <= 240, $"Result length {result.Length} exceeds MaxFileNameLength of 240");
            Assert.EndsWith(".txt", result); // Extension should be preserved
            // After truncation and extension addition, there should be no trailing dots except in extension
            if (result.Length > 4) // If result is longer than just the extension
            {
                var withoutExtension = result.Substring(0, result.Length - 4); // Remove .txt
                Assert.False(withoutExtension.EndsWith("."), "Should not end with dots after truncation");
            }
        }
        
        [Fact]
        public void Sanitize_WithDotfileWithoutExtension_DoesNotTreatAsHavingExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".profile"))
                .Returns(".profile");

            // Act
            var result = _service.Sanitize(".profile");

            // Assert
            Assert.Equal(".profile", result);
        }
        
        [Fact]
        public void Sanitize_WithDotfileWithFakeExtension_DoesNotTreatAsHavingExtension()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".bashrc"))
                .Returns(".bashrc");

            // Act
            var result = _service.Sanitize(".bashrc");

            // Assert
            Assert.Equal(".bashrc", result);
        }

        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotAndValidContent_PreservesHiddenFileStatus()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".hidden_file.txt"))
                .Returns(".hidden_file.txt");

            // Act
            var result = _service.Sanitize(".hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotAndInvalidChars_BecomesHiddenFileWithValidContent()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<hidden_file.txt"))
                .Returns(".<hidden_file.txt");

            // Act
            var result = _service.Sanitize(".<hidden_file.txt");

            // Assert
            Assert.Equal(".hidden_file.txt", result);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotFollowedByOnlyInvalidChars_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<>"))
                .Returns(".<>");
    
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<>"));
            Assert.Contains("Filename sanitizes to an empty string", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotFollowedBySpacesAndInvalidChars_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".   <>"))
                .Returns(".   <>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".   <>"));
            Assert.Contains("Filename sanitizes to an empty string", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotFollowedByMeaningfulContentAfterSanitization_PreservesHiddenFile()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".file_name.txt"))
                .Returns(".file_name.txt");

            // Act
            var result = _service.Sanitize(".file_name.txt");

            // Assert
            Assert.Equal(".file_name.txt", result);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotAndSanitizesToOnlyDots_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<...>"))
                .Returns(".<...>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<...>"));
            Assert.Contains("Filename sanitizes to an empty string", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotAndSanitizesToOnlyUnderscores_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".<___>"))
                .Returns(".<___>");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize(".<___>"));
            Assert.Contains("Filename sanitizes to an empty string", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithHiddenFileStartingWithDotAndSanitizesToMeaningfulContent_PreservesHiddenFile()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".valid_name.txt"))
                .Returns(".valid_name.txt");
            
            // Act
            var result = _service.Sanitize(".valid_name.txt");
        
            // Assert
            Assert.Equal(".valid_name.txt", result);
        }
        
        [Fact]
        public void Sanitize_WithBlockedExtensionThatResultsInInvalidBase_ReturnsSafeFilename()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".exe"))
                .Returns(".exe");
        
            // Act
            var result = _service.Sanitize(".exe");
        
            // Assert - With the fix, ".exe" should be handled safely and return a safe filename
            Assert.Equal(".file.blocked", result);
        }
        
        [Fact]
        public void Sanitize_WithBlockedExtensionThatResultsInEmptyBase_ThrowsArgumentException()
        {
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("..exe"))
                .Returns("..exe");
        
            // Act & Assert
            // For "..exe", the name part ".." gets sanitized to "." and then to empty,
            // so the exception occurs at the name processing level, not after extension blocking
            var exception = Assert.Throws<ArgumentException>(() => _service.Sanitize("..exe"));
            Assert.Contains("Filename sanitizes to an empty string", exception.Message);
        }
        
        [Fact]
        public void Sanitize_WithMinimalHiddenFilename_ReturnsValidResult()
        {
            // Test minimal hidden filename to ensure it works properly
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".a"))
                .Returns(".a");
        
            // Act
            var result = _service.Sanitize(".a");
        
            // Assert
            Assert.Equal(".a", result);
        }
        
        [Fact]
        public void Sanitize_WithMinimalNonHiddenFilename_ReturnsValidResult()
        {
            // Test minimal non-hidden filename to ensure it works properly
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("a"))
                .Returns("a");
        
            // Act
            var result = _service.Sanitize("a");
        
            // Assert
            Assert.Equal("a", result);
        }
        
        [Fact]
        public void Sanitize_WithMinimalHiddenFilenameWithBlockedExtension_ReturnsSafeResult()
        {
            // Test minimal hidden filename with blocked extension
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize(".js"))
                .Returns(".js");
        
            // Act
            var result = _service.Sanitize(".js");
        
            // Assert
            Assert.Equal(".file.blocked", result);
        }
        
        [Fact]
        public void Sanitize_WithMinimalNonHiddenFilenameWithBlockedExtension_ReturnsSafeResult()
        {
            // Test minimal non-hidden filename with blocked extension
            // Arrange
            _mockUnicodeNormalizationService
                .Setup(x => x.Normalize("x.exe"))
                .Returns("x.exe");
        
            // Act
            var result = _service.Sanitize("x.exe");
        
            // Assert
            Assert.Equal("x.blocked", result);
        }
        
        #region SafeSubstring Tests
        [Fact]
        public void SafeSubstring_WithNullString_ReturnsEmptyString()
        {
            // Arrange
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { null, 0, 5 }) as string;

            // Assert
            Assert.Equal(string.Empty, result);
        }
        
        [Fact]
        public void SafeSubstring_WithNegativeStartIndex_NormalizesToZero()
        {
            // Arrange
            var testString = "Hello World";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, -1, 5 }) as string;

            // Assert
            Assert.Equal("Hello", result);
        }
        
        [Fact]
        public void SafeSubstring_WithZeroLength_ReturnsEmptyString()
        {
            // Arrange
            var testString = "Hello World";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 0, 0 }) as string;

            // Assert
            Assert.Equal(string.Empty, result);
        }
        
        [Fact]
        public void SafeSubstring_WithNegativeLength_ReturnsEmptyString()
        {
            // Arrange
            var testString = "Hello World";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 0, -5 }) as string;

            // Assert
            Assert.Equal(string.Empty, result);
        }
        
        [Fact]
        public void SafeSubstring_WithStartIndexBeyondStringLength_ReturnsEmptyString()
        {
            // Arrange
            var testString = "Hello";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 10, 5 }) as string;

            // Assert
            Assert.Equal(string.Empty, result);
        }
        
        [Fact]
        public void SafeSubstring_WithStartIndexAtStringLength_ReturnsEmptyString()
        {
            // Arrange
            var testString = "Hello";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 5, 5 }) as string;

            // Assert
            Assert.Equal(string.Empty, result);
        }
        
        [Fact]
        public void SafeSubstring_WithSurrogatePairAtBoundary_DoesNotSplitPair()
        {
            // Arrange: Create a string with a surrogate pair at a potential truncation boundary
            // The emoji "😀" is a surrogate pair (U+1F600)
            var testString = "Hello" + "\uD83D\uDE00" + "World"; // "Hello😀World"
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act: Try to truncate at the position where it would split the surrogate pair
            var result = method.Invoke(null, new object[] { testString, 0, 6 }) as string; // Should avoid splitting the emoji

            // Assert: The result should not split the surrogate pair
            Assert.Equal("Hello", result); // Should return only up to the boundary without splitting
        }
        
        [Fact]
        public void SafeSubstring_WithSurrogatePair_DoesNotSplitWhenPossible()
        {
            // Arrange: Create a string with a surrogate pair
            var testString = "Test" + "\uD83D\uDE00" + "End"; // "Test😀End"
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act: Extract a substring that would include the surrogate pair
            var result = method.Invoke(null, new object[] { testString, 0, 10 }) as string;

            // Assert: Should include the full surrogate pair if possible
            Assert.Equal("Test😀End", result);
        }
        
        [Fact]
        public void SafeSubstring_WithValidParameters_WorksCorrectly()
        {
            // Arrange
            var testString = "Hello World";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 0, 5 }) as string;

            // Assert
            Assert.Equal("Hello", result);
        }
        
        [Fact]
        public void SafeSubstring_WithStartIndexAndLength_WorksCorrectly()
        {
            // Arrange
            var testString = "Hello World";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 6, 5 }) as string;

            // Assert
            Assert.Equal("World", result);
        }
        
        [Fact]
        public void SafeSubstring_WithLengthExceedingStringBounds_DoesNotThrow()
        {
            // Arrange
            var testString = "Hi";
            var method = typeof(FileNameSanitizationService)
                .GetMethod("SafeSubstring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert that the method was found
            Assert.NotNull(method);
            
            // Act
            var result = method.Invoke(null, new object[] { testString, 0, 10 }) as string;

            // Assert
            Assert.Equal("Hi", result);
        }
        #endregion
    }
}
