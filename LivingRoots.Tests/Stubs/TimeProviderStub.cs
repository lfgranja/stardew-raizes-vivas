using LivingRoots.Domain;

namespace LivingRoots.Tests.Stubs;

/// <summary>
/// Stub implementation of ITimeProvider for time-dependent tests.
/// Allows tests to control the current day by setting TotalDays.
/// </summary>
public class TimeProviderStub : ITimeProvider
{
    /// <summary>Gets or sets the current total days for testing.</summary>
    public int TotalDays { get; set; } = 1;
}
