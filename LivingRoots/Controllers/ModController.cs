using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Linq;
using LivingRoots.Services;

namespace LivingRoots.Controllers
{
    /// <summary>
    /// Controller for handling mod-related game events
    /// </summary>
    public class ModController : IDisposable
    {
        private volatile bool _disposed = false;
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _manifest;
        private readonly IModDataService _modDataService;
        private readonly object _registrationLock = new object();
        
        private volatile bool _eventsRegistered = false;
        
        /// <summary>
        /// Tracks whether the 'lr_version' console command has been registered to prevent duplicate registrations.
        /// Note: SMAPI does not provide a direct method to remove console commands, so once registered,
        /// the command remains active until the mod is disposed. This flag ensures the command is only
        /// registered once even if the GameLaunched event fires multiple times or during mod reloads.
        /// </summary>
        private volatile bool _commandRegistered = false;
        
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;

        public ModController(IModHelper helper, IMonitor monitor, IManifest manifest, IModDataService modDataService)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
        }

        public void RegisterEvents()
        {
            lock (_registrationLock)
            {
                if (_disposed)
                {
                    _monitor.Log("Attempted to register events after disposal. Operation skipped.", LogLevel.Trace);
                    return;
                }
                
                try
                {
                    // Move null check inside the lock to prevent race condition
                    var gameLoop = _helper?.Events?.GameLoop;
                    if (gameLoop == null)
                    {
                        _monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                        // Reset flags to prevent inconsistent state even when gameLoop is null
                        _eventsRegistered = false;
                        _commandRegistered = false;
                        return;
                    }

                    // Check if events are already registered to avoid duplicate registrations
                    if (_eventsRegistered)
                    {
                        _monitor.Log("Events are already registered, skipping registration.", LogLevel.Trace);
                        return;
                    }

                    // Initialize the handler once using null-coalescing assignment
                    _onGameLaunchedHandler ??= OnGameLaunched;
                    
                    // Subscribe to events using the local variable to avoid potential race condition
                    gameLoop.GameLaunched += _onGameLaunchedHandler;
                    
                    _eventsRegistered = true;
                    _monitor.Log("Events registered successfully.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    // Log concise error without stack trace/type name; clear handler to avoid stale reference
                    _monitor.Log($"Error registering events: {ex.Message}", LogLevel.Error);
                    _onGameLaunchedHandler = null;
                    _eventsRegistered = false;
                }
            }
        }

        public void UnregisterEvents()
        {
            lock (_registrationLock)
            {
                if (_disposed)
                {
                    _monitor.Log("Attempted to unregister events after disposal. Operation skipped.", LogLevel.Trace);
                    return;
                }
                
                UnregisterEventsInternal();
            }
        }

        /// <summary>
        /// Internal method to unregister events without checking the disposed flag.
        /// This is used by the Dispose method to ensure cleanup happens during disposal.
        /// </summary>
        private void UnregisterEventsInternal()
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
                    _monitor.Log("Controller state for command 'lr_version' has been reset. The command will be removed on mod disposal.", LogLevel.Trace);
                }
                
                _eventsRegistered = false;
                _commandRegistered = false;
                _monitor.Log("Events unregistered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error while unregistering events: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Handles the GameLaunched event to register the 'lr_version' console command.
        /// This method ensures the command is only registered once using the <see cref="_commandRegistered"/> flag
        /// to prevent double-registration across multiple GameLaunched events or mod reloads.
        /// Note: SMAPI does not provide a method to remove console commands directly, so the command
        /// lifecycle is tied to the mod's lifecycle (registered on mod load, removed on mod disposal).
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            try
            {
                _monitor.Log("The 'Living Roots' mod was loaded successfully!", LogLevel.Info);
                
                // Remove lock usage and use volatile boolean check instead
                // Since we're not using Interlocked.CompareExchange with bool, we'll use a simple volatile check
                if (!_commandRegistered)
                {
                    // Register console command only if not already registered
                    if (_helper?.ConsoleCommands != null)
                    {
                        _helper.ConsoleCommands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                        _commandRegistered = true;
                        _monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                    }
                }
                else
                {
                    _monitor.Log("Console command 'lr_version' is already registered, skipping registration.", LogLevel.Trace);
                }
                
                // Unsubscribe from the GameLaunched event to ensure this handler runs only once
                // This prevents multiple invocations during mod reloads, making the "run-once" behavior more robust
                if (_helper?.Events?.GameLoop != null && _onGameLaunchedHandler != null)
                {
                    _helper.Events.GameLoop.GameLaunched -= _onGameLaunchedHandler;
                    _onGameLaunchedHandler = null; // Clear the handler to prevent potential memory leaks
                    _monitor.Log("GameLaunched event handler unsubscribed after first execution.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnGameLaunched: {ex.Message}", LogLevel.Error);
            }
        }

        private void PrintVersion(string command, string[] args)
        {
            try
            {
                // Add null check for args parameter and use case-insensitive comparison
                args = args ?? Array.Empty<string>();
                
                // Filter out whitespace-only arguments to normalize the input
                var normalizedArgs = args.Where(arg => !string.IsNullOrWhiteSpace(arg)).ToArray();
                
                // Define help flags in a HashSet for better maintainability
                var helpFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "--help",
                    "-h",
                    "/?"
                };
                
                // Check if any argument matches a help flag
                if (normalizedArgs.Any(arg => helpFlags.Contains(arg)))
                {
                    _monitor.Log("Usage: lr_version", LogLevel.Info);
                    _monitor.Log("Shows the Living Roots mod version and UniqueID.", LogLevel.Info);
                    return;
                }
                
                // Include the mod's UniqueID in the output for better usability and clarity
                // Explicitly format the version string using MajorVersion, MinorVersion, and PatchVersion properties for consistent output
                var version = _manifest?.Version;
                string versionString = version?.ToString() ?? "unknown";
                    
                _monitor.Log($"Living Roots Mod Version: {versionString} (UniqueID: {_manifest?.UniqueID ?? "unknown"})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in PrintVersion: {ex.Message}", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            bool needsUnregistration = false;
            
            // Use a lock to ensure thread-safe disposal.
            lock (_registrationLock)
            {
                if (_disposed)
                {
                    return;
                }
                
                // Check if unregistration is needed before releasing the lock
                needsUnregistration = _eventsRegistered || _commandRegistered;
                _disposed = true;
            }

            // Unregister events outside the lock to prevent potential deadlock
            if (needsUnregistration)
            {
                UnregisterEventsInternal();
            }

            GC.SuppressFinalize(this);
        }
    }
}
