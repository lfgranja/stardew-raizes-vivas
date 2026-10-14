using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Tests for verifying feature status in README.md
    /// </summary>
    public class FeatureStatusTests
    {
        /// <summary>
        /// T198 (FR-016/SC-006): the README's `## Features` section must label
        /// only the capabilities the player CANNOT observe with `(Planned)` —
        /// and, symmetrically, must not label observable capabilities that way.
        /// The soil-health capabilities below became observable when Feature 004
        /// shipped the loop, so they are asserted present WITHOUT the label.
        /// </summary>
        [Fact]
        public void Readme_ObservableFeatures_AreNotLabelledPlanned()
        {
            // Arrange
            var expectedObservableFeatures = new[]
            {
                "Visual indicators show soil health status",
                "Health degrades over time when soil is left bare",
                "Health improves with compost application",
                "Composting bin: craft, place, load with organic waste, harvest compost after 2 in-game days"
            };

            // Arrange — capabilities with no in-game behaviour must carry the label.
            var expectedUnbuiltFeatures = new[]
            {
                "Learn about real-world soil management (Planned)",
                "Enhance your farming experience with educational elements (Planned)"
            };

            // Read README.md from embedded resource for reliable test access across all environments
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var readmeResourceName = resourceNames.FirstOrDefault(name => name.EndsWith("README.md"));

            Assert.True(readmeResourceName != null, $"README.md resource not found in assembly. Available resources: {string.Join(", ", resourceNames)}");

            // Act
            using var stream = assembly.GetManifestResourceStream(readmeResourceName);
            Assert.True(stream != null, $"Could not load README.md resource: {readmeResourceName}");

            using var reader = new StreamReader(stream);
            var readmeContent = reader.ReadToEnd();

            // Assert — observable capabilities claim no label...
            foreach (var feature in expectedObservableFeatures)
            {
                Assert.Contains(feature, readmeContent);
                Assert.DoesNotContain(feature + " (Planned)", readmeContent);
            }

            // ...and unbuilt ones carry it (FR-016 zero false claims).
            foreach (var feature in expectedUnbuiltFeatures)
            {
                Assert.Contains(feature, readmeContent);
            }

            // The retired-multiplier claim must not survive (FR-005a supersedes it).
            Assert.DoesNotContain("Different materials provide different benefits", readmeContent);
        }

        /// <summary>
        /// T231 (FR-016): every bullet under README.md `## Features` must either
        /// describe an observable capability or carry the explicit
        /// `(Planned)` label. README.md:20 "- Implement sustainable farming
        /// techniques" does neither — no `(Planned)` label while its
        /// neighbours carry one, and no observable referent in the shipped
        /// loop — so this fails until that bullet is labelled or given an
        /// observable referent. Fixed document assertion — property-based N/A
        /// (no input domain to generate over). The README is read from the
        /// repo root at test time (ProductionSourceGuardTests.LocateRepoRoot
        /// pattern), never a hardcoded copy.
        /// </summary>
        [Fact]
        public void Readme_FeatureBullets_AreObservableOrLabelledPlanned()
        {
            // Arrange — read the working-tree README at test time.
            var repoRoot = LocateRepoRoot();
            var readmePath = Path.Combine(repoRoot, "README.md");
            Assert.True(File.Exists(readmePath), $"README.md not found at {readmePath}.");

            var section = ExtractSection(File.ReadAllText(readmePath), "Features");
            Assert.True(section != null, "README.md has no ## Features section.");

            var bullets = ExtractBullets(section!);
            Assert.True(bullets.Count > 0, "README.md ## Features contains no bullets — the guard is vacuous.");

            // Capabilities with observable in-game behaviour (exact bullet
            // texts, without any label — the T198 observable set extended to
            // every shipped bullet under ## Features).
            var observableFeatures = new HashSet<string>
            {
                "Each farm tile has a persistent soil health value (0-100%)",
                "Health values are saved and loaded with the game",
                "Visual indicators show soil health status",
                "Health degrades over time when soil is left bare",
                "Health improves with compost application",
                "Create compost from organic materials",
                "Composting bin: craft, place, load with organic waste, harvest compost after 2 in-game days",
                "Apply compost to improve soil health"
            };

            // The curated set must describe the document, not drift from it.
            foreach (var feature in observableFeatures)
            {
                Assert.Contains(feature, section);
            }

            // Act + Assert — every bullet is either labelled (Planned) or
            // observably shipped; anything else is an unlabelled aspiration.
            var violations = new List<string>();
            foreach (var bullet in bullets)
            {
                var text = bullet.TrimStart().Substring(2).Trim();
                if (text.EndsWith("(Planned)", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!observableFeatures.Contains(text))
                {
                    violations.Add(text);
                }
            }

            Assert.True(
                violations.Count == 0,
                $"README.md ## Features bullets with no observable referent and no (Planned) label: {string.Join("; ", violations)}.");
        }

        /// <summary>
        /// Extracts the body of a `## {sectionTitle}` markdown section
        /// (lines until the next `## ` header), or `null` when the
        /// document has no such section.
        /// (ArchitecturalPlanTests.ExtractSection pattern.)
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
        /// (ArchitecturalPlanTests.ExtractBullets pattern.)
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
        /// `Stardew-LivingRoots.sln`, so the guard reads the source tree
        /// regardless of the runner's working directory.
        /// (ProductionSourceGuardTests.LocateRepoRoot pattern.)
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
