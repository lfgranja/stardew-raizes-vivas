using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class MinDepthRefactoringTest
    {
        private readonly PathValidationService _service;

        public MinDepthRefactoringTest()
        {
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            mockUnicodeService.Setup(s => s.Normalize(It.IsAny<string>())).Returns<string>(s => s);
            
            _service = new PathValidationService(mockUnicodeService.Object, new PathTraversalValidator());
        }

        [Fact]
        public void Validate_PathWithMultipleDotDotSegments_ThrowsImmediatelyWhenDepthGoesNegative()
        {
            // This test verifies that paths that go negative in depth immediately throw an exception
            // This confirms that minDepth check at the end is redundant since depth < 0 check
            // in loop already catches traversal attempts
            
            // Test case where depth goes negative: "folder/../../file.txt"
            // folder = depth 1
            // .. = depth 0  
            // .. = depth -1 -> throws immediately
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_PathWithMultipleConsecutiveDotDotSegments_ThrowsImmediatelyWhenDepthGoesNegative()
        {
            // Test case: "../../../file.txt"
            // .. = depth -1 -> throws immediately
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("../../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_PathWithDotDotAtRootLevel_ThrowsArgumentException()
        {
            // Test case: "../file.txt" - this should fail immediately when depth goes negative
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_ValidPathWithDotDotThatDoesNotGoNegative_DoesNotThrow()
        {
            // This path should be valid: "folder/../file.txt" 
            // folder = depth 1
            // .. = depth 0 (back to root level, not negative)
            // So this should NOT throw
            _service.Validate("folder/../file.txt");
        }

        [Fact]
        public void Validate_PathThatEndsInDotDotButNeverGoesNegative_ThrowsArgumentException()
        {
            // This path should throw: "folder/.." 
            // folder = depth 1
            // .. = depth 0 (back to root level, not negative)
            // But algorithm throws because it ends with ".." which is considered a traversal attempt
            var exception = Assert.Throws<ArgumentException>(() => _service.Validate("folder/.."));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }
    }
}