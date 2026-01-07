using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;

namespace LivingRoots.Tests
{
    public class SpecificDotSegmentTest
    {
        private readonly PathTraversalValidator _validator;

        public SpecificDotSegmentTest()
        {
            // Create a real PathValidationService with a mock UnicodeNormalizationService
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            mockUnicodeService.Setup(s => s.Normalize(It.IsAny<string>())).Returns<string>(s => s);
            var pathValidationService = new PathValidationService(mockUnicodeService.Object);
            _validator = new PathTraversalValidator(pathValidationService);
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
            Assert.Contains("Path cannot contain path traversal patterns", exception.Message);
        }

        [Fact]
        public void Validate_SingleDotSegmentInPath_ShouldBeAllowed()
        {
            // This should be allowed because "folder/." is a valid path referring to folder directory
            // "." segments in paths are generally safe and needed for legitimate use cases
            var ex = Record.Exception(() => _validator.Validate("folder/."));
            Assert.Null(ex);
        }
    }
}
