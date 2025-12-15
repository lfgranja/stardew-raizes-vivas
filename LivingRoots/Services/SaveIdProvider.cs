using System;
using System.Globalization;
using StardewModdingAPI;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Provides the save ID for data persistence operations by accessing Stardew Valley's Game1 class.
    /// This implementation uses SMAPI's Constants to access the save folder name.
    /// </summary>
    public class SaveIdProvider : ISaveIdProvider
    {
        private readonly IMonitor? _monitor;

        public SaveIdProvider(IMonitor? monitor = null)
        {
            _monitor = monitor;
        }

        public string? GetSaveId()
        {
            try
            {
                // Use SMAPI's Constants to get the save folder name directly
                // This is more reliable than using reflection
                string? saveId = Constants.SaveFolderName;
                
                // Validate that the save ID is not null, empty, or whitespace before returning
                if (IsValidSaveId(saveId))
                    return saveId;
                
                // If save folder name is not available (e.g., during early initialization),
                // return null which will be handled by the calling code
                return null;
            }
            catch (Exception ex)
            {
                // Log minimal information about any errors at Trace level for debugging
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