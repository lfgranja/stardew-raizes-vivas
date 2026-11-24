using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class CurrentDotIssueTest
    {
        private readonly PathTraversalValidator _validator;

        public CurrentDotIssueTest()
        {
            var mockPathValidationService = new Mock<IPathValidationService>();
            _validator = new PathTraversalValidator(mockPathValidationService.Object);
        }

        [Fact]
        public void Validate_CurrentImplementationAllowsLegitimateDotFiles()
        {
            // These legitimate uses of "." in filenames should now be allowed after fix
            // Hidden files like ".config", ".env", etc. are legitimate and should be allowed
            _validator.Validate(".hidden");
            _validator.Validate(".config");
            _validator.Validate(".env");
            _validator.Validate(".gitignore");

            // Paths with "." segments in middle should also be allowed
            _validator.Validate("folder/./file");
            _validator.Validate("path/to/./file.txt");
        }
    }
}
