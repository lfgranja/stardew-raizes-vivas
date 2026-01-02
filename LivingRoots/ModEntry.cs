using System.Threading;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using StardewModdingAPI;

namespace LivingRoots
{
    /// <summary>
    /// Main entry point for the Living Roots mod
    /// Following the architecture pattern described in ARCHITECTURE.md
    /// Now serves as a Composition Root where all dependencies are configured and injected
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private ModController? _controller;
        private bool _disposed = false;  // Thread-safe visibility of disposed state - all access is protected by _disposeLock
        private readonly object _disposeLock = new();

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Create domain services - Composition Root
            var unicodeNormalizationService = new UnicodeNormalizationService();
            var reservedNameHandler = new ReservedNameHandler(unicodeNormalizationService);
            var fileNameSanitizationService = new FileNameSanitizationService(unicodeNormalizationService, reservedNameHandler);
            var pathValidationService = new PathValidationService(unicodeNormalizationService);
            var modLogic = new ModLogic(fileNameSanitizationService, pathValidationService);

            // Create application services
            var modDataService = new ModDataService(helper, this.Monitor, modLogic);

            // Create the soil health service with the required dependencies
            var soilHealthService = new SoilHealthService(modDataService, this.Monitor, fileNameSanitizationService);

            // Create the save ID provider with monitor for logging
            var saveIdProvider = new SaveIdProvider(this.Monitor);

            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService, soilHealthService, saveIdProvider);

            // Register events through the Controller
            _controller.RegisterEvents();
        }

        /*********
        ** Protected methods
        ********/

        /// <summary>Clean up resources when the mod is unloaded.</summary>
        /// <param name="disposing">Whether to instance is being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            // Use a lock to ensure thread-safe disposal
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    _controller?.Dispose();
                    _controller = null;
                }

                _disposed = true;
            }

            // Call base.Dispose after releasing the lock to prevent potential deadlocks
            base.Dispose(disposing);
        }
    }
}
