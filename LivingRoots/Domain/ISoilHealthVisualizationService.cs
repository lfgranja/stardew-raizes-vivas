namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for soil health visualization operations.
    /// Provides methods to manage visualization lifecycle and event registration.
    /// </summary>
    public interface ISoilHealthVisualizationService
    {
        /// <summary>
        /// Registers SMAPI events for visualization functionality.
        /// </summary>
        void RegisterEvents();

        /// <summary>
        /// Unregisters SMAPI events for visualization functionality.
        /// </summary>
        void UnregisterEvents();

        /// <summary>
        /// Enables visualization features.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables visualization features.
        /// </summary>
        void Disable();

        /// <summary>
        /// Gets whether visualization is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the visualization configuration.
        /// </summary>
        IVisualizationConfig Config { get; }
    }
}
