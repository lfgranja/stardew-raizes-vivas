using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T234 (FR-001a/FR-005, fixed file-set): every texture path served through
    /// <c>e.LoadFromModFile&lt;Texture2D&gt;("Assets/...")</c> in
    /// <c>LivingRoots/ModEntry.cs</c> (the compost icon plus the bin
    /// empty/processing/ready sprites) must exist as a source file under
    /// <c>LivingRoots/Assets/</c> AND be covered by a csproj
    /// <c>Content</c> + <c>CopyToOutputDirectory</c> directive mirroring the
    /// existing <c>manifest.json</c> / <c>i18n/*.json</c> entries, so the PNGs
    /// ship to the mod folder. The expected set is derived by scanning
    /// <c>ModEntry.cs</c> for the <c>LoadFromModFile</c> literals, not by
    /// restating the filenames. Property-based testing N/A (fixed set).
    /// Observed state at authoring: RED — <c>LivingRoots/Assets/</c> does not
    /// exist (PNGs live only under the machine-specific
    /// <c>.gamepath/Mods/LivingRoots/Assets/</c>) and the csproj copies only
    /// manifest.json + i18n.
    /// </summary>
    public class AssetPresenceTests
    {
        [Fact]
        public void T234_TextureAssets_ExistInSourceAndAreCopiedToBuildOutput()
        {
            // Arrange — locate the source tree regardless of runner cwd.
            var repoRoot = LocateRepoRoot();
            var modEntryPath = Path.Combine(repoRoot, "LivingRoots", "ModEntry.cs");
            Assert.True(File.Exists(modEntryPath), $"ModEntry.cs not found at {modEntryPath}.");

            // Derive the expected set from the LoadFromModFile literals.
            var modEntrySource = File.ReadAllText(modEntryPath);
            var matches = Regex.Matches(modEntrySource, @"LoadFromModFile<Texture2D>\(\s*""([^""]+)""");
            var assetPaths = matches
                .Select(m => m.Groups[1].Value)
                .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            // Guard against a vacuous scan (e.g. literal syntax drift).
            Assert.True(assetPaths.Count > 0, "No LoadFromModFile<Texture2D>(\"Assets/...\") literals found in ModEntry.cs — the scan is vacuous.");

            // Act — check source presence and csproj copy coverage per asset.
            var missingFiles = new List<string>();
            foreach (var assetPath in assetPaths)
            {
                var sourcePath = Path.Combine(repoRoot, "LivingRoots", assetPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                {
                    missingFiles.Add(assetPath);
                }
            }

            var csprojPath = Path.Combine(repoRoot, "LivingRoots", "LivingRoots.csproj");
            Assert.True(File.Exists(csprojPath), $"LivingRoots.csproj not found at {csprojPath}.");
            var contentDirectives = LoadContentDirectives(csprojPath);

            var uncoveredAssets = new List<string>();
            foreach (var assetPath in assetPaths)
            {
                if (!IsCoveredByContentDirective(assetPath, contentDirectives))
                {
                    uncoveredAssets.Add(assetPath);
                }
            }

            // Assert — both halves fail today: no Assets/ sources, no copy directives.
            Assert.True(
                missingFiles.Count == 0,
                $"T234 violation — texture sources missing under LivingRoots/: {string.Join(", ", missingFiles)}.");
            Assert.True(
                uncoveredAssets.Count == 0,
                $"T234 violation — textures not covered by a csproj Content + CopyToOutputDirectory directive: {string.Join(", ", uncoveredAssets)}.");
        }

        /// <summary>
        /// A single csproj <c>Content</c> entry: its <c>Include</c> pattern and
        /// its <c>CopyToOutputDirectory</c> value.
        /// </summary>
        private sealed class ContentDirective
        {
            public string IncludePattern = string.Empty;

            public string CopyToOutputDirectory = string.Empty;
        }

        /// <summary>
        /// Parses the csproj XML for <c>Content</c> entries and their
        /// <c>CopyToOutputDirectory</c> values.
        /// </summary>
        private static List<ContentDirective> LoadContentDirectives(string csprojPath)
        {
            var directives = new List<ContentDirective>();
            var document = XDocument.Load(csprojPath);
            foreach (var element in document.Descendants("Content"))
            {
                var include = element.Attribute("Include");
                if (include == null)
                {
                    continue;
                }

                var directive = new ContentDirective();
                directive.IncludePattern = include.Value.Replace('\\', '/');
                var copyElement = element.Element("CopyToOutputDirectory");
                if (copyElement != null)
                {
                    directive.CopyToOutputDirectory = copyElement.Value.Trim();
                }

                directives.Add(directive);
            }

            return directives;
        }

        /// <summary>
        /// True when a <c>Content</c> directive with a copy-to-output value
        /// (mirroring the <c>manifest.json</c> / <c>i18n/*.json</c>
        /// <c>PreserveNewest</c> convention) covers the asset, either by exact
        /// match or by wildcard (<c>*</c>/<c>?</c>) pattern.
        /// </summary>
        private static bool IsCoveredByContentDirective(string assetPath, List<ContentDirective> directives)
        {
            foreach (var directive in directives)
            {
                var copiesToOutput = directive.CopyToOutputDirectory.Equals("PreserveNewest", StringComparison.OrdinalIgnoreCase)
                    || directive.CopyToOutputDirectory.Equals("Always", StringComparison.OrdinalIgnoreCase);
                if (!copiesToOutput)
                {
                    continue;
                }

                if (directive.IncludePattern.Equals(assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (directive.IncludePattern.Contains('*') || directive.IncludePattern.Contains('?'))
                {
                    var pattern = "^" + Regex.Escape(directive.IncludePattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    if (Regex.IsMatch(assetPath, pattern, RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Walks up from the test output directory to the directory containing
        /// <c>Stardew-LivingRoots.sln</c> (the ArchitecturalPlanTests /
        /// ProductionSourceGuardTests pattern), so the guard reads the source
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
