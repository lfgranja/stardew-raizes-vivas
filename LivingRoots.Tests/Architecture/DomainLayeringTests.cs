using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace LivingRoots.Tests.Architecture;

/// <summary>
/// Domain layering assertions (T172, Phase 20 Wave 1 — TDD Red).
/// The Domain layer is framework-agnostic: no source file under
/// <c>LivingRoots/Domain/</c> may declare a
/// <c>using StardewModdingAPI;</c> or <c>using StardewValley;</c>
/// directive — including sub-namespaces (<c>StardewValley.Menus</c>)
/// and aliases. XNA (<c>Microsoft.Xna.Framework</c>) usings are
/// exempt: value types carry no SMAPI runtime dependency.
/// </summary>
/// <remarks>
/// Scaffold: the source-locating walk-up (test binary →
/// <c>Stardew-LivingRoots.sln</c>) is copied from
/// <c>ArchitecturalPlanTests</c>; the offenders-list assertion style
/// is copied from <c>VisualizationPerScreenArchitectureTests</c>.
/// RED status (TDD): three files currently violate the rule —
/// <c>Domain/Services/OrganicWasteValidator.cs:2-3</c>
/// (<c>using StardewModdingAPI;</c> for <c>IMonitor</c> and
/// <c>using StardewValley;</c> for <c>Item</c>),
/// <c>Domain/Interfaces/IPlayerProvider.cs:1</c> and
/// <c>Domain/IPlayerInventory.cs:1</c> (<c>using StardewValley;</c>
/// for <c>Farmer</c>/<c>Item</c> in contracts). The production seam
/// these tests demand is the domain-purity migration: the SMAPI
/// surface (<c>IMonitor</c>) stays injected at the application layer
/// and farmer/item-shaped contracts move behind domain interfaces.
/// Every assertion below fails naming each offending file and line.
/// </remarks>
public class DomainLayeringTests
{
    /// <summary>
    /// The forbidden using prefixes. Sub-namespaces and aliases are
    /// caught by the containment match. XNA
    /// (<c>Microsoft.Xna.Framework</c>) is deliberately absent —
    /// value types only, no SMAPI runtime dependency.
    /// </summary>
    private static readonly string[] ForbiddenUsingPrefixes = new[]
    {
        "StardewModdingAPI",
        "StardewValley",
    };

    /// <summary>
    /// Guard: the solution-root walk must actually find the Domain
    /// tree, so a failed source location can never read as a
    /// vacuous green (zero test cases).
    /// </summary>
    [Fact]
    public void DomainSourceFiles_AreDiscoveredFromSolutionRoot()
    {
        var files = GetDomainSourceFiles().Select(data => (string)data[0]).ToList();

        Assert.NotEmpty(files);
    }

    /// <summary>
    /// T172: every Domain source file is free of SMAPI and
    /// StardewValley using directives. Each failing case names the
    /// file and every offending line number.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetDomainSourceFiles))]
    public void DomainFiles_DeclareNoSmApiOrStardewValleyUsings(string filePath)
    {
        // Act
        var lines = File.ReadAllLines(filePath);

        var offenders = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (IsForbiddenUsingDirective(lines[i]))
            {
                offenders.Add($"line {i + 1}: {lines[i].Trim()}");
            }
        }

        // Assert
        Assert.True(
            offenders.Count == 0,
            $"Domain file {filePath} must not reference SMAPI or StardewValley (framework-agnostic layer; XNA Microsoft.Xna.Framework usings are exempt). Offending usings: {string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// Locates every <c>.cs</c> file under <c>LivingRoots/Domain/</c>
    /// by walking up from the test binary to the solution root.
    /// </summary>
    public static IEnumerable<object[]> GetDomainSourceFiles()
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
            return Enumerable.Empty<object[]>();
        }

        var domainDirectory = Path.Combine(projectRoot, "LivingRoots", "Domain");
        if (!Directory.Exists(domainDirectory))
        {
            return Enumerable.Empty<object[]>();
        }

        return Directory.GetFiles(domainDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(filePath => new object[] { filePath });
    }

    /// <summary>
    /// Determines whether a source line is a using directive against
    /// a forbidden namespace (SMAPI or StardewValley, any
    /// sub-namespace or alias). Comments and XNA usings never match.
    /// </summary>
    /// <param name="line">The source line to inspect.</param>
    /// <returns>True when the line is a forbidden using directive.</returns>
    private static bool IsForbiddenUsingDirective(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("using ", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var prefix in ForbiddenUsingPrefixes)
        {
            if (trimmed.Contains(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
