using System;
using LivingRoots.Domain;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    /// <summary>
    /// Provides the save ID for data persistence operations by accessing Stardew Valley's Game1 class.
    /// This implementation uses SMAPI's Constants to access the save folder name.
    /// </summary>
    public class SaveIdProvider(IMonitor? monitor = null) : ISaveIdProvider
    {
        private readonly IMonitor? _monitor = monitor;

        public string? GetSaveId()
        {
            try
            {
                var saveId = Constants.SaveFolderName;

                // Combine validation checks to eliminate null reference issues
                if (string.IsNullOrWhiteSpace(saveId) || saveId.Length > ModConstants.MaxSaveIdLength)
                {
                    if (string.IsNullOrWhiteSpace(saveId))
                    {
                        // Save ID is null, empty, or whitespace - no specific log needed
                    }
                    else if (saveId.Length > ModConstants.MaxSaveIdLength)
                    {
                        _monitor?.Log($"GetSaveId: Save ID exceeded maximum length ({ModConstants.MaxSaveIdLength}); returning null.", LogLevel.Trace);
                    }
                    return null;
                }

                // At this point, saveId is guaranteed non-null and within length limit
                return saveId;
            }
            catch (Exception ex)
            {
                _monitor?.Log($"GetSaveId: Exception occurred: {ex.GetType().Name}", LogLevel.Trace);
                return null;
            }
        }

    }
}
