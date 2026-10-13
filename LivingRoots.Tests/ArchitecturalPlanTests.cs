using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

        // =========================================================================
        // Phase 21 / Tasks T207-T209 (FR-017): markdown scope-guard assertions.
        // TDD red-first — each test below is written to FAIL against the current
        // documents and BLOCKS its matching remediation (T212/T213/T214) until
        // the wording is corrected. Fixed-document assertions — property-based
        // testing N/A (no input domain to generate over).
        // =========================================================================

        /// <summary>
        /// The eight stale specification/plan/design documents FR-017 names,
        /// relative to the repository root.
        /// </summary>
        private static readonly string[] Fr017Documents = new[]
        {
            Path.Combine("specs", "001-soil-health-visualization", "spec.md"),
            Path.Combine("specs", "001-soil-health-visualization", "plan.md"),
            Path.Combine("specs", "002-soil-decay-compost", "spec.md"),
            Path.Combine("specs", "002-soil-decay-compost", "plan.md"),
            Path.Combine("specs", "003-composting-tests", "spec.md"),
            Path.Combine("specs", "003-composting-tests", "plan.md"),
            Path.Combine("docs", "design-system", "REALITY-CHECK-EVALUATION.md"),
            Path.Combine("docs", "design-system", "LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md"),
        };

        /// <summary>
        /// T207 (F2, contradicts, FR-017): no FR-017 document's
        /// <c>## Out of Scope</c> section may carry an unqualified multiplayer
        /// exclusion. <c>specs/001-soil-health-visualization/spec.md</c> still
        /// lists "Multiplayer synchronization of visualization settings" as a
        /// bare exclusion bullet, contradicting Principle VI; the reconciled
        /// statements (specs/001:178, specs/002 plan:30, specs/003 R-2 rows)
        /// show the permitted shape — a qualified pointer to host-authoritative
        /// farm state per Principle VI. A bullet that mentions multiplayer must
        /// carry that qualification, otherwise it is an unqualified exclusion.
        /// </summary>
        [Fact]
        public void Fr017Documents_OutOfScopeSections_DeclareNoUnqualifiedMultiplayerExclusion()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var violations = new List<string>();

            // Act — scan the Out of Scope section of every FR-017 document.
            foreach (var relativePath in Fr017Documents)
            {
                var path = Path.Combine(repoRoot, relativePath);
                Assert.True(File.Exists(path), $"FR-017 document not found at {path}.");

                var section = ExtractSection(File.ReadAllText(path), "Out of Scope");
                if (section == null)
                {
                    continue; // no such section — nothing declared out of scope
                }

                foreach (var bullet in ExtractBullets(section))
                {
                    var mentionsMultiplayer = Regex.IsMatch(bullet, @"multiplayer", RegexOptions.IgnoreCase);
                    var isQualified = Regex.IsMatch(
                        bullet,
                        @"Principle\s+VI|not out of scope|host-authoritative|host authoritative|roadmap",
                        RegexOptions.IgnoreCase);

                    if (mentionsMultiplayer && !isQualified)
                    {
                        violations.Add($"{relativePath}: \"{bullet.Trim()}\"");
                    }
                }
            }

            // Assert
            Assert.True(
                violations.Count == 0,
                $"FR-017 violation — unqualified multiplayer exclusions remain: {string.Join("; ", violations)}.");
        }

        /// <summary>
        /// T208 (F3, contradicts, FR-017): the single-player assumption in
        /// <c>specs/002-soil-decay-compost/spec.md</c> still reads "Single-player
        /// game context only (no multiplayer sync required)" with no Principle VI
        /// reconciliation, while the matching <c>plan.md</c> constraint was
        /// already reconciled. The assumptions section must drop the stale
        /// phrase and carry the host-authoritative Principle VI wording its
        /// sibling documents use.
        /// </summary>
        [Fact]
        public void Specs002Spec_SinglePlayerAssumption_IsReconciledWithPrincipleVI()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var path = Path.Combine(repoRoot, "specs", "002-soil-decay-compost", "spec.md");
            Assert.True(File.Exists(path), $"specs/002 spec.md not found at {path}.");

            var assumptions = ExtractSection(File.ReadAllText(path), "Assumptions");
            Assert.True(assumptions != null, "specs/002-soil-decay-compost/spec.md has no Assumptions section.");

            // Assert — stale phrase gone, Principle VI reconciliation present.
            Assert.DoesNotMatch(
                new Regex(@"no multiplayer sync required", RegexOptions.IgnoreCase),
                assumptions!);
            Assert.Matches(
                new Regex(@"Principle\s+VI", RegexOptions.IgnoreCase),
                assumptions!);
            Assert.Matches(
                new Regex(@"host-authoritative", RegexOptions.IgnoreCase),
                assumptions!);
        }

        /// <summary>
        /// T209 (F4, partial, FR-017 / T092): both
        /// <c>docs/design-system/*.md</c> documents must reference Principle VI
        /// host-authoritative farm state, so a reader comparing the project's
        /// multiplayer scope against the constitution finds no contradiction.
        /// Neither document carries the pointer today (and no architectural
        /// test asserted it), so this guard also provides the test half of the
        /// previously checkbox-only T092.
        /// </summary>
        [Fact]
        public void DesignSystemDocs_ReferencePrincipleVIHostAuthoritativeFarmState()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var designSystemDocs = new[]
            {
                Path.Combine("docs", "design-system", "REALITY-CHECK-EVALUATION.md"),
                Path.Combine("docs", "design-system", "LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md"),
            };

            foreach (var relativePath in designSystemDocs)
            {
                var path = Path.Combine(repoRoot, relativePath);
                Assert.True(File.Exists(path), $"Design-system document not found at {path}.");

                var content = File.ReadAllText(path);

                // Assert — the Principle VI host-authoritative pointer, without
                // redesigning the documents' threading advice.
                var principleVI = new Regex(@"Principle\s+VI", RegexOptions.IgnoreCase);
                var hostAuthoritative = new Regex(@"host-authoritative", RegexOptions.IgnoreCase);
                Assert.True(
                    principleVI.IsMatch(content),
                    $"{relativePath} must reference Principle VI (FR-017).");
                Assert.True(
                    hostAuthoritative.IsMatch(content),
                    $"{relativePath} must reference host-authoritative farm state (FR-017, Principle VI).");
            }
        }

        /// <summary>
        /// Extracts the body of a <c>## {sectionTitle}</c> markdown section
        /// (lines until the next <c>## </c> header), or <c>null</c> when the
        /// document has no such section.
        /// </summary>
        private static string? ExtractSection(string markdown, string sectionTitle)
        {
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var inSection = false;
            var collected = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("## "))
                {
                    if (inSection)
                    {
                        break;
                    }

                    inSection = line.Contains(sectionTitle, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (inSection)
                {
                    collected.Add(line);
                }
            }

            return inSection ? string.Join("\n", collected) : null;
        }

        /// <summary>
        /// Extracts the bullet items ("- " / "* " prefixes) of a markdown
        /// fragment.
        /// </summary>
        private static List<string> ExtractBullets(string markdownFragment)
        {
            var bullets = new List<string>();
            foreach (var line in markdownFragment.Split('\n'))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    bullets.Add(line);
                }
            }

            return bullets;
        }

        /// <summary>
        /// Walks up from the test output directory to the directory containing
        /// <c>Stardew-LivingRoots.sln</c>, so document guards read the source
        /// tree regardless of the runner's working directory.
        /// </summary>
        private static string LocateRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Stardew-LivingRoots.sln")))
            {
                directory = directory.Parent;
            }

            var root = directory?.FullName;
            Assert.True(root != null, "Could not locate Stardew-LivingRoots.sln above the test output directory.");
            return root!;
        }


    }
}
