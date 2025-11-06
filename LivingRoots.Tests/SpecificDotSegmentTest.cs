using System;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class SpecificDotSegmentTest
    {
        private readonly PathTraversalValidator _validator;

        public SpecificDotSegmentTest()
        {
            _validator = new PathTraversalValidator();
        }

        [Fact]
        public void Validate_SpecificDotSegmentInPath_ShouldBeAllowed()
        {
            // This should be allowed now because "/./" in the middle of a path is safe
            // "." segments in the middle of paths are generally safe and needed for legitimate use cases
            _validator.Validate("folder/./file");
            _validator.Validate("path/to/./file.txt");
            _validator.Validate("normal/.hidden");
        }

        [Fact]
        public void Validate_SingleDotSegment_ShouldStillFail()
        {
            // This should still fail because it's just "." representing current directory navigation
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("."));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }

        [Fact]
        public void Validate_SingleDotSegmentInPath_ShouldStillFail()
        {
            // This should still fail because it ends with "." as explicit path navigation
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("folder/."));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }
    }
}