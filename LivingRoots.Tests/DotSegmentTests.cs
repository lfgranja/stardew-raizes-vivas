using System;
using Xunit;
using LivingRoots.Services;
using LivingRoots.Domain;

namespace LivingRoots.Tests
{
    public class DotSegmentTests
    {
        private readonly PathTraversalValidator _validator;

        public DotSegmentTests()
        {
            _validator = new PathTraversalValidator(new PathValidationService());
        }

        [Fact]
        public void Validate_HiddenFilesWithDotSegments_ShouldBeAllowed()
        {
            // These are legitimate uses of "." in filenames that should be allowed
            _validator.Validate(".hidden");
            _validator.Validate(".config");
            _validator.Validate(".env");
            _validator.Validate(".gitignore");
            _validator.Validate(".hidden_file.txt");
            _validator.Validate(".config.json");
            _validator.Validate("folder/.hidden");
            _validator.Validate("folder/.config");
        }

        [Fact]
        public void Validate_DotInFilenames_ShouldBeAllowed()
        {
            // These are legitimate uses of "." as part of filenames
            _validator.Validate("file.with.dots.txt");
            _validator.Validate("document.txt");
            _validator.Validate("archive.tar.gz");
            _validator.Validate("config.local.json");
            _validator.Validate("app.settings.xml");
        }

        [Fact]
        public void Validate_ExplicitCurrentDirectoryTraversal_ShouldBeTreatedCorrectly()
        {
            // "./file" should be blocked (caught by start path checks)
            var exception1 = Assert.Throws<ArgumentException>(() => _validator.Validate("./file"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            // But "file/." should be allowed (as it refers to a directory)
            var ex1 = Record.Exception(() => _validator.Validate("file/."));
            Assert.Null(ex1);
            
            // And "folder/./file" should be allowed (middle "." segments are safe)
            _validator.Validate("folder/./file"); // This should not throw
            _validator.Validate("path/to/./file.txt");  // This should not throw
            _validator.Validate("normal/.hidden");  // This should not throw
        }

        [Fact]
        public void Validate_SingleDotAsPath_ShouldBeBlocked()
        {
            // A single "." represents current directory navigation and should be blocked
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate("."));
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_PathWithDotAtStartOrEnd_ShouldBeTreatedCorrectly()
        {
            // Paths that start with "." should be blocked as they represent directory navigation
            var exception1 = Assert.Throws<ArgumentException>(() => _validator.Validate("./path"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);
            
            // But paths that end with "." should be allowed as they refer to directories
            var ex1 = Record.Exception(() => _validator.Validate("path/."));
            Assert.Null(ex1);
        }
    }
}