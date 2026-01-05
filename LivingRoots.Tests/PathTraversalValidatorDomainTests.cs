using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class PathTraversalValidatorDomainTests
    {
        private readonly IPathValidationService _service;

        public PathTraversalValidatorDomainTests()
        {
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            mockUnicodeService.Setup(s => s.Normalize(It.IsAny<string>())).Returns<string>(s => s);

            _service = new PathValidationService(mockUnicodeService.Object);
        }

        [Fact]
        public void Validate_WithValidRelativePath_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/file.txt"); // Should not throw
            _service.Validate("file.with.dots.txt");
            _service.Validate("config.local.json");
        }

        [Fact]
        public void Validate_WithDotSegmentsInMiddle_DoesNotThrow()
        {
            // Act & Assert
            _service.Validate("folder/.config/file.txt"); // Should not throw
            _service.Validate("path/to/./file.txt"); // Should not throw
            _service.Validate("normal/.hidden"); // Should not throw
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
        public void Validate_WithSingleDot_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("."));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_WithDotDotAtEnd_DoesNotThrow()
        {
            // Act & Assert
            // After refactoring, paths ending with ".." are allowed as long as they don't go above root
            // "folder/.." goes down 1 level then up 1 level, which doesn't go above root, so it's valid
            _service.Validate("folder/.."); // Should not throw
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
    }
}
