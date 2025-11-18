using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using LivingRoots.Domain;
using Xunit;
using System;
using System.IO;
using System.Reflection;

namespace LivingRoots.Tests
{
    public class SanitizePathSegmentsTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IModLogic> _mockModLogic;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly ModDataService _service;

        public SanitizePathSegmentsTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockModLogic = new Mock<IModLogic>();
            
            _service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
        }

        [Fact]
        public void SanitizePathSegments_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(_service, new object[] { null! }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("path", ((ArgumentException)exception.InnerException).ParamName);
            Assert.Contains("Path cannot be null", exception.InnerException.Message);
        }

        [Fact]
        public void SanitizePathSegments_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(_service, new object[] { "" }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("path", ((ArgumentException)exception.InnerException).ParamName);
            Assert.Contains("Path cannot be empty", exception.InnerException.Message);
        }

        [Fact]
        public void SanitizePathSegments_WithWhitespaceOnlyPath_ThrowsArgumentException()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(_service, new object[] { "   " }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("path", ((ArgumentException)exception.InnerException).ParamName);
            Assert.Contains("Sanitized path cannot be empty", exception.InnerException.Message);
        }

        [Fact]
        public void SanitizePathSegments_WithAllSegmentsSanitizingToEmpty_ThrowsArgumentException()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Configure mock to return null for sanitization (simulating empty result)
            _mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns((string)null);
            
            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(_service, new object[] { "segment1" }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("path", ((ArgumentException)exception.InnerException).ParamName);
            Assert.Contains("Sanitized path cannot be empty", exception.InnerException.Message);
        }

        [Fact]
        public void SanitizePathSegments_WithAllSegmentsSanitizingToWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Configure mock to return whitespace for sanitization (simulating empty result)
            _mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns("   ");
            
            // Act & Assert
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(_service, new object[] { "segment1" }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("path", ((ArgumentException)exception.InnerException).ParamName);
            Assert.Contains("Sanitized path cannot be empty", exception.InnerException.Message);
        }

        [Fact]
        public void SanitizePathSegments_WithValidPath_ReturnsSanitizedPath()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Configure mock to return valid sanitized segments
            _mockModLogic.Setup(x => x.SanitizeFileName("segment1")).Returns("segment1");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment2")).Returns("segment2");
            
            // Act
            var result = method?.Invoke(_service, new object[] { "segment1/segment2" });
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("segment1/segment2", result.ToString().Replace('\\', '/'));
        }
        
        [Fact]
        public void SanitizePathSegments_WithPlatformSpecificSeparators_ReturnsSanitizedPath()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Configure mock to return valid sanitized segments
            _mockModLogic.Setup(x => x.SanitizeFileName("segment1")).Returns("segment1");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment2")).Returns("segment2");
            
            // Act - Test with backslash separators (Windows-style)
            var result = method?.Invoke(_service, new object[] { @"segment1\segment2" });
            
            // Assert
            Assert.NotNull(result);
            // Convert result to forward slashes for comparison to ensure consistency
            Assert.Equal("segment1/segment2", result.ToString().Replace('\\', '/'));
        }
        
        [Fact]
        public void SanitizePathSegments_WithMixedPathSeparators_ReturnsSanitizedPath()
        {
            // Arrange
            var method = _service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Configure mock to return valid sanitized segments
            _mockModLogic.Setup(x => x.SanitizeFileName("segment1")).Returns("segment1");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment2")).Returns("segment2");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment3")).Returns("segment3");
            
            // Act - Test with mixed separators
            var result = method?.Invoke(_service, new object[] { @"segment1/segment2\segment3" });
            
            // Assert
            Assert.NotNull(result);
            // The result should handle mixed separators properly
            var normalizedResult = result.ToString()
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
            // The actual result should be "segment1/segment2/segment3" after processing
            Assert.Equal("segment1/segment2/segment3", normalizedResult);
        }
    }
}