using System;
using System.Collections.Generic;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit tests for <see cref="CompostingBinStateModel"/> progress retention and modData serialization.
/// </summary>
public class CompostingBinStateModelTests
{
    [Fact]
    public void DaysRemaining_DefaultsToZero()
    {
        var model = new CompostingBinStateModel();
        Assert.Equal(0, model.DaysRemaining);
    }

    [Fact]
    public void DaysRemaining_CanBeSetAndRetrieved()
    {
        var model = new CompostingBinStateModel { DaysRemaining = 2 };
        Assert.Equal(2, model.DaysRemaining);
    }

    [Fact]
    public void PopulateFromModData_WithProcessingData_SetsStateAndProperties()
    {
        var model = new CompostingBinStateModel();
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)24",
            [ModConstants.ModDataDaysRemainingKey] = "1"
        };

        model.PopulateFromModData(modData);

        Assert.Equal("(O)24", model.InputItemId);
        Assert.Equal(1, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Processing, model.State);
    }

    [Fact]
    public void PopulateFromModData_WithZeroDaysRemaining_SetsReadyState()
    {
        var model = new CompostingBinStateModel();
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)24",
            [ModConstants.ModDataDaysRemainingKey] = "0"
        };

        model.PopulateFromModData(modData);

        Assert.Equal("(O)24", model.InputItemId);
        Assert.Equal(0, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Ready, model.State);
    }

    [Fact]
    public void PopulateFromModData_WithNegativeDaysRemaining_ClampsToZeroAndReady()
    {
        var model = new CompostingBinStateModel();
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)24",
            [ModConstants.ModDataDaysRemainingKey] = "-3"
        };

        model.PopulateFromModData(modData);

        Assert.Equal("(O)24", model.InputItemId);
        Assert.Equal(0, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Ready, model.State);
    }

    [Fact]
    public void PopulateFromModData_WithMissingDaysRemaining_DefaultsToMaturationDays()
    {
        var model = new CompostingBinStateModel();
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)24"
        };

        model.PopulateFromModData(modData);

        Assert.Equal("(O)24", model.InputItemId);
        Assert.Equal(ModConstants.MaturationDays, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Processing, model.State);
    }

    [Fact]
    public void PopulateFromModData_WithInvalidDaysRemaining_DefaultsToMaturationDays()
    {
        var model = new CompostingBinStateModel();
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)24",
            [ModConstants.ModDataDaysRemainingKey] = "not-a-number"
        };

        model.PopulateFromModData(modData);

        Assert.Equal("(O)24", model.InputItemId);
        Assert.Equal(ModConstants.MaturationDays, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Processing, model.State);
    }

    [Fact]
    public void PopulateFromModData_WithEmptyInput_SetsEmptyState()
    {
        var model = new CompostingBinStateModel
        {
            InputItemId = "(O)24",
            DaysRemaining = 1,
            State = CompostingBinState.Processing
        };
        var modData = new Dictionary<string, string>();

        model.PopulateFromModData(modData);

        Assert.Null(model.InputItemId);
        Assert.Equal(0, model.DaysRemaining);
        Assert.Equal(CompostingBinState.Empty, model.State);
    }

    [Fact]
    public void PopulateFromModData_ThrowsArgumentNullException_WhenModDataIsNull()
    {
        var model = new CompostingBinStateModel();
        Assert.Throws<ArgumentNullException>(() => model.PopulateFromModData(null!));
    }

    [Fact]
    public void WriteToModData_WithActiveProgress_WritesKeys()
    {
        var model = new CompostingBinStateModel
        {
            InputItemId = "(O)16",
            DaysRemaining = 2,
            State = CompostingBinState.Processing
        };
        var modData = new Dictionary<string, string>();

        model.WriteToModData(modData);

        Assert.Equal("(O)16", modData[ModConstants.ModDataInputIdKey]);
        Assert.Equal("2", modData[ModConstants.ModDataDaysRemainingKey]);
    }

    [Fact]
    public void WriteToModData_WithEmptyBin_RemovesProgressKeys()
    {
        var model = new CompostingBinStateModel
        {
            InputItemId = null,
            DaysRemaining = 0,
            State = CompostingBinState.Empty
        };
        var modData = new Dictionary<string, string>
        {
            [ModConstants.ModDataInputIdKey] = "(O)16",
            [ModConstants.ModDataDaysRemainingKey] = "1"
        };

        model.WriteToModData(modData);

        Assert.False(modData.ContainsKey(ModConstants.ModDataInputIdKey));
        Assert.False(modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
    }

    [Fact]
    public void WriteToModData_ThrowsArgumentNullException_WhenModDataIsNull()
    {
        var model = new CompostingBinStateModel();
        Assert.Throws<ArgumentNullException>(() => model.WriteToModData(null!));
    }

    [Fact]
    public void RoundTrip_ProgressSerialization_PreservesState()
    {
        var original = new CompostingBinStateModel
        {
            InputItemId = "(O)88",
            DaysRemaining = 1,
            State = CompostingBinState.Processing
        };
        var modData = new Dictionary<string, string>();

        original.WriteToModData(modData);

        var restored = new CompostingBinStateModel();
        restored.PopulateFromModData(modData);

        Assert.Equal(original.InputItemId, restored.InputItemId);
        Assert.Equal(original.DaysRemaining, restored.DaysRemaining);
        Assert.Equal(original.State, restored.State);
    }
}
