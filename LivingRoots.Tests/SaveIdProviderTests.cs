using System;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using Xunit;

namespace LivingRoots.Tests;

public class SaveIdProviderTests
{
    /// <summary>A valid save folder name returns the folder name itself.</summary>
    [Fact]
    public void GetSaveId_WithValidSaveFolder_ReturnsId()
    {
        using var fixture = new SaveIdProviderFixture("test_save");

        var result = fixture.Provider.GetSaveId();

        Assert.Equal("test_save", result);
    }

    /// <summary>A null save folder yields no id rather than throwing.</summary>
    [Fact]
    public void GetSaveId_WithNullSaveFolder_ReturnsNull()
    {
        using var fixture = new SaveIdProviderFixture(null);

        var result = fixture.Provider.GetSaveId();

        Assert.Null(result);
    }

    /// <summary>An empty save folder yields no id.</summary>
    [Fact]
    public void GetSaveId_WithEmptySaveFolder_ReturnsNull()
    {
        using var fixture = new SaveIdProviderFixture(string.Empty);

        var result = fixture.Provider.GetSaveId();

        Assert.Null(result);
    }

    /// <summary>A whitespace-only save folder yields no id.</summary>
    [Fact]
    public void GetSaveId_WithWhitespaceSaveFolder_ReturnsNull()
    {
        using var fixture = new SaveIdProviderFixture("   ");

        var result = fixture.Provider.GetSaveId();

        Assert.Null(result);
    }

    /// <summary>A save folder one character beyond the limit is rejected.</summary>
    [Fact]
    public void GetSaveId_WithTooLongSaveFolder_ReturnsNull()
    {
        using var fixture = new SaveIdProviderFixture(new string('x', ModConstants.MaxSaveIdLength + 1));

        var result = fixture.Provider.GetSaveId();

        Assert.Null(result);
    }

    /// <summary>A save folder exactly at the length limit is accepted.</summary>
    [Fact]
    public void GetSaveId_WithMaxLengthSaveFolder_ReturnsId()
    {
        var saveFolder = new string('x', ModConstants.MaxSaveIdLength);
        using var fixture = new SaveIdProviderFixture(saveFolder);

        var result = fixture.Provider.GetSaveId();

        Assert.Equal(saveFolder, result);
    }

    /// <summary>Repeated calls return the same id.</summary>
    [Fact]
    public void GetSaveId_MultipleCalls_ReturnsSameId()
    {
        using var fixture = new SaveIdProviderFixture("test_save");

        var result1 = fixture.Provider.GetSaveId();
        var result2 = fixture.Provider.GetSaveId();

        Assert.Equal(result1, result2);
    }

    /// <summary>A nested fixture does not clobber the outer fixture's value on dispose.</summary>
    [Fact]
    public void Dispose_RestoresEnclosingFixtureValue()
    {
        using var outer = new SaveIdProviderFixture("outer_save");

        using (var inner = new SaveIdProviderFixture("inner_save"))
        {
            Assert.Equal("inner_save", inner.Provider.GetSaveId());
        }

        Assert.Equal("outer_save", outer.Provider.GetSaveId());
    }

    /// <summary>An accessor that throws is swallowed rather than propagated.</summary>
    [Fact]
    public void GetSaveId_WhenAccessorThrows_ReturnsNull()
    {
        var provider = new SaveIdProvider(() => throw new InvalidOperationException("game state unavailable"));

        var result = provider.GetSaveId();

        Assert.Null(result);
    }

    /// <summary>A null accessor is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullAccessor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveIdProvider((Func<string?>)null!));
    }
}
