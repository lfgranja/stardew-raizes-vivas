using System;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class DotSegmentAllowanceTest
    {
        private readonly PathTraversalValidator _validator;

        public DotSegmentAllowanceTest()
        {
            _validator = new PathTraversalValidator();
        }

        [Fact]
        public void Validate_PathWithDotSegment_ShouldBeAllowed_AfterFix()
        {
            // These should be allowed after the fix because "." represents current directory
            // and is generally safe in path contexts, important for hidden files like ".config", ".env", etc.
            // This test will fail with current implementation but should pass after fix
            _validator.Validate("folder/./file");
            _validator.Validate("normal/.hidden");
            _validator.Validate("path/to/./file.txt");
        }

        [Fact]
        public void Validate_SingleDotPath_ShouldStillBeBlocked()
        {
            // This should still be blocked as it's just navigating to current directory
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("."));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }

        [Fact]
        public void Validate_PathStartingWithDotSlash_ShouldStillBeBlocked()
        {
            // This should still be blocked as it's explicit path navigation
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("./file"));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }

        [Fact]
        public void Validate_PathEndingWithDot_ShouldStillBeBlocked()
        {
            // This should still be blocked as it's explicit path navigation
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("file/."));
            Assert.Contains("Path cannot contain relative path navigation", exception.Message);
        }
    }
}