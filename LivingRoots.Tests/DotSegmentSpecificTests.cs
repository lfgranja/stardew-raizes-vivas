using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class DotSegmentSpecificTests
    {
        private readonly PathValidationService _service;

        public DotSegmentSpecificTests()
        {
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            mockUnicodeService.Setup(s => s.Normalize(It.IsAny<string>())).Returns<string>(s => s);

            _service = new PathValidationService(mockUnicodeService.Object);
        }

        [Fact]
        public void Validate_DotSegmentsInMiddle_ShouldBeAllowed()
        {
            // These should be allowed - "." in middle of paths is safe
            _service.Validate("folder/./file.txt");
            _service.Validate("path/to/./file.txt");
            _service.Validate("folder/.config/file.txt"); // Hidden directory with dot
            _service.Validate("normal/.hidden"); // Hidden file in subdirectory
        }

        [Fact]
        public void Validate_MultipleDotSegmentsInMiddle_ShouldBeAllowed()
        {
            // These should be allowed - multiple "." segments in middle of paths
            _service.Validate("folder/./subfolder/./file.txt");
            _service.Validate("a/./b/./c.txt");
            _service.Validate("path/to/./deep/./file.txt");
        }

        [Fact]
        public void Validate_HiddenFilesAtEnd_ShouldBeAllowed()
        {
            // These should be allowed - hidden files and directories
            _service.Validate(".hidden");
            _service.Validate(".config");
            _service.Validate(".env");
            _service.Validate(".gitignore");
            _service.Validate("folder/.hidden");
            _service.Validate("folder/.config");
        }

        [Fact]
        public void Validate_ValidFilenamesWithDots_ShouldBeAllowed()
        {
            // These should be allowed - legitimate uses of dots in filenames
            _service.Validate("file.with.dots.txt");
            _service.Validate("document.txt");
            _service.Validate("archive.tar.gz");
            _service.Validate("config.local.json");
            _service.Validate("app.settings.xml");
        }

        [Fact]
        public void Validate_ExplicitCurrentDirectoryAtStart_ShouldBeAllowed()
        {
            // After removing overly restrictive check, these should be allowed - they are relative paths to current directory
            var ex1 = Record.Exception(() => _service.Validate("./file"));
            Assert.Null(ex1);

            var ex2 = Record.Exception(() => _service.Validate("./path/to/file.txt"));
            Assert.Null(ex2);
        }

        [Fact]
        public void Validate_ExplicitCurrentDirectoryAtEnd_ShouldBeAllowed()
        {
            // These should be allowed - "file/." is a valid path referring to file directory
            var ex1 = Record.Exception(() => _service.Validate("file/."));
            Assert.Null(ex1);

            var ex2 = Record.Exception(() => _service.Validate("path/to/file/."));
            Assert.Null(ex2);
        }

        [Fact]
        public void Validate_StandaloneDotOrDotSlash_ShouldBeBlocked()
        {
            // These should still be blocked - standalone navigation
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("."));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);

            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("./"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);
        }

        [Fact]
        public void Validate_PathTraversalWithDot_ShouldStillBeBlocked()
        {
            // These should still be blocked - path traversal with ".."
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);

            var exception2 = Assert.Throws<ArgumentException>(() => _service.Validate("../../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception2.Message);

            // This path should be allowed as it doesn't go above root level
            var ex3 = Record.Exception(() => _service.Validate("folder/../file.txt"));
            Assert.Null(ex3);
        }

        [Fact]
        public void Validate_PathWithMixedDotAndDotDot_ShouldBeBlocked()
        {
            // This should still be blocked - "./../" goes above root level
            var exception1 = Assert.Throws<ArgumentException>(() => _service.Validate("./../file.txt"));
            Assert.Contains("Path cannot contain path traversal patterns", exception1.Message);

            // This path should be allowed - it doesn't go above root level: folder(1) -> .(1) -> ..(0) -> file(1)
            var ex2 = Record.Exception(() => _service.Validate("folder/./../file.txt"));
            Assert.Null(ex2);
        }

        [Fact]
        public void Validate_PathWithDotAtVariousPositions_ShouldBeTreatedCorrectly()
        {
            // Valid cases - "." in middle and at end, and now also at start (after removing overly restrictive check)
            _service.Validate("folder/./file");
            _service.Validate("a/./b/./c.txt");
            _service.Validate("file/."); // This should be allowed as it refers to directory
            _service.Validate("./file"); // This should now be allowed as it refers to current directory
        }
    }
}
