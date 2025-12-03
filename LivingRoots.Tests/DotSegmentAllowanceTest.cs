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
    public class DotSegmentAllowanceTest
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IModLogic> _mockModLogic;
        private readonly Mock<IMonitor> _mockMonitor;

        public DotSegmentAllowanceTest()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockModLogic = new Mock<IModLogic>();
            
            // Set up DirectoryPath to return a valid path
            _mockHelper.Setup(x => x.DirectoryPath).Returns("/test/directory");
            
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
            
            // Configure mod logic to return expected sanitized values for testing
            _mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns<string>(input => 
            {
                // Simulate real sanitization behavior for individual segments
                if (input == "with:invalid|chars")
                    return "with_invalid_chars";
                if (input == "file....name")
                    return "file.name";
                if (input == "  test_key " || input == "test_key " || input == " test_key")
                    return "test_key";
                if (input == "<>:\"|?*" || input == "........." || input == "___" || input == "   ")
                    throw new ArgumentException("Filename sanitizes to an empty string.", nameof(input)); // This should throw like real implementation
                // Special handling for ".." which should be blocked at path segment level
                if (input == "..")
                    throw new ArgumentException($"Filename sanitizes to invalid path component '{input}'.", nameof(input));
                // For individual segments with invalid characters, replace them with underscores
                if (input.Contains(':') || input.Contains('|') || input.Contains('?') || input.Contains('*') || input.Contains('<') || input.Contains('>') || input.Contains('"'))
                {
                    return input.Replace(':', '_').Replace('|', '_').Replace('?', '_').Replace('*', '_')
                                   .Replace('<', '_').Replace('>', '_').Replace('"', '_');
                }
                // For segments with multiple dots, process them appropriately
                if (input.Contains("...."))
                {
                    return input.Replace("....", ".").Replace("...", "."); // Simplified processing for test
                }
                return input;
            });
            
            // Configure path validation to not throw for valid paths in most tests
            // but throw for path traversal attempts
            _mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Verifiable();
            _mockModLogic.Setup(x => x.ValidatePath(It.Is<string>(s => 
                s.Contains("../") || 
                s.Contains("..\\") || 
                s.Contains("../../../") || 
                s.Contains("..\\..\\") || 
                ContainsIgnoreCase(s, "http://") || 
                ContainsIgnoreCase(s, "https://") ||
                ContainsIgnoreCase(s, "ftp://") ||
                ContainsIgnoreCase(s, "file://")))).Throws<ArgumentException>();
        }
        
        // Helper method for case-insensitive string containment check
        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return false;
            
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        [Fact]
        public void SanitizePathSegments_WithDotSegments_SkipsDotSegmentsCorrectly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Configure mock to return expected sanitized values
            _mockModLogic.Setup(x => x.SanitizeFileName("segment1")).Returns("segment1");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment2")).Returns("segment2");
            _mockModLogic.Setup(x => x.SanitizeFileName("segment3")).Returns("segment3");
            
            // Use reflection to access private SanitizePathSegments method
            var method = service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act - Test path with dot segments that should be skipped
            var result = method?.Invoke(service, new object[] { "segment1/./segment2/./segment3" });
            
            // Assert - Dot segments should be skipped, resulting in only the valid segments
            Assert.Equal("segment1/segment2/segment3", result);
        }
        
        [Fact]
        public void SanitizePathSegments_WithOnlyDotSegments_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Use reflection to access private SanitizePathSegments method
            var method = service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act & Assert - Path with only dot segments should result in empty path and throw exception
            var exception = Assert.Throws<TargetInvocationException>(() => 
                method?.Invoke(service, new object[] { "././." }));
            
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains("Path sanitization resulted in empty path", exception.InnerException?.Message);
        }
        
        [Fact]
        public void SanitizePathSegments_WithMixedDotAndValidSegments_SkipsOnlyDotSegments()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Configure mock to return expected sanitized values
            _mockModLogic.Setup(x => x.SanitizeFileName("valid1")).Returns("valid1");
            _mockModLogic.Setup(x => x.SanitizeFileName("valid2")).Returns("valid2");
            
            // Use reflection to access private SanitizePathSegments method
            var method = service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act - Test path with mix of dot segments and valid segments
            var result = method?.Invoke(service, new object[] { "./valid1/./valid2/." });
            
            // Assert - Only the valid segments should remain
            Assert.Equal("valid1/valid2", result);
        }
        
        [Fact]
        public void SanitizePathSegments_WithDotSegmentAtBeginning_SkipsDotSegment()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Configure mock to return expected sanitized values
            _mockModLogic.Setup(x => x.SanitizeFileName("start")).Returns("start");
            _mockModLogic.Setup(x => x.SanitizeFileName("end")).Returns("end");
            
            // Use reflection to access private SanitizePathSegments method
            var method = service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act - Test path starting with dot segment
            var result = method?.Invoke(service, new object[] { "./start/end" });
            
            // Assert - Dot segment at beginning should be skipped
            Assert.Equal("start/end", result);
        }
        
        [Fact]
        public void SanitizePathSegments_WithDotSegmentAtEnd_SkipsDotSegment()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Configure mock to return expected sanitized values
            _mockModLogic.Setup(x => x.SanitizeFileName("start")).Returns("start");
            _mockModLogic.Setup(x => x.SanitizeFileName("end")).Returns("end");
            
            // Use reflection to access private SanitizePathSegments method
            var method = service.GetType()
                .GetMethod("SanitizePathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act - Test path ending with dot segment
            var result = method?.Invoke(service, new object[] { "start/end/." });
            
            // Assert - Dot segment at end should be skipped
            Assert.Equal("start/end", result);
        }
        
        [Fact]
        public void IsPathTraversalSegment_WithDotSegment_ReturnsFalse_AfterRemovalOfRedundantCheck()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Use reflection to access private IsPathTraversalSegment method
            var method = service.GetType()
                .GetMethod("IsPathTraversalSegment", BindingFlags.NonPublic | BindingFlags.Static);
            
            // Act - Test IsPathTraversalSegment with a dot segment
            var result = method?.Invoke(null, new object[] { "." });
            
            // Assert - After removing the redundant check, this should return false
            // because dot segments are handled in the calling method by skipping them
            Assert.False((bool)result);
        }
        
        [Fact]
        public void IsPathTraversalSegment_WithDotDotSegment_StillReturnsTrue()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object, _mockModLogic.Object);
            
            // Use reflection to access private IsPathTraversalSegment method
            var method = service.GetType()
                .GetMethod("IsPathTraversalSegment", BindingFlags.NonPublic | BindingFlags.Static);
            
            // Act - Test IsPathTraversalSegment with a dot-dot segment
            var result = method?.Invoke(null, new object[] { ".." });
            
            // Assert - This should still return true as it's a legitimate path traversal attempt
            Assert.True((bool)result);
        }
    }
}