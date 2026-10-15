using System;
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
        /// T241 (F22, partial, Constitution Governance): <c>.specify/memory/constitution.md:8-10</c>
        /// still carries the pre-merge flag "amendment merges only after human
        /// review and approval. Until ratified it is drafted, not governing",
        /// contradicting the ratified footer (<c>**Version**: 2.0.0 |
        /// **Ratified**: 2026-09-05 | **Last Amended**: 2026-10-13</c> at :76),
        /// and the header reports the wrong amended date. The flag must be gone
        /// and the header and footer amendment dates must agree. TDD red-first:
        /// this test is written to FAIL against the current file and BLOCKS T268
        /// (delete the stale flag, align the dates). Fixed-document assertion —
        /// property-based testing N/A (no input domain to generate over).
        /// </summary>
        [Fact]
        public void Constitution_CarriesNoStaleGovernanceFlagAndHeaderFooterDatesAgree()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var constitutionPath = Path.Combine(repoRoot, ".specify", "memory", "constitution.md");
            Assert.True(File.Exists(constitutionPath), $"Constitution not found at {constitutionPath}.");

            var content = File.ReadAllText(constitutionPath);

            // Assert 1 — the pre-merge governance flag (:8-10) must be gone now
            // that the v2.0.0 amendment is ratified (:76).
            var staleFlag = new Regex(@"Until ratified|not governing", RegexOptions.IgnoreCase);
            Assert.True(
                !staleFlag.IsMatch(content),
                ".specify/memory/constitution.md:8-10 must carry no stale \"Until ratified ... not governing\" flag; the v2.0.0 amendment is ratified (.specify/memory/constitution.md:76).");

            // Assert 2 — the header's amended date must agree with the ratified
            // footer's Last Amended date (:76).
            var footerDate = new Regex(@"\*\*Last Amended\*\*:\s*(\d{4}-\d{2}-\d{2})");
            var footerMatch = footerDate.Match(content);
            Assert.True(
                footerMatch.Success,
                ".specify/memory/constitution.md:76 must record a Last Amended date in the ratified footer.");

            var headerEnd = content.IndexOf("-->", StringComparison.Ordinal);
            Assert.True(
                headerEnd >= 0,
                ".specify/memory/constitution.md header must keep its Sync Impact Report block (:1-11) so its amended date can be checked against the footer.");
            var header = content.Substring(0, headerEnd);
            Assert.True(
                header.Contains(footerMatch.Groups[1].Value, StringComparison.Ordinal),
                $".specify/memory/constitution.md header (:1-11) amended date must agree with the ratified footer :76 (Last Amended {footerMatch.Groups[1].Value}).");
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
