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
        

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Create domain services - Composition Root
            var unicodeNormalizationService = new UnicodeNormalizationService();
            var fileNameSanitizationService = new FileNameSanitizationService(unicodeNormalizationService);
            var pathValidationService = new PathValidationService();
            var reservedNameHandler = new ReservedNameHandler(unicodeNormalizationService);
            var modLogic = new ModLogic(fileNameSanitizationService, pathValidationService);
            
            // Create adapters for backward compatibility
            var fileNameSanitizer = new FileNameSanitizer(fileNameSanitizationService);
            var pathTraversalValidator = new PathTraversalValidator(pathValidationService);
            var unicodeNormalizer = new UnicodeNormalizer(unicodeNormalizationService);
            var reservedNameHandlerAdapter = reservedNameHandler;
            
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
            _controller?.Dispose();
            base.Dispose(disposing);
        }
        
        
    }
}