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

        // =========================================================================
        // Phase 22 / Task T230 (F10, contradicts, FR-017): markdown scope-guard
        // assertions. TDD red-first — each test below is written to FAIL against
        // the current documents and BLOCKS T256 until the four unreconciled
        // "single-player" statements are wording-corrected and both specs/001
        // Constitution Check tables gain a Principle VI row. Fixed-literal
        // assertions — property-based testing N/A (no input domain to
        // generate over).
        // =========================================================================

        /// <summary>
        /// T230 (F10, contradicts, FR-017): <c>specs/001-soil-health-visualization/spec.md:110</c>
        /// (clarification answer: tile identity) and <c>:147</c> (Soil Health Tile
        /// entity) still read "...single-player scope" with no Principle VI
        /// reconciliation, while <c>:178</c> and the Out of Scope section show the
        /// permitted shape. Each cited line must carry the Principle VI pointer.
        /// </summary>
        [Fact]
        public void Spec001Spec_SinglePlayerScopeSentences_CarryPrincipleVIReconciliation()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var path = Path.Combine(repoRoot, "specs", "001-soil-health-visualization", "spec.md");
            Assert.True(File.Exists(path), $"specs/001-soil-health-visualization/spec.md not found at {path}.");

            var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
            Assert.True(lines.Length >= 147, $"specs/001-soil-health-visualization/spec.md must keep the cited lines (:110 and :147); file has only {lines.Length} lines.");

            var principleVI = new Regex(@"Principle\s+VI", RegexOptions.IgnoreCase);

            // Assert — spec.md:110 (tile-identity clarification answer).
            var line110 = lines[109];
            Assert.True(
                principleVI.IsMatch(line110),
                $"specs/001-soil-health-visualization/spec.md:110 must carry the Principle VI reconciliation (FR-017); actual line: \"{line110.Trim()}\".");

            // Assert — spec.md:147 (Soil Health Tile entity).
            var line147 = lines[146];
            Assert.True(
                principleVI.IsMatch(line147),
                $"specs/001-soil-health-visualization/spec.md:147 must carry the Principle VI reconciliation (FR-017); actual line: \"{line147.Trim()}\".");
        }

        /// <summary>
        /// T230 (F10, contradicts, FR-017): <c>specs/002-soil-decay-compost/plan.md:21</c>
        /// and <c>specs/003-composting-tests/plan.md:21</c> still read
        /// "(Windows/Linux/macOS, single-player)". Neither Target Platform line
        /// may declare single-player scope; the reconciled wording names the
        /// cross-platform targets without the single-player qualifier.
        /// </summary>
        [Fact]
        public void Plans002And003_TargetPlatformLines_DoNotDeclareSinglePlayerScope()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var targetPlatformFiles = new[]
            {
                Path.Combine("specs", "002-soil-decay-compost", "plan.md"),
                Path.Combine("specs", "003-composting-tests", "plan.md"),
            };
            var singlePlayer = new Regex(@"single-player", RegexOptions.IgnoreCase);

            // Act + Assert — one assertion per cited line.
            foreach (var relativePath in targetPlatformFiles)
            {
                var path = Path.Combine(repoRoot, relativePath);
                Assert.True(File.Exists(path), $"{relativePath} not found at {path}.");

                var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                Assert.True(lines.Length >= 21, $"{relativePath} must keep the cited Target Platform line (:21); file has only {lines.Length} lines.");

                var line21 = lines[20];
                Assert.True(
                    !singlePlayer.IsMatch(line21),
                    $"{relativePath}:21 must not read \"single-player\" (FR-017); actual line: \"{line21.Trim()}\".");
            }
        }

        /// <summary>
        /// T230 (F10, contradicts, FR-017): both Constitution Check tables in
        /// <c>specs/001-soil-health-visualization/plan.md</c> (the pre-design gate
        /// at :35-43 and the post-design re-evaluation at :45-55) list Principles
        /// I-V only. Each table must contain a Principle VI row.
        /// </summary>
        [Fact]
        public void Spec001Plan_BothConstitutionCheckTables_ContainPrincipleVIRow()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var relativePath = Path.Combine("specs", "001-soil-health-visualization", "plan.md");
            var path = Path.Combine(repoRoot, relativePath);
            Assert.True(File.Exists(path), $"{relativePath} not found at {path}.");

            var content = File.ReadAllText(path);
            var preDesignStart = content.IndexOf("## Constitution Check", StringComparison.Ordinal);
            var postDesignStart = content.IndexOf("### Post-Design Re-evaluation", StringComparison.Ordinal);
            var projectStructureStart = content.IndexOf("## Project Structure", StringComparison.Ordinal);
            Assert.True(
                preDesignStart >= 0 && postDesignStart > preDesignStart && projectStructureStart > postDesignStart,
                $"{relativePath} must keep both Constitution Check tables (pre-design gate and post-design re-evaluation).");

            var principleVI = new Regex(@"Principle\s+VI", RegexOptions.IgnoreCase);

            // Assert — first table (plan.md:35-43, pre-design gate).
            var preDesignTable = content.Substring(preDesignStart, postDesignStart - preDesignStart);
            Assert.True(
                principleVI.IsMatch(preDesignTable),
                $"{relativePath}:35-43 (pre-design Constitution Check table) must contain a Principle VI row (FR-017).");

            // Assert — second table (plan.md:45-55, post-design re-evaluation).
            var postDesignTable = content.Substring(postDesignStart, projectStructureStart - postDesignStart);
            Assert.True(
                principleVI.IsMatch(postDesignTable),
                $"{relativePath}:45-55 (post-design Constitution Check re-evaluation table) must contain a Principle VI row (FR-017).");
        }

        // =========================================================================
        // Phase 22 / Tasks T242-T243 (F23, F24): project-structure guards.
        // TDD red-first — each test below is written to FAIL against the current
        // tree and BLOCKS its matching remediation (T269/T270). Fixed file-set
        // and literal assertions — property-based testing N/A.
        // =========================================================================

        /// <summary>
        /// T242 (F23, partial, plan project structure): <c>specs/004-playable-compost-loop/plan.md:95</c>
        /// names <c>LivingRoots.Tests/Invariants/</c> and tasks T009/T016/T027 name
        /// the three invariant files inside it (all checked [X]), but the folder
        /// does not exist today — the 18 <c>[Property]</c> FsCheck tests live in
        /// top-level files. The three declared paths must exist (contents are
        /// T269's concern, not this guard's).
        /// </summary>
        [Fact]
        public void TestsInvariantsFolder_ContainsDeclaredPropertyBasedInvariantFiles()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var expectedFiles = new[]
            {
                Path.Combine("LivingRoots.Tests", "Invariants", "BinCraftingInvariantsTests.cs"),
                Path.Combine("LivingRoots.Tests", "Invariants", "CompostingMachineInvariantsTests.cs"),
                Path.Combine("LivingRoots.Tests", "Invariants", "SoilHealthInvariantsTests.cs"),
            };

            // Act + Assert — existence at the declared paths, not contents.
            foreach (var relativePath in expectedFiles)
            {
                var fullPath = Path.Combine(repoRoot, relativePath);
                Assert.True(
                    File.Exists(fullPath),
                    $"{relativePath} must exist per specs/004-playable-compost-loop/plan.md:95 and tasks T009/T016/T027 (F23); expected file at {fullPath}.");
            }
        }

        /// <summary>
        /// T243 (F24, unrequested, Constitution V): <c>LivingRoots/AGENTS.md:13</c>
        /// and <c>LivingRoots/Controllers/AGENTS.md:10</c> still list
        /// <c>Controllers/CompostingBinController.cs</c>, deleted by the spec 003
        /// research decision. Every <c>*.cs</c> path the code maps mention is
        /// resolved against the filesystem: relative to the AGENTS.md file,
        /// relative to <c>LivingRoots/</c>, by basename anywhere under
        /// <c>LivingRoots/</c> (build outputs excluded), or as a declared type
        /// (covers <c>ModConstants.cs</c>, whose <c>ModConstants</c> class lives
        /// in <c>Constants.cs</c>).
        /// </summary>
        [Fact]
        public void AgentsMdCodeMaps_ReferenceOnlyExistingFiles()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var livingRootsDir = Path.Combine(repoRoot, "LivingRoots");
            var agentsDocs = new[]
            {
                Path.Combine("LivingRoots", "AGENTS.md"),
                Path.Combine("LivingRoots", "Controllers", "AGENTS.md"),
            };
            var csToken = new Regex(@"(?<![\w/.-])([A-Za-z0-9_][\w/.-]*\.cs)\b");
            var violations = new List<string>();

            // Pre-read every production source once (build outputs excluded) so
            // bare mentions can fall back to a declared-type match.
            var productionSources = ReadProductionSources(livingRootsDir);

            // Act — resolve every *.cs token in both code maps.
            foreach (var relativePath in agentsDocs)
            {
                var agentsPath = Path.Combine(repoRoot, relativePath);
                Assert.True(File.Exists(agentsPath), $"AGENTS.md code map not found at {agentsPath}.");

                var agentsDir = Path.GetDirectoryName(agentsPath);
                Assert.True(agentsDir != null, $"Could not determine the directory of {agentsPath}.");

                var docLines = File.ReadAllText(agentsPath).Replace("\r\n", "\n").Split('\n');
                for (int lineIndex = 0; lineIndex < docLines.Length; lineIndex++)
                {
                    foreach (Match match in csToken.Matches(docLines[lineIndex]))
                    {
                        var token = match.Groups[1].Value.Replace('\\', '/');
                        if (token.Contains("*") || token.Contains("{") || token.Contains("}"))
                        {
                            continue;
                        }

                        if (!ResolvesToExistingFileOrDeclaredType(token, agentsDir!, livingRootsDir, productionSources))
                        {
                            violations.Add($"{relativePath}:{lineIndex + 1}: '{token}'");
                        }
                    }
                }
            }

            // Assert
            Assert.True(
                violations.Count == 0,
                $"AGENTS.md code maps must list no non-existent files (F24 — LivingRoots/AGENTS.md:13 and LivingRoots/Controllers/AGENTS.md:10 still list the deleted Controllers/CompostingBinController.cs): {string.Join("; ", violations)}.");
        }

        /// <summary>
        /// T243 (F24, unrequested, Constitution V): <c>LivingRoots.Tests/BinPlacementTests.cs:779, 851, 927</c>
        /// still carry stale "RED today" remarks (pending markers) for
        /// implemented, green behaviour. No test file may retain such a marker
        /// tied to an already-implemented task; markers for genuinely pending
        /// work (e.g. T098/T129) are out of scope for this guard.
        /// </summary>
        [Fact]
        public void TestFiles_CarryNoStaleRedTodayRemarksForGreenBehaviour()
        {
            // Arrange
            var repoRoot = LocateRepoRoot();
            var testsDir = Path.Combine(repoRoot, "LivingRoots.Tests");
            Assert.True(Directory.Exists(testsDir), $"LivingRoots.Tests directory not found at {testsDir}.");

            var staleTaskIds = new[] { "T100", "T105", "T127" };
            var violations = new List<string>();

            // Act — scan every test source for RED-today markers tied to the
            // already-implemented tasks.
            foreach (var file in Directory.GetFiles(testsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcludedTestPath(testsDir, file))
                {
                    continue;
                }

                var testLines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
                for (int lineIndex = 0; lineIndex < testLines.Length; lineIndex++)
                {
                    var line = testLines[lineIndex];
                    if (line.IndexOf("RED today", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    foreach (var taskId in staleTaskIds)
                    {
                        if (line.IndexOf(taskId, StringComparison.Ordinal) >= 0)
                        {
                            var relativeFile = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
                            violations.Add($"{relativeFile}:{lineIndex + 1} ({taskId}): \"{line.Trim()}\"");
                        }
                    }
                }
            }

            // Assert
            Assert.True(
                violations.Count == 0,
                $"No test file may retain a \"RED today\" remark for already-implemented (green) behaviour (F24 — LivingRoots.Tests/BinPlacementTests.cs:779, :851, :927): {string.Join("; ", violations)}.");
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
        /// Concatenates the contents of every production <c>*.cs</c> file under
        /// <paramref name="livingRootsDir"/>, excluding build outputs, so bare
        /// code-map mentions can fall back to a declared-type match.
        /// </summary>
        private static string ReadProductionSources(string livingRootsDir)
        {
            var contents = new List<string>();
            foreach (var file in Directory.GetFiles(livingRootsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcludedProductionPath(livingRootsDir, file))
                {
                    continue;
                }

                contents.Add(File.ReadAllText(file));
            }

            return string.Join("\n", contents);
        }

        /// <summary>
        /// Resolves a <c>*.cs</c> code-map token against the filesystem:
        /// relative to the AGENTS.md file, relative to <c>LivingRoots/</c>, by
        /// basename anywhere under <c>LivingRoots/</c> (build outputs excluded),
        /// or as a declared type in the production sources.
        /// </summary>
        private static bool ResolvesToExistingFileOrDeclaredType(string token, string agentsDir, string livingRootsDir, string productionSources)
        {
            if (File.Exists(Path.Combine(agentsDir, token)))
            {
                return true;
            }

            if (File.Exists(Path.Combine(livingRootsDir, token)))
            {
                return true;
            }

            var fileName = token.Substring(token.LastIndexOf('/') + 1);
            foreach (var file in Directory.GetFiles(livingRootsDir, fileName, SearchOption.AllDirectories))
            {
                if (!IsExcludedProductionPath(livingRootsDir, file))
                {
                    return true;
                }
            }

            var stem = fileName.Substring(0, fileName.Length - ".cs".Length);
            var declaration = new Regex(@"\b(class|interface|enum|struct)\s+" + Regex.Escape(stem) + @"\b");
            return declaration.IsMatch(productionSources);
        }

        /// <summary>
        /// Reports whether a file under <c>LivingRoots/</c> is a build output
        /// rather than a production source.
        /// </summary>
        private static bool IsExcludedProductionPath(string livingRootsDir, string file)
        {
            var relativePath = file.Substring(livingRootsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var segment in segments)
            {
                if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reports whether a file under <c>LivingRoots.Tests/</c> is generated
        /// output rather than a test source.
        /// </summary>
        private static bool IsExcludedTestPath(string testsDir, string file)
        {
            var relativePath = file.Substring(testsDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var segment in segments)
            {
                if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase) || segment.Equals("TestResults", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
