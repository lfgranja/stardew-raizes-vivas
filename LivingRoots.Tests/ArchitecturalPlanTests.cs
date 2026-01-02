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

                Assert.True(endsWithNewline, $"The file {filePath} should end with a trailing newline. Content ends with character {(int)content[content.Length - 1]}.");
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


    }
}
