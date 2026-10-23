using System;
using System.Collections.Generic;

namespace LivingRoots.Domain.Models;

/// <summary>
/// Runtime state for a single composting bin machine.
/// Tracks tile position, operational state, input item, maturation, and idle time.
/// </summary>
public class CompostingBinStateModel
{
    /// <summary>X tile coordinate of the bin.</summary>
    public int TileX { get; set; }

    /// <summary>Y tile coordinate of the bin.</summary>
    public int TileY { get; set; }

    /// <summary>Current operational state of the bin.</summary>
    public CompostingBinState State { get; set; }

    /// <summary>Qualified item ID of the input waste item (null if empty).</summary>
    public string? InputItemId { get; set; }

    /// <summary>Game day when waste was added via ITimeProvider.TotalDays (null if empty).</summary>
    public int? InputTimestamp { get; set; }

    /// <summary>Current maturation level (1-5), determines output multiplier.</summary>
    public int MaturationLevel { get; set; } = 1;

    /// <summary>Consecutive days without activity, used for maturation reset.</summary>
    public int ConsecutiveIdleDays { get; set; } = 0;

    /// <summary>Days of continuous operation, used for maturation increment.</summary>
    public int ConsecutiveActiveDays { get; set; } = 0;

    /// <summary>
    /// Whether compost has been paid for this batch (prevents dual-payout with interception).
    /// </summary>
    public bool HasPaidThisBatch { get; set; } = false;

    /// <summary>
    /// Remaining days until maturation (0 when ready or empty).
    /// </summary>
    public int DaysRemaining { get; set; } = 0;

    /// <summary>
    /// Populates progress retention data from an item's modData dictionary.
    /// </summary>
    /// <param name="modData">The modData dictionary from the item.</param>
    /// <exception cref="ArgumentNullException">Thrown when modData is null.</exception>
    public void PopulateFromModData(IDictionary<string, string> modData)
    {
        if (modData == null)
        {
            throw new ArgumentNullException(nameof(modData));
        }

        if (modData.TryGetValue(ModConstants.ModDataInputIdKey, out var inputId) && !string.IsNullOrWhiteSpace(inputId))
        {
            InputItemId = inputId;
        }
        else
        {
            InputItemId = null;
        }

        if (modData.TryGetValue(ModConstants.ModDataDaysRemainingKey, out var daysRemainingStr) &&
            int.TryParse(daysRemainingStr, out var daysRemaining))
        {
            DaysRemaining = Math.Max(0, daysRemaining);
        }
        else
        {
            DaysRemaining = !string.IsNullOrWhiteSpace(InputItemId) ? ModConstants.MaturationDays : 0;
        }

        if (!string.IsNullOrWhiteSpace(InputItemId))
        {
            State = DaysRemaining <= 0 ? CompostingBinState.Ready : CompostingBinState.Processing;
        }
        else
        {
            State = CompostingBinState.Empty;
        }
    }

    /// <summary>
    /// Whether compost has been paid for this batch (prevents dual-payout with interception).
    /// Reads from modData to restore state when a carried bin is placed.
    /// </summary>
    public bool GetHasPaidThisBatch(IDictionary<string, string> modData)
    {
        if (modData.TryGetValue(ModConstants.ModDataDaysRemainingKey, out var daysStr) &&
            int.TryParse(daysStr, out var days) &&
            days <= 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Writes progress retention data to an item's modData dictionary.
    /// </summary>
    /// <param name="modData">The modData dictionary of the item to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when modData is null.</exception>
    public void WriteToModData(IDictionary<string, string> modData)
    {
        if (modData == null)
        {
            throw new ArgumentNullException(nameof(modData));
        }

        if (!string.IsNullOrWhiteSpace(InputItemId))
        {
            modData[ModConstants.ModDataInputIdKey] = InputItemId;
            modData[ModConstants.ModDataDaysRemainingKey] = DaysRemaining.ToString();
        }
        else
        {
            modData.Remove(ModConstants.ModDataInputIdKey);
            modData.Remove(ModConstants.ModDataDaysRemainingKey);
        }

        // Write whether compost has been paid for this batch (days <= 0 indicates paid)
        if (HasPaidThisBatch)
        {
            modData[ModConstants.ModDataDaysRemainingKey] = "0";
        }
    }
}
