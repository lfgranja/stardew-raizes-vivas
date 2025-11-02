using StardewModdingAPI;
using LivingRoots.Controllers;


namespace LivingRoots
{
    /// <summary>
    /// Main entry point for the Living Roots mod
    /// Following the architecture pattern described in ARCHITECTURE.md
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
            // Create controller with dependency injection
            _controller = new ModController(helper, this.Monitor, this.ModManifest);
            
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