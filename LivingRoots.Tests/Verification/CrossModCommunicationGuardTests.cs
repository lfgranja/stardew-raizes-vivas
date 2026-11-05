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
        public void ProductionSources_VerificationMod_HasNoScenarioInjectionSendMessageOrModMessageReceived()
        {
            // Arrange — scan the verification companion mod only. The main mod
            // legitimately uses SendMessage/ModMessageReceived for host-authoritative
            // soil-health sync (FR-009/A8); only LivingRoots.Verification is
            // forbidden from using them for scenario injection/signal capture.
            var repoRoot = LocateRepoRoot();
            var verificationRoot = Path.Combine(repoRoot, "LivingRoots.Verification");
            Assert.True(Directory.Exists(verificationRoot), $"Verification mod root not found at {verificationRoot}.");

            var violations = new List<string>();
            var scanned = 0;

            foreach (var file in Directory.EnumerateFiles(verificationRoot, "*.cs", SearchOption.AllDirectories))
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

            Assert.True(scanned > 0, "No verification mod sources were scanned — the guard is vacuous.");
            Assert.True(
                violations.Count == 0,
                "FR-009 violation — these LivingRoots.Verification files use SendMessage/ModMessageReceived (only ModRegistry.GetApi<T> is allowed): " + string.Join(", ", violations));
        }

        [Fact]
        public void LivingRootsMainMod_HasHostScopedSoilHealthSyncChannel()
        {
            // Arrange — the main mod must contain a host-scoped soil-health sync
            // channel using helper.Multiplayer.SendMessage and ModMessageReceived
            // (FR-009/A8). This is RED today because no such channel exists yet.
            var repoRoot = LocateRepoRoot();
            var mainRoot = Path.Combine(repoRoot, "LivingRoots");
            Assert.True(Directory.Exists(mainRoot), $"Main mod root not found at {mainRoot}.");

            var hasSend = false;
            var hasReceive = false;

            foreach (var file in Directory.EnumerateFiles(mainRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(file))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                if (Regex.IsMatch(content, @"\bSendMessage\b"))
                {
                    hasSend = true;
                }

                if (Regex.IsMatch(content, @"\bModMessageReceived\b"))
                {
                    hasReceive = true;
                }
            }

            Assert.True(hasSend, "LivingRoots must contain a host-scoped soil-health sync send channel.");
            Assert.True(hasReceive, "LivingRoots must contain a ModMessageReceived handler for soil-health sync.");
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
