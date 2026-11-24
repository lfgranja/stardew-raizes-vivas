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
                var endsWithNewline = content.EndsWith('\n') || content.EndsWith("\r\n");

                Assert.True(endsWithNewline, $"The file {filePath} should end with a trailing newline. Content ends with character {(int)content[content.Length - 1]}.");
            }
        }

        public static IEnumerable<object[]> GetReconciliationDocuments()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string? projectRoot = null;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
                {
                    projectRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            if (projectRoot == null)
            {
                yield break;
            }

            var relativePaths = new[]
            {
                Path.Combine("specs", "001-soil-health-visualization", "spec.md"),
                Path.Combine("specs", "001-soil-health-visualization", "plan.md"),
                Path.Combine("specs", "002-soil-decay-compost", "spec.md"),
                Path.Combine("specs", "002-soil-decay-compost", "plan.md"),
                Path.Combine("specs", "003-composting-tests", "spec.md"),
                Path.Combine("specs", "003-composting-tests", "plan.md"),
                Path.Combine("docs", "design-system", "REALITY-CHECK-EVALUATION.md"),
                Path.Combine("docs", "design-system", "LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md")
            };

            foreach (var relPath in relativePaths)
            {
                var fullPath = Path.Combine(projectRoot, relPath);
                yield return new object[] { fullPath, relPath };
            }
        }

        [Theory]
        [MemberData(nameof(GetReconciliationDocuments))]
        public void SpecifiedDocuments_EndWithTrailingNewline(string fullPath, string relPath)
        {
            Assert.True(File.Exists(fullPath), $"Document {relPath} must exist at {fullPath}");

            var content = File.ReadAllText(fullPath);
            Assert.False(string.IsNullOrEmpty(content), $"Document {relPath} must not be empty.");

            var endsWithNewline = content.EndsWith('\n') || content.EndsWith("\r\n");
            Assert.True(endsWithNewline, $"The document {relPath} should end with a trailing newline.");
        }

        [Theory]
        [MemberData(nameof(GetReconciliationDocuments))]
        public void SpecifiedDocuments_ZeroMultiplayerScopeExclusions(string fullPath, string relPath)
        {
            // Per FR-017: No specification or plan document may declare multiplayer support out of scope
            // where that contradicts the project's constitution (Principle VI).
            // This checks the 8 stale documents for contradicting exclusion phrases.
            Assert.True(File.Exists(fullPath), $"Document {relPath} must exist at {fullPath}");

            var content = File.ReadAllText(fullPath);

            var forbiddenPatterns = new[]
            {
                "multiplayer sync is Out of Scope",
                "Single-player only (no multiplayer sync)",
                "Single-player game context only",
                "single-player scope",
                "Target Platform**: Stardew Valley via SMAPI (Windows/Linux/macOS, single-player)"
            };

            foreach (var pattern in forbiddenPatterns)
            {
                Assert.DoesNotContain(pattern, content, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static IEnumerable<object[]> GetDesignSystemDocuments()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string? projectRoot = null;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
                {
                    projectRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            if (projectRoot == null)
            {
                yield break;
            }

            var relativePaths = new[]
            {
                Path.Combine("docs", "design-system", "REALITY-CHECK-EVALUATION.md"),
                Path.Combine("docs", "design-system", "LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md")
            };

            foreach (var relPath in relativePaths)
            {
                var fullPath = Path.Combine(projectRoot, relPath);
                yield return new object[] { fullPath, relPath };
            }
        }

        [Theory]
        [MemberData(nameof(GetDesignSystemDocuments))]
        public void DesignSystemDocuments_ReferencePrincipleSixHostAuthoritativeFarmState(string fullPath, string relPath)
        {
            // Per FR-017: both docs/design-system/*.md documents must carry an explicit pointer to
            // the constitution's Principle VI (Multiplayer As A First-Class Requirement), under which
            // farm state is shared, host-authoritative state rather than per-player state.
            // Scope of this assertion is the POINTER only: FR-017 correction is limited to scope
            // wording and MUST NOT redesign the settled threading advice in these documents.
            Assert.True(File.Exists(fullPath), $"Document {relPath} must exist at {fullPath}");

            var content = File.ReadAllText(fullPath);
            Assert.False(string.IsNullOrEmpty(content), $"Document {relPath} must not be empty.");

            Assert.Contains(
                "Principle VI",
                content,
                StringComparison.OrdinalIgnoreCase);

            Assert.True(
                content.Contains("host-authoritative", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("host authoritative", StringComparison.OrdinalIgnoreCase),
                $"FR-017: document {relPath} must reference Principle VI host-authoritative farm state. "
                    + $"Expected a 'host-authoritative' (or 'host authoritative') reference alongside the "
                    + $"Principle VI pointer, so readers are not left believing farm state is per-player "
                    + $"state. Add the pointer; do NOT redesign the document's threading advice.");

            Assert.True(
                content.Contains("farm state", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("farm-state", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("farm's state", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("shared farm", StringComparison.OrdinalIgnoreCase),
                $"FR-017: document {relPath} must tie its Principle VI reference to farm state "
                    + $"(expected 'farm state', 'farm-state', \"farm's state\", or 'shared farm'). "
                    + $"Constitution Principle VI states farm state is shared and host-authoritative, "
                    + $"not owned by an individual player.");
        }

        public static IEnumerable<object[]> GetArchitecturalPlanFiles()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string? projectRoot = null;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
                {
                    projectRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            if (projectRoot == null)
            {
                // Can't find project root, so can't find docs.
                return Enumerable.Empty<object[]>();
            }
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
