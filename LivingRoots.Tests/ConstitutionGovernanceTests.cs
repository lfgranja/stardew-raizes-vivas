using System.IO;
using System.Text.RegularExpressions;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T206 (F1, CRITICAL, contradicts): the governing constitution on disk must
    /// not be stale relative to the ratified v2.0.0 amendment.
    /// <c>.specify/memory/constitution.md</c> still reads v1.1.0 and declares
    /// "Single-player only—multiplayer sync is architecturally out of scope",
    /// while the amendment adding Principle VI (Multiplayer As A First-Class
    /// Requirement) — recorded in
    /// <c>.specify/assessments/next-feature-80-20/research.md</c> ("the
    /// constitution was amended to v2.0.0 mid-assessment, adding Principle VI")
    /// and referenced by <c>docs/GDD-living-roots.md</c>, <c>plan.md</c>'s
    /// Constitution Check and FR-017/FR-018/FR-019 — is absent from the file.
    /// Until this guard passes, every FR-017 document reconciliation lacks a
    /// stable referent. TDD red-first: this test is written to FAIL against the
    /// current file and BLOCKS T211 (the amendment) until the constitution
    /// records v2.0.0 with Principle VI. Fixed-document assertion — property-
    /// based testing N/A (no input domain to generate over).
    /// </summary>
    public class ConstitutionGovernanceTests
    {
        [Fact]
        public void Constitution_OnDisk_IsNotStaleAgainstRatifiedV2Amendment()
        {
            // Arrange — locate the constitution from the repository root, found
            // by walking up from the test output directory to the solution file
            // (the ArchitecturalPlanTests pattern), so the guard does not pass
            // or fail on where the runner happened to start.
            var repoRoot = LocateRepoRoot();
            var constitutionPath = Path.Combine(repoRoot, ".specify", "memory", "constitution.md");
            Assert.True(File.Exists(constitutionPath), $"Constitution not found at {constitutionPath}.");

            var content = File.ReadAllText(constitutionPath);

            // Assert 1 — the multiplayer-first principle this whole feature
            // (FR-017/FR-018/FR-019, SC-005/SC-010) is governed by must exist
            // in the file, not only in the assessments and GDD.
            var principleVI = new Regex(@"Principle\s+VI", RegexOptions.IgnoreCase);
            Assert.True(
                principleVI.IsMatch(content),
                "The constitution on disk must contain Principle VI (Multiplayer As A First-Class Requirement), ratified as the v2.0.0 amendment.");

            // Assert 2 — the recorded version must be the ratified amendment,
            // not the pre-amendment 1.1.0.
            var versionV2 = new Regex(@"\*\*Version\*\*:\s*2\.0\.0");
            Assert.True(
                versionV2.IsMatch(content),
                "The constitution on disk must record version 2.0.0 (the ratified amendment), not the stale 1.1.0.");

            // Assert 3 — the single-player-only scope constraint that Principle
            // VI superseded must not survive in the Technology & Architecture
            // Constraints section.
            var singlePlayerOnly = new Regex(@"Single-player\s+only", RegexOptions.IgnoreCase);
            Assert.True(
                !singlePlayerOnly.IsMatch(content),
                "The constitution on disk must not declare 'Single-player only' scope; Principle VI (v2.0.0) supersedes that constraint.");
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
