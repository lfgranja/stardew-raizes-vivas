using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    public class ArchitecturalPlanTests
    {
        [Fact]
        public void ArchitecturalPlan_ShouldEndWithTrailingNewline()
        {
            // Arrange
            var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
            var filePath = Path.Combine(projectRoot, "LivingRoots", "docs", "architectural_and_refactor_plans", "pr72_round106_fixes_architectural_plan.md");
            
            // Act
            var content = File.ReadAllText(filePath);
            
            // Debug: Print the last few characters to understand what we have
            var last10Chars = content.Length >= 10 ? content.Substring(content.Length - 10) : content;
            Console.WriteLine($"Last 10 characters: '{last10Chars}'");
            Console.WriteLine($"Last character as int: {(int)content[content.Length - 1]}");
            Console.WriteLine($"Ends with newline: {content.EndsWith("\n")}");
            
            // Assert - This should fail because the file currently does not end with a newline
            Assert.True(content.EndsWith("\n"), $"The file {filePath} should end with a trailing newline. Content ends with character {(int)content[content.Length - 1]}.");
        }
    }
}
