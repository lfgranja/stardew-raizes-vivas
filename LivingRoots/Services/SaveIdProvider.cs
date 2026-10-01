using System;
using LivingRoots.Domain;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    /// <summary>
    /// Provides the save ID for data persistence operations.
    /// SMAPI exposes the save folder through <c>Constants.SaveFolderName</c>, which is a
    /// computed property backed by live game state and cannot be assigned. The lookup is
    /// therefore injected so tests can supply a value without a running game.
    /// </summary>
    public class SaveIdProvider : ISaveIdProvider
    {
        private readonly Func<string?> _saveFolderNameAccessor;
        private readonly IMonitor? _monitor;

        /// <summary>
        /// Initializes the provider using SMAPI's save folder name.
        /// </summary>
        /// <param name="monitor">Optional monitor for diagnostic output.</param>
        public SaveIdProvider(IMonitor? monitor = null)
            : this(() => Constants.SaveFolderName, monitor)
        {
        }

        /// <summary>
        /// Initializes the provider with an explicit save folder name accessor.
        /// </summary>
        /// <param name="saveFolderNameAccessor">Accessor returning the current save folder name.</param>
        /// <param name="monitor">Optional monitor for diagnostic output.</param>
        public SaveIdProvider(Func<string?> saveFolderNameAccessor, IMonitor? monitor = null)
        {
            _saveFolderNameAccessor = saveFolderNameAccessor ?? throw new ArgumentNullException(nameof(saveFolderNameAccessor));
            _monitor = monitor;
        }

        /// <inheritdoc />
        public string? GetSaveId()
        {
            try
            {
                var saveId = _saveFolderNameAccessor();

                if (string.IsNullOrWhiteSpace(saveId))
                {
                    return null;
                }

                if (saveId.Length > ModConstants.MaxSaveIdLength)
                {
                    _monitor?.Log($"GetSaveId: Save ID exceeded maximum length ({ModConstants.MaxSaveIdLength}); returning null.", LogLevel.Trace);
                    return null;
                }

                return saveId;
            }
            catch (Exception ex)
            {
                // Never surface the raw exception message; type name only.
                _monitor?.Log($"GetSaveId: Exception occurred: {ex.GetType().Name}", LogLevel.Trace);
                return null;
            }
        }
    }
}
