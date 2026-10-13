using System.IO;
using System.Text.RegularExpressions;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T210 (F5, partial, FR-015): production sources must never write raw
    /// exception stack-trace detail into the mod's output — failures are
    /// reported as <c>GetType().FullName</c> + <c>HResult</c> at
    /// <c>LogLevel.Trace</c> only. T097 removed the offending log sites and
    /// T200 claimed a source-scan guard, but the guard itself never existed,
    /// so the invariant was unprotected against regression. This test is the
    /// guard: a universal property over the production file tree — for every
    /// <c>.cs</c> file under <c>LivingRoots/</c> (build output excluded), no
    /// member access resolves a <c>StackTrace</c>. The generated domain is the
    /// real source set, so the assertion quantifies over it; a regression
    /// reintroducing <c>ex.StackTrace</c> fails here. Observed state at
    /// authoring: GREEN — the remediation already landed (verified zero
    /// occurrences across <c>LivingRoots/</c>); the red-first intent is
    /// satisfied by the fact that the guard did not exist to fail, and its
    /// value is preventing reintroduction.
    /// </summary>
    public class ProductionSourceGuardTests
    {
        [Fact]
        public void ProductionSources_ContainNoStackTraceAccess()
        {
            // Arrange — scan the real production tree, never build output.
            var productionRoot = Path.Combine(LocateRepoRoot(), "LivingRoots");
            Assert.True(Directory.Exists(productionRoot), $"Production root not found at {productionRoot}.");

            var violations = new List<string>();
            var scanned = 0;

            // Act — quantify over every production source file.
            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                scanned++;
                var content = File.ReadAllText(file);
                if (Regex.IsMatch(content, @"\b\w+\.StackTrace\b"))
                {
                    violations.Add(Path.GetRelativePath(productionRoot, file));
                }
            }

            // Assert — the guard must never be vacuous, and no production file
            // may reach into an exception's stack trace.
            Assert.True(scanned > 0, "No production sources were scanned — the guard is vacuous.");
            Assert.True(
                violations.Count == 0,
                $"FR-015 violation — these production files access StackTrace: {string.Join(", ", violations)}.");
        }

        /// <summary>
        /// True when the path belongs to build output (<c>bin/</c> or
        /// <c>obj/</c>) on either path separator, so generated artifacts are
        /// never treated as production sources.
        /// </summary>
        private static bool IsBuildOutput(string path)
        {
            foreach (var segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (segment == "bin" || segment == "obj")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Walks up from the test output directory to the directory containing
        /// <c>Stardew-LivingRoots.sln</c> (the ArchitecturalPlanTests pattern),
        /// so the guard reads the source tree regardless of the runner's
        /// working directory.
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
