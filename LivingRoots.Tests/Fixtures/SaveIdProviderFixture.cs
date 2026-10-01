using System;
using LivingRoots.Services;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Provides a <see cref="SaveIdProvider"/> bound to a controlled save folder value.
/// <para>
/// SMAPI's <c>Constants.SaveFolderName</c> is a computed property over live game state with
/// no setter and no backing field, so it cannot be assigned by tests. The fixture instead
/// injects the value through the provider's accessor seam, which keeps the test deterministic
/// without reflection into a static that does not exist.
/// </para>
/// </summary>
public class SaveIdProviderFixture : IDisposable
{
    /// <summary>The provider under test, reading the fixture's save folder value.</summary>
    public SaveIdProvider Provider { get; }

    private readonly string? _saveFolderName;

    /// <summary>
    /// Initializes the fixture with the given save folder name.
    /// </summary>
    /// <param name="saveFolderName">Value the provider should observe.</param>
    public SaveIdProviderFixture(string? saveFolderName)
    {
        _saveFolderName = saveFolderName;
        Provider = new SaveIdProvider(() => _saveFolderName);
    }

    /// <summary>
    /// Releases the fixture. Each fixture owns its own value, so there is no shared static state.
    /// </summary>
    public void Dispose()
    {
    }
}
