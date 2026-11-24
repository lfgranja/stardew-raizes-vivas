using LivingRoots.Domain.Visualization;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Manages loading, saving, and accessing visualization configuration.
    /// Configuration is persisted per-save via IModDataService.
    /// </summary>
    public interface IVisualizationConfigurationService
    {
        /// <summary>
        /// Gets the current visualization configuration.
        /// </summary>
        /// <returns>Active configuration with defaults for any invalid fields</returns>
        VisualizationConfiguration GetConfiguration();

        /// <summary>
        /// Loads configuration from storage for the specified save.
        /// Called on SaveLoaded event.
        /// </summary>
        /// <param name="saveId">Unique save identifier</param>
        void LoadConfiguration(string saveId);

        /// <summary>
        /// Saves current configuration to storage.
        /// Called on Saving event.
        /// </summary>
        /// <param name="saveId">Unique save identifier</param>
        void SaveConfiguration(string saveId);

        /// <summary>
        /// Resets configuration to default values.
        /// Does not persist until SaveConfiguration is called.
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Updates configuration with validated values.
        /// Invalid fields are replaced with defaults per FR-006.
        /// </summary>
        /// <param name="configuration">New configuration to apply</param>
        void UpdateConfiguration(VisualizationConfiguration configuration);
    }
}
