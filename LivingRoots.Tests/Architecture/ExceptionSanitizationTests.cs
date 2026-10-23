using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace LivingRoots.Tests.Architecture;

/// <summary>
/// Exception-sanitization assertions (T087, Phase 11 Wave 12 — TDD
/// Red) for FR-015: "The mod MUST NOT write raw exception detail into
/// its output; failures MUST be reported in a form that does not
/// expose internal content." The sanctioned sanitized form is
/// <c>GetType().FullName</c> plus <c>HResult</c> at
/// <c>LogLevel.Trace</c> (Controllers/AGENTS.md and
/// Services/AGENTS.md anti-patterns). No production source file under
/// <c>LivingRoots/</c> may therefore carry the <c>.StackTrace</c>
/// token: the <c>#if DEBUG</c> guards around the current sites do not
/// exempt them, because FR-015 has no debug carve-out and a stack
/// trace is internal content whenever it is written. The test project
/// tree and the <c>bin/</c>/<c>obj/</c> build outputs are excluded
/// from the scan.
/// </summary>
/// <remarks>
/// Scaffold: the solution-root walk-up and the offenders-list assertion
/// style are copied from <c>DomainLayeringTests</c> (T172); the
/// per-file source-reading helper follows
/// <c>NoHardcodedVisualizationLiteralsTests</c> (T176). RED status
/// (TDD): eight <c>ex.StackTrace</c> log sites exist today —
/// <c>Controllers/ModController.cs</c> lines 328, 841, 890, 1926, 1987
/// and 2033, and <c>Services/SoilHealthService.cs</c> lines 103 and
/// 518. The production seam this test demands is T097 (Wave 13): remove
/// the eight stack-trace log calls while keeping the
/// <c>GetType().FullName</c> + <c>HResult</c> Trace lines. Every
/// failing assertion below names FR-015 and each offending line.
/// </remarks>
public class ExceptionSanitizationTests
{
    /// <summary>
    /// The forbidden token: member access on the <c>StackTrace</c>
    /// property of any exception-shaped object (<c>ex.StackTrace</c>,
    /// <c>ctx.Exception.StackTrace</c>, <c>Environment.StackTrace</c>,
    /// ...). The leading dot keeps the scan off the
    /// <c>System.Diagnostics.StackTrace</c> type name and off prose.
    /// </summary>
    private const string ForbiddenStackTraceToken = ".StackTrace";

    /// <summary>
    /// Guard: the solution-root walk must actually find the production
    /// tree, so a failed source location can never read as a vacuous
    /// green (zero test cases).
    /// </summary>
    [Fact]
    public void ProductionSourceFiles_AreDiscoveredFromSolutionRoot()
    {
        var files = GetProductionSourceFiles().Select(data => (string)data[0]).ToList();

        Assert.NotEmpty(files);
    }

    /// <summary>
    /// T087 / FR-015: no production source file may carry the
    /// <c>.StackTrace</c> token. Each failing case names the file and
    /// every offending line number.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetProductionSourceFiles))]
    public void ProductionFiles_WriteNoStackTraceIntoOutput(string filePath)
    {
        // Act
        var lines = File.ReadAllLines(filePath);

        var offenders = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(ForbiddenStackTraceToken, StringComparison.Ordinal))
            {
                offenders.Add($"line {i + 1}: {lines[i].Trim()}");
            }
        }

        // Assert
        Assert.True(
            offenders.Count == 0,
            $"FR-015: production file {filePath} must not write raw exception detail into its output — the only sanctioned exception trace is GetType().FullName + HResult at LogLevel.Trace. Offending .StackTrace sites: {string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// The complementary half of FR-015's logging contract: the
    /// exception reporting that remains must be the sanitized
    /// <c>GetType().FullName</c> + <c>HResult</c> form. T097 (Wave 13)
    /// removes the stack-trace lines while keeping these tokens, so this
    /// assertion must stay green across the remediation.
    /// </summary>
    [Fact]
    public void ExceptionTraceLogging_RemainsGetTypeFullNameAndHResult()
    {
        // The two production files that report caught exceptions.
        var controllerSource = ReadProductionSource(
            "LivingRoots",
            "Controllers",
            "ModController.cs");
        var soilHealthSource = ReadProductionSource(
            "LivingRoots",
            "Services",
            "SoilHealthService.cs");

        Assert.Contains("GetType().FullName", controllerSource, StringComparison.Ordinal);
        Assert.Contains("HResult", controllerSource, StringComparison.Ordinal);
        Assert.Contains("GetType().FullName", soilHealthSource, StringComparison.Ordinal);
        Assert.Contains("HResult", soilHealthSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// Locates every production <c>.cs</c> file under
    /// <c>LivingRoots/</c> by walking up from the test binary to the
    /// solution root, excluding the test project tree and build
    /// outputs.
    /// </summary>
    public static IEnumerable<object[]> GetProductionSourceFiles()
    {
        if (!TryGetSolutionRoot(out var solutionRoot))
        {
            return Enumerable.Empty<object[]>();
        }

        var modDirectory = Path.Combine(solutionRoot, "LivingRoots");
        if (!Directory.Exists(modDirectory))
        {
            return Enumerable.Empty<object[]>();
        }

        return Directory.GetFiles(modDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSource)
            .Select(filePath => new object[] { filePath });
    }

    /// <summary>
    /// Walks up from the test output directory to the solution root.
    /// Scaffold copied from ArchitecturalPlanTests's sln-walk.
    /// </summary>
    private static bool TryGetSolutionRoot(out string solutionRoot)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
            {
                solutionRoot = dir.FullName;
                return true;
            }

            dir = dir.Parent;
        }

        solutionRoot = string.Empty;
        return false;
    }

    /// <summary>
    /// Determines whether a discovered source file is production code:
    /// the test project tree and the <c>bin/</c>/<c>obj/</c> build
    /// outputs are not production sources.
    /// </summary>
    /// <param name="filePath">The discovered file path.</param>
    /// <returns>True when the file is a production source file.</returns>
    private static bool IsProductionSource(string filePath)
    {
        var segments = filePath.Split(Path.DirectorySeparatorChar);
        return !segments.Contains("LivingRoots.Tests", StringComparer.Ordinal)
            && !segments.Contains("bin", StringComparer.Ordinal)
            && !segments.Contains("obj", StringComparer.Ordinal);
    }

    /// <summary>
    /// Reads a production source file located relative to the solution
    /// root, failing the test when the file cannot be found.
    /// </summary>
    /// <param name="relativePathSegments">Path segments below the solution root.</param>
    /// <returns>The file's full text.</returns>
    private static string ReadProductionSource(params string[] relativePathSegments)
    {
        Assert.True(
            TryGetSolutionRoot(out var solutionRoot),
            "Could not locate Stardew-LivingRoots.sln above the test output directory.");

        var fullPath = Path.Combine(solutionRoot, Path.Combine(relativePathSegments));
        Assert.True(File.Exists(fullPath), $"Production source file not found: {fullPath}");
        return File.ReadAllText(fullPath);
    }
}
