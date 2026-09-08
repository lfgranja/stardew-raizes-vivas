using LivingRoots.Domain;

namespace LivingRoots.Tests.Stubs;

/// <summary>
/// Stub implementation of ISeasonProvider for season-dependent tests.
/// Allows tests to control the current season by setting CurrentSeason.
/// </summary>
public class SeasonProviderStub : ISeasonProvider
{
    /// <summary>Gets or sets the current season for testing.</summary>
    public string CurrentSeason { get; set; } = "spring";
}
