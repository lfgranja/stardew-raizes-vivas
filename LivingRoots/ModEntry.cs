using StardewModdingAPI;
using LivingRoots.Controllers;
using LivingRoots.Services;
using LivingRoots.Domain;
using System.Threading;

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
        private readonly object _disposeLock = new object();

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
            
            // NOVO: Serviço de Saúde do Solo
            var soilHealthService = new SoilHealthService(modDataService, this.Monitor);
            
            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService, soilHealthService);
            
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
            bool callBaseDispose = false;
            
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
                callBaseDispose = true;
            }
            
            // Only call base.Dispose after releasing the lock to prevent potential deadlocks
            if (callBaseDispose)
            {
                base.Dispose(disposing);
            }
        }
    }
}