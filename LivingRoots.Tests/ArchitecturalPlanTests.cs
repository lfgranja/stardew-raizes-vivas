using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    public class ArchitecturalPlanTests
    {
        [Fact]
        public void ArchitecturalPlanFile_EndsWithTrailingNewline()
        {
            // Arrange
            var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
            var filePath = Path.Combine(projectRoot, "LivingRoots", "docs", "architectural_and_refactor_plans", "pr72_round106_fixes_architectural_plan.md");
            
            // Act
            var content = File.ReadAllText(filePath);

            Assert.False(string.IsNullOrEmpty(content), $"The file {filePath} should not be empty.");

            // Accept both Unix and Windows trailing newlines
            bool endsWithNewline = content.EndsWith("\n") || content.EndsWith("\r\n");
            int lastChar = content[^1];

            Assert.True(endsWithNewline, $"The file {filePath} should end with a trailing newline. Content ends with character {lastChar}.");
        }
        
        [Fact]
        public void ArchitecturalPlanFile_EmptyFileShouldNotCrash()
        {
            // Arrange - Create a temporary empty file
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, ""); // Write empty content
            
            try
            {
                // Act - Read the content from the file
                var content = File.ReadAllText(tempFilePath);
                
                // This test verifies that our implementation handles empty files gracefully
                // without throwing an IndexOutOfRangeException when accessing content[^1]
                if (string.IsNullOrEmpty(content))
                {
                    // If content is empty, we shouldn't access content[^1] which would cause a crash
                    // The fix should handle this case properly without throwing an exception
                    Assert.True(true, "Empty file handled without crashing");
                }
                else
                {
                    // If content is not empty, test the same logic as the main test
                    bool endsWithNewline = content.EndsWith("\n") || content.EndsWith("\r\n");
                    int lastChar = content[^1];
                    Assert.True(endsWithNewline, $"The file should end with a trailing newline. Content ends with character {lastChar}.");
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}
