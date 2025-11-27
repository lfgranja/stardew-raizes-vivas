using System;
using System.IO;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class PathValidationServiceTests
    {
        private readonly PathValidationService _service;

        public PathValidationServiceTests()
        {
            // Create mock dependencies
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            mockUnicodeService.Setup(s => s.Normalize(It.IsAny<string>())).Returns<string>(s => s);
            
            _service = new PathValidationService(mockUnicodeService.Object);
        }

        [Fact]
        public void Validate_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(null!));
            Assert.Contains("Path cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(""));
            Assert.Contains("Path cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Validate_WithWhitespacePath_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("   "));
            Assert.Contains("Path cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Validate_WithValidRelativePath_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithPathTraversal_DotDot_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithPathTraversal_DotDotPlatformSpecific_ThrowsArgumentException()
        {
            // Act & Assert - Test both forward slash and backslash path separators
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_WithWindowsAbsolutePath_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("C:\\Windows\\file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithUnixAbsolutePath_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("/etc/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithHttpUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("http://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithEncodedPathTraversal_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("%2e%2e%2f"));
            Assert.Contains("Path cannot contain encoded path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotSlashAtStart_ThrowsArgumentException()
        {
            // Act & Assert - Test both forward slash and backslash separators
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("./file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate(".\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_WithDotDotAtEnd_DoesNotThrow()
        {
            // Act & Assert - Test both forward slash and backslash separators
            // After refactoring, paths ending with ".." are allowed as long as they don't go above root
            _service.Validate("folder/.."); // Should not throw
            _service.Validate("folder\\.."); // Should not throw
        }

        [Fact]
        public void Validate_WithMultipleDotDot_ThrowsArgumentException()
        {
            // Act & Assert - Test both forward slash and backslash separators
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("..\\..\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_WithDotSegmentsInMiddle_DoesNotThrow()
        {
            // Act & Assert - Test both forward slash and backslash separators
            _service.Validate("folder/.config/file.txt"); // Should not throw
            _service.Validate("path/to/./file.txt"); // Should not throw - "." in middle is safe
            _service.Validate("path\\to\\.file.txt"); // Should not throw - test backslash version too
        }

        [Fact]
        public void Validate_WithValidPathWithDots_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("test.file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithStandaloneDot_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("."));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotSlash_ThrowsArgumentException()
        {
            // Act & Assert - Test both forward slash and backslash separators
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("./"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate(".\\"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_WithDotDotSlashAtStart_ThrowsArgumentException()
        {
            // Act & Assert - Test both forward slash and backslash separators
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("./../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate(".\\..\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_WithComplexValidPath_DoesNotThrow()
        {
            // Act & Assert - This path is actually valid: "folder/subfolder/../../file.txt" resolves to "file.txt"
            // It goes down 2 levels (folder/subfolder) then up 2 levels (../../) then down 1 level (file.txt)
            // Final depth is 1 (at file.txt level), which is safe and should not throw
            _service.Validate("folder/subfolder/../../file.txt"); // Should not throw
            _service.Validate("folder\\subfolder\\..\\..\\file.txt"); // Should not throw - backslash version
        }

        [Fact]
        public void Validate_WithValidNestedPath_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/subfolder/file.txt"); // Should not throw
            _service.Validate("folder\\subfolder\\file.txt"); // Should not throw - backslash version
        }

        [Fact]
        public void Validate_WithHiddenFileInPath_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/.hidden_file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithValidPathWithMultipleSegments_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder1/folder2/folder3/file.txt"); // Should not throw
            _service.Validate("folder1\\folder2\\folder3\\file.txt"); // Should not throw - backslash version
        }

        [Fact]
        public void Validate_WithDepthTraversal_GoesNegative_ThrowsArgumentException()
        {
            // Act & Assert - Test path that goes negative in depth: "folder/../../file.txt" goes 0->1->0->-1
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("folder/../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            // The second path "folder\\..\\file.txt" goes 0->1->0->1, which does NOT go negative
            // So it should NOT throw anymore after removing the "ends with .." check
            _service.Validate("folder\\..\\file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithComplexDepthTraversal_DoesNotGoNegative_DoesNotThrow()
        {
            // Act & Assert - This should be valid as it doesn't go above root
            _service.Validate("folder/subfolder/../file.txt"); // Should not throw
            _service.Validate("folder\\subfolder\\..\\file.txt"); // Should not throw - backslash version
        }

        [Fact]
        public void Validate_WithMultipleDotSegmentsInMiddle_DoesNotThrow()
        {
            // Act & Assert - Test both forward slash and backslash separators
            _service.Validate("folder/./file.txt"); // Should not throw
            _service.Validate("folder\\.\\.\\file.txt"); // Should not throw - backslash version
        }

        [Fact]
        public void Validate_WithMixedDotAndDotDotSegments_DoesNotGoNegative_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/../subfolder/file.txt"); // Should not throw - ends at root level
            _service.Validate("folder\\..\\subfolder\\file.txt"); // Should not throw - backslash version
        }
        
        [Fact]
        public void Validate_WithMixedCaseHttpUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("hTtP://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithMixedCaseHttpsUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("HtTpS://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithMixedCaseFtpUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("FtP://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithMixedCaseFileUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("FiLe://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithUpperCaseHttpUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("HTTP://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithUpperCaseHttpsUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("HTTPS://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithUpperCaseFtpUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("FTP://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithUpperCaseFileUrl_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("FILE://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithLowerCaseHttpUrl_ThrowsArgumentException()
        {
            // Act & Assert - This should already work but let's make sure it still works after our changes
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("http://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }

        [Fact]
        public void Validate_WithLowerCaseHttpsUrl_ThrowsArgumentException()
        {
            // Act & Assert - This should already work but let's make sure it still works after our changes
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("https://example.com/file.txt"));
            Assert.Contains("Path cannot be an absolute path or URI", exception.Message);
        }
        
        [Fact]
        public void Validate_WithValidPathThatGoesUpAndDown_DoesNotThrow()
        {
            // This test verifies that paths like "a/b/../../c" are valid after security fix
            // This path goes down 2 levels (a/b) then up 2 levels (../../) then down 1 level (c)
            // Final depth would be 1 (same level as root), and it doesn't go above root, so it should be allowed
            
            // Act & Assert - This should NOT throw since it doesn't go above root
            _service.Validate("a/b/../../c");
            _service.Validate("a\\b\\..\\..\\c"); // Backslash version
        }
        
        [Fact]
        public void Validate_WithValidPathEndingWithDotDot_DoesNotThrow()
        {
            // After refactoring, paths ending with ".." are allowed as long as they don't go above root
            // This path "a/b/.." goes down 2 levels (a/b) then up 1 level (..)
            // Final depth is 1 (at directory "a"), and it doesn't go above root, so it should be allowed
            // Test both forward slash and backslash separators
            
            // Act & Assert - This should NOT throw after the refactoring
            _service.Validate("a/b/.."); // Should not throw
            _service.Validate("a\\b\\.."); // Should not throw
        }
        
        [Fact]
        public void Validate_WithConsecutiveDotSegments_DoesNotThrow()
        {
            // This test verifies that paths with consecutive ".." segments like "a/b/../../c" 
            // are allowed if they don't go above root level
            // Test both forward slash and backslash separators
            
            // Act & Assert - This should NOT throw since it doesn't go above root
            _service.Validate("a/b/../../c");
            _service.Validate("a\\b\\..\\..\\c"); // Backslash version
        }
        
        [Fact]
        public void Validate_WithValidPathThatGoesUpButNotAboveRoot_DoesNotThrow()
        {
            // This test verifies that paths that go up but don't go above root are allowed
            // e.g., "folder/subfolder/../file.txt" should be allowed since it doesn't go above root
            // Test both forward slash and backslash separators
            
            // Act & Assert - This should NOT throw since it doesn't go above root
            _service.Validate("folder/subfolder/../file.txt");
            _service.Validate("folder\\subfolder\\..\\file.txt"); // Backslash version
        }
        
        [Fact]
        public void Validate_WithPathTraversalAboveRoot_StillThrowsArgumentException()
        {
            // This test verifies that paths that actually go above root still throw
            // e.g., "folder/../../../file.txt" should still throw since it goes above root
            // Test both forward slash and backslash separators
            
            // Act & Assert
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("folder/../../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("folder\\..\\..\\..\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }
        
        [Fact]
        public void Validate_WithPlatformSpecificPathSeparators_DoesNotThrow()
        {
            // This test ensures that valid paths with platform-specific separators work correctly
            // Uses Path.Combine for cross-platform compatibility
            
            // Act & Assert - Valid paths using platform-specific separators
            _service.Validate(Path.Combine("folder", "file.txt"));
            _service.Validate(Path.Combine("folder", "subfolder", "file.txt"));
        }
        
        [Fact]
        public void Validate_WithPlatformSpecificPathTraversal_ThrowsArgumentException()
        {
            // This test ensures that path traversal attempts are caught regardless of platform-specific separators
            // Uses Path.Combine for cross-platform compatibility
            
            // Act & Assert - Path traversal attempts with platform-specific separators
            var exception = Assert.Throws<ArgumentException>(() => 
                _service.Validate(Path.Combine("..", "file.txt")));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }
        
        [Fact]
        public void Validate_WithUnicodeDotHomoglyphs_PathTraversal_ThrowsArgumentException()
        {
            // Test path traversal using Unicode homoglyphs that should be normalized to ".."
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2024\u2024/file.txt")); // ".." with U+2024
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2025\u2025/file.txt")); // ".." with U+2025
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
            
            var exception3 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2026\u2026/file.txt")); // ".." with U+2026
            Assert.Contains("Path cannot contain path traversal patterns", exception3.Message);
            
            var exception4 = Assert.Throws<ArgumentException>(() => _service.Validate("\uFF0E\uFF0E/file.txt")); // ".." with U+FF0E
            Assert.Contains("Path cannot contain path traversal patterns", exception4.Message);
        }
        
        [Fact]
        public void Validate_WithMultipleUnicodeDotHomoglyphs_PathTraversal_ThrowsArgumentException()
        {
            // Test more complex path traversal using Unicode homoglyphs
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../../\u2024\u2024/file.txt")); // "../../../" with homoglyph
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2025\u2025/\u2026\u2026/file.txt")); // ".." with different homoglyphs
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }
        
        [Fact]
        public void Validate_WithMixedPathSeparatorsAndUnicodeDots_PathTraversal_ThrowsArgumentException()
        {
            // Test that both path separator canonicalization and dot homoglyph normalization work together
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../\u2024\u2024/file.txt")); // "../.."
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("\u2025\u2025\\file.txt")); // ".." with backslash
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }
        
        [Fact]
        public void Validate_WithExcessivePathSegments_ThrowsArgumentException()
        {
            // Create a path with more segments than MaxSegments (1000) to test the hard cap
            string path = string.Join("/", new string[1001].Select((_, i) => $"dir{i}"));
            
            // Act & Assert - This should throw because it exceeds MaxSegments
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(path));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }
        
        [Fact]
        public void Validate_WithValidPathBelowSegmentLimit_DoesNotThrow()
        {
            // Create a path with fewer segments than MaxSegments (1000) to ensure normal operation still works
            // Use 5 segments to stay well within the depth limit (depth <= 10)
            string path = string.Join("/", new string[5].Select((_, i) => $"dir{i}"));
            
            // Act & Assert - This should not throw since it's below the limits
            _service.Validate(path); // Should not throw
        }
    }
}