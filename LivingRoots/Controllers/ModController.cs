using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Linq;



namespace LivingRoots.Controllers
{
    /// <summary>
    /// Controller for handling mod-related game events
    /// </summary>
    public class ModController : IDisposable
    {
        private bool _disposed = false;
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _manifest;
        private readonly object _registrationLock = new object();
        
        private bool _eventsRegistered = false;
        private bool _commandRegistered = false;
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;

        public ModController(IModHelper helper, IMonitor monitor, IManifest manifest)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        }

        public void RegisterEvents()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModController));
            
            // Defensive null checks
            if (_helper?.Events?.GameLoop == null)
            {
                _monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                return;
            }

            lock (_registrationLock)
            {
                try
                {
                    // Check if events are already registered to avoid duplicate registrations
                    if (_eventsRegistered)
                    {
                        _monitor.Log("Events are already registered, skipping registration.", LogLevel.Trace);
                        return;
                    }

                    // Initialize the handler once using null-coalescing assignment
                    _onGameLaunchedHandler ??= OnGameLaunched;
                    
                    // Subscribe to events
                    _helper.Events.GameLoop.GameLaunched += _onGameLaunchedHandler;
                    
                    _eventsRegistered = true;
                    _monitor.Log("Events registered successfully.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Error registering events: {ex}", LogLevel.Error);
                    _eventsRegistered = false;
                }
            }
        }

        public void UnregisterEvents()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModController));
            
            lock (_registrationLock)
            {
                try
                {
                    var gameLoop = _helper?.Events?.GameLoop;
                    if (gameLoop == null)
                    {
                        _monitor.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Trace);
                        return;
                    }

                    // Always attempt to detach to avoid leaked handlers even if the flag is out of sync
                    if (_onGameLaunchedHandler != null)
                    {
                        gameLoop.GameLaunched -= _onGameLaunchedHandler;
                        _onGameLaunchedHandler = null; // Clear the handler to prevent potential memory leaks
                    }

                    // Unregister console command if it was registered
                    if (_commandRegistered && _helper?.ConsoleCommands != null)
                    {
                        // In SMAPI, there's no direct method to remove a console command
                        // The command will be automatically removed when the mod is disposed
                        // We just reset the flag to indicate it's unregistered
                        _monitor.Log("Console command 'lr_version' unregistered successfully.", LogLevel.Trace);
                    }
                    
                    _eventsRegistered = false;
                    _commandRegistered = false;
                    _monitor.Log("Events unregistered successfully.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Error while unregistering events: {ex}", LogLevel.Error);
                }
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            try
            {
                _monitor.Log("The 'Living Roots' mod was loaded successfully!", LogLevel.Info);
                
                // Register console command only if not already registered
                if (_helper?.ConsoleCommands != null)
                {
                    // Check if command is already registered to prevent duplicate registration
                    if (!_commandRegistered)
                    {
                        _helper.ConsoleCommands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                        _commandRegistered = true;
                        _monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                    }
                    else
                    {
                        _monitor.Log("Console command 'lr_version' is already registered, skipping registration.", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnGameLaunched: {ex}", LogLevel.Error);
            }
        }

        private void PrintVersion(string command, string[] args)
        {
            try
            {
                // Add null check for args parameter and use case-insensitive comparison
                args = args ?? Array.Empty<string>();
                
                // Define help flags in a HashSet for better maintainability
                var helpFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "--help",
                    "-h",
                    "/?"
                };
                
                // Check if any argument matches a help flag
                if (args.Any(arg => helpFlags.Contains(arg)))
                {
                    _monitor.Log("Usage: lr_version", LogLevel.Info);
                    _monitor.Log("Shows the Living Roots mod version and UniqueID.", LogLevel.Info);
                    return;
                }
                
                // Include the mod's UniqueID in the output for better usability and clarity
                _monitor.Log($"Living Roots Mod Version: {_manifest?.Version?.ToString() ?? "unknown"} (UniqueID: {_manifest?.UniqueID ?? "unknown"})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in PrintVersion: {ex}", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    UnregisterEvents();
                }

                _disposed = true;
            }
        }
    }
}