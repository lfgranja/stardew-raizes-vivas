using System;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class PathValidationServiceTests
    {
        private readonly PathValidationService _service;

        public PathValidationServiceTests()
        {
            _service = new PathValidationService();
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
        public void Validate_WithPathTraversal_DotDotBackslash_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("..\\file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
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
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("./file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotDotAtEnd_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/.."));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithMultipleDotDot_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotSegmentsInMiddle_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/.config/file.txt"); // Should not throw
            _service.Validate("path/to/./file.txt"); // Should not throw - "." in middle is safe
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
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("./"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotDotSlashAtStart_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("./../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithComplexPathTraversal_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/subfolder/../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithValidNestedPath_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/subfolder/file.txt"); // Should not throw
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
        }

        [Fact]
        public void Validate_WithDepthTraversal_GoesNegative_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithComplexDepthTraversal_DoesNotGoNegative_DoesNotThrow()
        {
            // Act & Assert - This should be valid as it doesn't go above root
            _service.Validate("folder/subfolder/../file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithMultipleDotSegmentsInMiddle_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/././file.txt"); // Should not throw
        }

        [Fact]
        public void Validate_WithMixedDotAndDotDotSegments_DoesNotGoNegative_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/../subfolder/file.txt"); // Should not throw - ends at root level
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
            // This test verifies that valid paths like "a/b/../../c" are accepted
            // This path goes down 2 levels (a/b) then up 2 levels (../../) then down 1 level (c)
            // Final depth should be 1 (same level as root), which is valid
            
            // Act & Assert - This should NOT throw after the fix
            _service.Validate("a/b/../../c"); // Should not throw
        }
        
        [Fact]
        public void Validate_WithValidPathEndingWithDotDot_DoesNotThrow()
        {
            // This test verifies that valid paths like "a/b/.." are accepted
            // This path goes down 2 levels (a/b) then up 1 level (..)
            // Final depth should be 1 (at directory "a"), which is valid
            
            // Act & Assert - This should NOT throw after the fix
            _service.Validate("a/b/.."); // Should not throw
        }
        
        [Fact]
        public void Validate_WithConsecutiveDotDotSegments_DoesNotThrow()
        {
            // This test verifies that paths with consecutive ".." segments like "a/b/../../c" are accepted
            // when they don't go above the root level
            
            // Act & Assert - This should NOT throw after the fix
            _service.Validate("a/b/../../c"); // Should not throw
        }
        
        [Fact]
        public void Validate_WithValidPathThatGoesUpButNotAboveRoot_DoesNotThrow()
        {
            // This test verifies that paths that go up but don't go above root are accepted
            // e.g., "folder/subfolder/../file.txt" should be valid
            
            // Act & Assert
            _service.Validate("folder/subfolder/../file.txt"); // Should not throw
        }
        
        [Fact]
        public void Validate_WithPathTraversalAboveRoot_StillThrowsArgumentException()
        {
            // This test verifies that paths that actually go above root still throw
            // e.g., "folder/../../../file.txt" should still throw since it goes above root
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/../../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }
    }
}