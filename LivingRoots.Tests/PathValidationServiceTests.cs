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
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate(null));
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
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
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
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }

        [Fact]
        public void Validate_WithDotSlash_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("./"));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }

        [Fact]
        public void Validate_WithDotDotSlashAtStart_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("./../file.txt"));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
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
    }
}