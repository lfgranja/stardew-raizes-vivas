# Contract: IVisualizationConfigurationService

**Domain**: LivingRoots.Domain
**Implementation**: LivingRoots.Services.Visualization.VisualizationConfigurationService

## Interface Definition

```csharp
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
```

## Preconditions
- saveId is non-null and validated by FileNameSanitizationService
- Configuration object is non-null when passed to UpdateConfiguration

## Postconditions
- GetConfiguration() always returns a valid configuration (never null)
- Invalid fields in loaded configuration replaced with defaults
- Missing configuration file results in complete defaults
- Configuration changes persisted on SaveConfiguration()

## Error Handling
- Invalid saveId logs warning and skips operation
- JSON deserialization errors log error and return defaults
- Invalid field values logged as warning and replaced with defaults

## Thread Safety
- Configuration access must be thread-safe (multiple threads may read during render)
- Save/Load operations are async-compatible (no blocking calls)
