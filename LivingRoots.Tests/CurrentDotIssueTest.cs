using System;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class CurrentDotIssueTest
    {
        private readonly PathTraversalValidator _validator;

        public CurrentDotIssueTest()
        {
            _validator = new PathTraversalValidator();
        }

        [Fact]
        public void Validate_CurrentImplementationAllowsLegitimateDotFiles()
        {
            // These legitimate uses of "." in filenames should now be allowed after the fix
            // Hidden files like ".config", ".env", etc. are legitimate and should be allowed
            _validator.Validate(".hidden");
            _validator.Validate(".config");
            _validator.Validate(".env");
            _validator.Validate(".gitignore");
            
            // Paths with "." segments in the middle should also be allowed
            _validator.Validate("folder/./file");
            _validator.Validate("path/to/./file.txt");
        }
    }
}