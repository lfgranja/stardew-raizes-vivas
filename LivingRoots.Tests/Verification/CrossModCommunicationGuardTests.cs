using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LivingRoots.Tests.Verification
{
    /// <summary>
    /// T183 (FR-009): no production code path may use SendMessage/ModMessageReceived
    /// for scenario injection, signal capture, or dump orchestration; the only
    /// in-process cross-mod mechanism is ModRegistry.GetApi&lt;T&gt;.
    /// </summary>
    public class CrossModCommunicationGuardTests
    {
        [Fact]
        public void ProductionSources_NoSendMessageOrModMessageReceived_ForScenarioInjectionSignalCaptureOrDumpOrchestration()
        {
            // Arrange — scan the real production trees (main mod + verification
            // companion), never build output.
            var repoRoot = LocateRepoRoot();
            var productionRoots = new[]
            {
                Path.Combine(repoRoot, "LivingRoots"),
                Path.Combine(repoRoot, "LivingRoots.Verification")
            };

            var violations = new List<string>();
            var scanned = 0;

            // Act — quantify over every production source file.
            foreach (var root in productionRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    if (IsBuildOutput(file))
                    {
                        continue;
                    }

                    scanned++;
                    var content = File.ReadAllText(file);
                    var relative = Path.GetRelativePath(repoRoot, file);

                    if (Regex.IsMatch(content, @"\bSendMessage\b"))
                    {
                        violations.Add(relative + " (SendMessage)");
                    }

                    if (Regex.IsMatch(content, @"\bModMessageReceived\b"))
                    {
                        violations.Add(relative + " (ModMessageReceived)");
                    }
                }
            }

            // Assert — the guard must never be vacuous, and no production file
            // may use SendMessage/ModMessageReceived for scenario injection,
            // signal capture, or dump orchestration.
            Assert.True(scanned > 0, "No production sources were scanned — the guard is vacuous.");
            Assert.True(
                violations.Count == 0,
                "FR-009 violation — these production files use SendMessage/ModMessageReceived (only ModRegistry.GetApi<T> is allowed for scenario injection, signal capture, or dump orchestration): " + string.Join(", ", violations));
        }

        [Fact]
        public void LivingRootsVerificationMod_UsesOnlyModRegistryGetApi_NoSendMessageOrModMessageReceived()
        {
            // Arrange — scan the verification companion mod specifically.
            var repoRoot = LocateRepoRoot();
            var verificationRoot = Path.Combine(repoRoot, "LivingRoots.Verification");
            Assert.True(Directory.Exists(verificationRoot), $"Verification mod root not found at {verificationRoot}.");

            var violations = new List<string>();
            var scanned = 0;

            // Act.
            foreach (var file in Directory.EnumerateFiles(verificationRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                scanned++;
                var content = File.ReadAllText(file);
                var relative = Path.GetRelativePath(verificationRoot, file);

                if (content.Contains("SendMessage") || content.Contains("ModMessageReceived"))
                {
                    violations.Add(relative);
                }
            }

            // Assert.
            Assert.True(scanned > 0, "No verification mod sources were scanned — the guard is vacuous.");
            Assert.True(
                violations.Count == 0,
                "FR-009 violation — LivingRoots.Verification uses SendMessage/ModMessageReceived (only ModRegistry.GetApi<T> is allowed): " + string.Join(", ", violations));
        }

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
