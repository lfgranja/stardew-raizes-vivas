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

                if (!IsValidSaveId(saveId))
                    return null;

                if (saveId!.Length > ModConstants.MaxSaveIdLength)
                {
                    _monitor?.Log($"GetSaveId: Save ID exceeded maximum length ({ModConstants.MaxSaveIdLength}); returning null.", LogLevel.Trace);
                    return null;
                }

                return saveId;
            }
            catch (Exception ex)
            {
                _monitor?.Log($"GetSaveId: Exception occurred: {ex.GetType().Name}", LogLevel.Trace);
                return null;
            }
        }

        /// <summary>
        /// Validates that the save ID is not null, empty, or whitespace
        /// </summary>
        /// <param name="saveId">The save ID to validate</param>
        /// <returns>True if the save ID is valid, false otherwise</returns>
        private static bool IsValidSaveId(string? saveId)
        {
            return !string.IsNullOrWhiteSpace(saveId);
        }
    }
}
