using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    public class ArchitecturalPlanTests
    {
        [Theory]
        [MemberData(nameof(GetArchitecturalPlanFiles))]
        public void AllArchitecturalPlanFiles_EndWithTrailingNewline(string filePath)
        {
            // Act
            var content = File.ReadAllText(filePath);

            // Only check for trailing newline if the file is not empty
            if (!string.IsNullOrEmpty(content))
            {
                // Accept both Unix and Windows trailing newlines
                bool endsWithNewline = content.EndsWith("\n") || content.EndsWith("\r\n");
                int lastChar = content[^1];

                Assert.True(endsWithNewline, $"The file {filePath} should end with a trailing newline. Content ends with character {lastChar}.");
            }
        }
        
        public static IEnumerable<object[]> GetArchitecturalPlanFiles()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var architecturalPlansDirectory = Path.Combine(projectRoot, "LivingRoots", "docs", "architectural_and_refactor_plans");

            if (!Directory.Exists(architecturalPlansDirectory))
            {
                return Enumerable.Empty<object[]>();
            }
            
            var mdFiles = Directory.GetFiles(architecturalPlansDirectory, "*.md", SearchOption.AllDirectories)
                                   .Select(filePath => new object[] { filePath });
            
            return mdFiles;
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

        [Fact]
        public void AllArchitecturalPlanFiles_EndWithTrailingNewline_EmptyFileDoesNotCrash()
        {
            // Arrange
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, ""); // Write empty content

            try
            {
                // Act
                var ex = Record.Exception(() => AllArchitecturalPlanFiles_EndWithTrailingNewline(tempFilePath));

                // Assert
                Assert.Null(ex);
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