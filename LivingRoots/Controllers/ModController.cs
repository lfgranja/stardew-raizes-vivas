using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Threading;
using System.Linq;
using LivingRoots.Services;

namespace LivingRoots.Controllers
{
    /// <summary>
    /// Controller for handling mod-related game events
    /// </summary>
    public sealed class ModController : IDisposable
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _manifest;
        private readonly IModDataService _modDataService;
        
        private int _eventsRegistered = 0; // Use int (0 = false, 1 = true) for atomic operations
        
        /// <summary>
        /// Tracks whether the 'lr_version' console command has been registered to prevent duplicate registrations.
        /// Note: SMAPI does not provide a direct method to remove console commands, so once registered,
        /// the command remains active until the mod is disposed. This flag ensures the command is only
        /// registered once even if the GameLaunched event fires multiple times or during mod reloads.
        /// </summary>
        private int _commandRegistered = 0; // Use int (0 = false, 1 = true) for atomic operations
        
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;

        // Single integer field for atomic disposal check to prevent race conditions
        // Using 0 for not disposed, 1 for disposed
        private int _disposed = 0;

        public ModController(IModHelper helper, IMonitor monitor, IManifest manifest, IModDataService modDataService)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
        }

        public void RegisterEvents()
        {
            // Check if disposed using single integer flag
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
            {
                _monitor.Log("Attempted to register events after disposal. Operation skipped.", LogLevel.Trace);
                return;
            }
            
            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;
            
            // Try to set the event registration flag atomically
            if (Interlocked.CompareExchange(ref _eventsRegistered, 1, 0) == 1)
            {
                monitor.Log("Events are already registered, skipping registration.", LogLevel.Trace);
                return;
            }
            
            try
            {
                var gameLoop = helper?.Events?.GameLoop;
                if (gameLoop == null)
                {
                    monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                    // Reset flag since registration failed
                    Interlocked.Exchange(ref _eventsRegistered, 0);
                    return;
                }

                // Initialize the handler once
                _onGameLaunchedHandler ??= OnGameLaunched;
                
                // Subscribe to events
                gameLoop.GameLaunched += _onGameLaunchedHandler;
                
                monitor.Log("Events registered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error and reset the flag if registration failed
                monitor.Log($"Error registering events: {ex.Message}", LogLevel.Error);
                _onGameLaunchedHandler = null;
                Interlocked.Exchange(ref _eventsRegistered, 0);
            }
        }

        public void UnregisterEvents()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
            {
                _monitor.Log("Attempted to unregister events after disposal. Operation skipped.", LogLevel.Trace);
                return;
            }
            
            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;
            
            UnregisterEventsInternal(monitor, helper);
        }

        /// <summary>
        /// Internal method to unregister events without checking the disposed flag.
        /// This is used by the Dispose method to ensure cleanup happens during disposal.
        /// </summary>
        private void UnregisterEventsInternal(IMonitor? monitor = null, IModHelper? helper = null)
        {
            // Create snapshots of dependencies if not provided
            var localMonitor = monitor ?? _monitor;
            var localHelper = helper ?? _helper;
            
            try
            {
                var gameLoop = localHelper?.Events?.GameLoop;
                if (gameLoop == null)
                {
                    localMonitor.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Trace);
                    return;
                }

                // Use Interlocked.Exchange to safely get and clear the handler
                var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                
                // Always attempt to detach to avoid leaked handlers
                if (handler != null)
                {
                    gameLoop.GameLaunched -= handler;
                }

                // Unregister console command if it was registered
                if (Interlocked.CompareExchange(ref _commandRegistered, 0, 0) == 1 && localHelper?.ConsoleCommands != null)
                {
                    // In SMAPI, there's no direct method to remove a console command
                    // The command will be automatically removed when the mod is disposed
                    // We just reset the flag to indicate it's unregistered
                    localMonitor.Log("Controller state for command 'lr_version' has been reset. The command will be removed on mod disposal.", LogLevel.Trace);
                }
                
                Interlocked.Exchange(ref _eventsRegistered, 0);
                Interlocked.Exchange(ref _commandRegistered, 0);
                localMonitor.Log("Events unregistered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                localMonitor.Log($"Error while unregistering events: {ex.Message}", LogLevel.Error);
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
                // Check if disposed at the beginning
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                {
                    _monitor.Log("OnGameLaunched called after disposal. Operation skipped.", LogLevel.Trace);
                    return;
                }

                // Create snapshots of dependencies to avoid errors if disposed mid-execution
                var monitor = _monitor;
                var helper = _helper;
                
                monitor.Log("The 'Living Roots' mod was loaded successfully!", LogLevel.Info);
                
                // Use CompareExchange to make command registration atomic
                if (Interlocked.CompareExchange(ref _commandRegistered, 1, 0) == 0)
                {
                    // Check again after disposal check to ensure we're not disposed during execution
                    if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                        return;

                    var commands = helper?.ConsoleCommands;
                    if (commands != null)
                    {
                        commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                        monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                    }
                }
                
                // Check again before unsubscribing to ensure we're still not disposed
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                    return;

                // Use Interlocked.Exchange to safely get and clear the handler to avoid race condition
                var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                if (handler != null && helper?.Events?.GameLoop != null)
                {
                    helper.Events.GameLoop.GameLaunched -= handler;
                    monitor.Log("GameLaunched event handler unsubscribed after first execution.", LogLevel.Trace);
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
                // Check disposal flag at start of method to prevent execution after disposal
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
                {
                    return; // Skip execution if disposed
                }
                
                // Snapshot dependencies to local variables to avoid NullReferenceExceptions
                var monitor = _monitor;
                var manifest = _manifest;
                
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
                    monitor?.Log("Usage: lr_version", LogLevel.Info);
                    monitor?.Log("Shows the Living Roots mod version and UniqueID.", LogLevel.Info);
                    return;
                }
                
                // Include the mod's UniqueID in the output for better usability and clarity
                // Explicitly format the version string using MajorVersion, MinorVersion, and PatchVersion properties for consistent output
                var version = manifest?.Version;
                string versionString = version?.ToString() ?? "unknown";
                    
                monitor?.Log($"Living Roots Mod Version: {versionString} (UniqueID: {manifest?.UniqueID ?? "unknown"})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor?.Log($"Error in PrintVersion: {ex.Message}", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            // Ensure only one thread executes disposal logic
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            // Perform cleanup in a thread-safe manner
            PerformCleanup();
        }

        /// <summary>
        /// Performs cleanup operations in a thread-safe manner.
        /// This method is used by both the public Dispose method and internally
        /// to ensure consistent cleanup behavior.
        /// </summary>
        private void PerformCleanup()
        {
            try
            {
                // Snapshot dependencies to avoid accessing disposed objects
                var helper = _helper;
                var monitor = _monitor;

                var gameLoop = helper?.Events?.GameLoop;
                
                // Use Interlocked.Exchange to safely get and clear the handler
                var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                
                if (gameLoop != null && handler != null)
                {
                    try
                    {
                        gameLoop.GameLaunched -= handler;
                    }
                    catch (Exception ex)
                    {
                        monitor?.Log($"Error while unregistering GameLaunched: {ex.Message}", LogLevel.Error);
                    }
                }

                // Reset flags before returning so other threads won't attempt registration
                Interlocked.Exchange(ref _eventsRegistered, 0);
                Interlocked.Exchange(ref _commandRegistered, 0);
            }
            finally
            {
                // No re-registration after this point; avoid further cleanup that touches SMAPI
            }
        }
    }
}
