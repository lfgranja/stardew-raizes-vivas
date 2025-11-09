using StardewModdingAPI;
using LivingRoots.Controllers;
using LivingRoots.Services;
using LivingRoots.Domain;

namespace LivingRoots
{
    /// <summary>
    /// Main entry point for the Living Roots mod
    /// Following the architecture pattern described in ARCHITECTURE.md
    /// Now serves as the Composition Root where all dependencies are configured and injected
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private ModController? _controller;
        private bool _disposed = false;

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
            var pathValidationService = new PathValidationService();
            var modLogic = new ModLogic(fileNameSanitizationService, pathValidationService);
            
            // Create application services
            var modDataService = new ModDataService(helper, this.Monitor, modLogic);
            
            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest, modDataService);
            
            // Register events through the controller
            _controller.RegisterEvents();
        }
        
        /*********
        ** Protected methods
        *********/
        
        /// <summary>Clean up resources when the mod is unloaded.</summary>
        /// <param name="disposing">Whether the instance is being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources only when disposing is true
                    _controller?.Dispose();
                    _controller = null;
                }

                _disposed = true;
            }
            
            base.Dispose(disposing);
        }
    }
}