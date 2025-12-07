using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using LivingRoots.Services;
using LivingRoots.Domain; // Add

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
        private readonly ISoilHealthService _soilHealthService; // NEW DEPENDENCY

        private static readonly HashSet<string> HelpFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "--help",
            "-h",
            "/?"
        };

        // Single atomic state for thread-safe management of controller state
        // Using bit flags: 0x01 = events registered, 0x02 = command registered, 0x04 = disposed, 0x08 = disposed message logged
        private int _state = 0;

        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;
        private EventHandler<SaveLoadedEventArgs>? _onSaveLoadedHandler; // NEW
        private EventHandler<SavingEventArgs>? _onSavingHandler; // CORRIGIDO: Era _onSavedHandler // NEW

        // State bit flags
        private const int EventsRegisteredFlag = 0x01;
        private const int CommandRegisteredFlag = 0x02;
        private const int DisposedFlag = 0x04;
        private const int DisposedMessageLoggedFlag = 0x08; // NEW: Flag to track if disposal message was already logged

        public ModController(IModHelper helper, IMonitor monitor, IManifest manifest, IModDataService modDataService, ISoilHealthService soilHealthService) // Update constructor
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
        }

        public void RegisterEvents()
        {
            // Early exit if already disposed - single atomic check using volatile read
            if (IsDisposed())
            {
                _monitor.Log("Attempted to register events after disposal. Operation skipped.", LogLevel.Trace);
                return;
            }

            // Use TrySetStateFlag to ensure events are only registered once
            if (!TrySetStateFlag(EventsRegisteredFlag))
            {
                _monitor.Log("Events are already registered, skipping registration.", LogLevel.Trace);
                return;
            }

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;

            // Capture GameLoop once for consistent subscribe/rollback
            var gameLoop = helper?.Events?.GameLoop;
            if (gameLoop == null)
            {
                monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                // Reset flag since registration failed - ensure disposed flag is preserved
                // Use Interlocked.And for simple flag clearing
                Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                return;
            }

            // Track which events were successfully added for proper rollback
            bool gameLaunchedAdded = false;
            bool saveLoadedAdded = false;
            bool savingAdded = false; // CORRIGIDO: Era savedAdded // NEW

            try
            {
                // Initialize the handlers once
                _onGameLaunchedHandler ??= OnGameLaunched;
                _onSaveLoadedHandler ??= OnSaveLoaded; // NEW
                _onSavingHandler ??= OnSaving; // CORRIGIDO: Era _onSavedHandler // NEW

                // Double-check disposed state before subscribing to prevent race condition
                if (IsDisposed())
                {
                    monitor.Log("Controller disposed during registration. Skipping event subscription.", LogLevel.Trace);
                    Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                    return;
                }

                // Subscribe to events
                gameLoop.GameLaunched += _onGameLaunchedHandler;
                gameLaunchedAdded = true;
                
                // NEW EVENTS - Loading and saving soil health data
                gameLoop.SaveLoaded += _onSaveLoadedHandler; // NEW
                saveLoadedAdded = true;
                gameLoop.Saving += _onSavingHandler; // CORRIGIDO: Era gameLoop.Saved // NEW
                savingAdded = true; // CORRIGIDO: Era savedAdded // NEW

                monitor.Log("Events registered successfully.", LogLevel.Trace);
            }
            catch (Exception)
            {
                // Log error and reset the flag if registration failed - ensure disposed flag is preserved
                monitor.Log("Error occurred while registering game events.", LogLevel.Error);
                
                // Attempt to rollback any partial subscriptions
                try
                {
                    if (gameLoop != null) // Guard against null gameLoop in rollback
                    {
                        if (gameLaunchedAdded && _onGameLaunchedHandler != null)
                            gameLoop.GameLaunched -= _onGameLaunchedHandler;
                        if (saveLoadedAdded && _onSaveLoadedHandler != null)
                            gameLoop.SaveLoaded -= _onSaveLoadedHandler; // NEW
                        if (savingAdded && _onSavingHandler != null) // CORRIGIDO: Era _onSavedHandler // NEW
                            gameLoop.Saving -= _onSavingHandler; // CORRIGIDO: Era gameLoop.Saved // NEW
                    }
                }
                catch (Exception rollbackEx) 
                { 
                    monitor.Log($"Error during event subscription rollback: {rollbackEx.Message}", LogLevel.Trace); 
                    /* avoid masking original failure */ 
                }

                _onGameLaunchedHandler = null;
                _onSaveLoadedHandler = null; // NEW
                _onSavingHandler = null; // CORRIGIDO: Era _onSavedHandler // NEW

                Interlocked.And(ref _state, ~(EventsRegisteredFlag));
            }
        }

        public void UnregisterEvents()
        {
            // Early exit if already disposed - single atomic check using volatile read
            if (IsDisposed())
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

                // Use Interlocked.Exchange to safely get and clear the handlers
                var gameLaunchedHandler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                var saveLoadedHandler = Interlocked.Exchange(ref _onSaveLoadedHandler, null); // NEW
                var savingHandler = Interlocked.Exchange(ref _onSavingHandler, null); // CORRIGIDO: Era _onSavedHandler // NEW

                // Always attempt to detach to avoid leaked handlers - each with its own exception handling
                if (gameLaunchedHandler != null)
                {
                    try { gameLoop.GameLaunched -= gameLaunchedHandler; }
                    catch (Exception) { localMonitor.Log("Error occurred while unregistering GameLaunched event.", LogLevel.Error); }
                }
                
                // Detach new handlers as well - each with its own exception handling
                if (saveLoadedHandler != null) // NEW
                {
                    try { gameLoop.SaveLoaded -= saveLoadedHandler; }
                    catch (Exception) { localMonitor.Log("Error occurred while unregistering SaveLoaded event.", LogLevel.Error); }
                }
                
                if (savingHandler != null) // CORRIGIDO: Era _onSavedHandler // NEW
                {
                    try { gameLoop.Saving -= savingHandler; } // CORRIGIDO: Era gameLoop.Saved // NEW
                    catch (Exception) { localMonitor.Log("Error occurred while unregistering Saving event.", LogLevel.Error); } // CORRIGIDO: Era Saving event // NEW
                }

                // Unregister console command if it was registered
                if (IsCommandRegistered() && localHelper?.ConsoleCommands != null)
                {
                    // In SMAPI, there's no direct method to remove a console command
                    // The command will be automatically removed when the mod is disposed
                    // We just reset the flag to indicate it's unregistered
                    localMonitor.Log("Controller state for command 'lr_version' has been reset. The command will be removed on mod disposal.", LogLevel.Trace);
                }

                // Reset state flags atomically - ensure disposed flag is preserved
                Interlocked.And(ref _state, ~(EventsRegisteredFlag | CommandRegisteredFlag));

                localMonitor.Log("Events unregistered successfully.", LogLevel.Trace);
            }
            catch (Exception)
            {
                localMonitor.Log("Error occurred while unregistering game events.", LogLevel.Error);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) // NEW
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;
                
            try
            {
                // Load data using the save folder name as unique ID
                var saveId = Constants.SaveFolderName;
                if (string.IsNullOrWhiteSpace(saveId))
                {
                    _monitor.Log("Cannot load soil health data: SaveFolderName is unavailable.", LogLevel.Warn);
                    return;
                }
                
                _soilHealthService.LoadData(saveId);
                _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace); // CORRECTED: Removed save ID from success log
            }
            catch (Exception)
            {
                var saveId = Constants.SaveFolderName ?? "unknown";
                _monitor.Log($"Error occurred while loading soil health data for save '{saveId}'.", LogLevel.Error);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e) // NEW - CORRIGIDO: Era OnSaved
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;
            
            try
            {
                // Save data before the game saves/exits (using the saving event)
                var saveId = Constants.SaveFolderName;
                if (string.IsNullOrWhiteSpace(saveId))
                {
                    _monitor.Log("Cannot save soil health data: SaveFolderName is unavailable.", LogLevel.Warn);
                    return;
                }
                
                _soilHealthService.SaveData(saveId);
                _monitor.Log("Soil health data saved successfully.", LogLevel.Trace); // CORRECTED: Removed save ID from success log
            }
            catch (Exception)
            {
                var saveId = Constants.SaveFolderName ?? "unknown";
                _monitor.Log($"Error occurred while saving soil health data for save '{saveId}'.", LogLevel.Error);
            }
        }

        /// <summary>
        /// Handles the GameLaunched event to register the 'lr_version' console command.
        /// This method ensures the command is only registered once using the state flags
        /// to prevent double-registration across multiple GameLaunched events or mod reloads.
        /// Note: SMAPI does not provide a method to remove console commands directly, so the command
        /// lifecycle is tied to the mod's lifecycle (registered on mod load, removed on mod disposal).
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Early exit if disposed - single atomic check at the beginning using volatile read
            if (IsDisposed())
            {
                _monitor.Log("OnGameLaunched called after disposal. Operation skipped.", LogLevel.Trace);
                return;
            }

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;

            try
            {
                monitor.Log("The 'Living Roots' mod was loaded successfully!", LogLevel.Info);

                // Use the new atomic registration method
                bool commandRegistered = TryRegisterCommandAtomically(helper, monitor);

                if (!commandRegistered)
                {
                    monitor.Log("Command registration was not performed (already registered or unavailable).", LogLevel.Trace);
                }

                // Use Interlocked.Exchange to safely get and clear the handler to avoid race condition
                var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                if (handler != null && helper?.Events?.GameLoop != null)
                {
                    helper.Events.GameLoop.GameLaunched -= handler;
                    monitor.Log("GameLaunched event handler unsubscribed after first execution.", LogLevel.Trace);
                }
            }
            catch (Exception)
            {
                _monitor.Log("Error occurred in game launched event handler.", LogLevel.Error);
            }
        }

        /// <summary>
        /// Attempts to register the console command atomically, ensuring thread safety and state consistency.
        /// This method performs the complete registration operation atomically:
        /// 1. Checks if command is already registered
        /// 2. Verifies ConsoleCommands is available
        /// 3. Registers the command
        /// 4. Sets the flag - only if all previous steps succeeded
        /// </summary>
        /// <param name="helper">The mod helper instance</param>
        /// <param name="monitor">The monitor instance for logging</param>
        /// <returns>True if the command was successfully registered, false otherwise</returns>
        private bool TryRegisterCommandAtomically(IModHelper? helper, IMonitor? monitor)
        {
            // Use a loop to handle potential race conditions during the multi-step process
            int currentState, newState;
            bool registrationAttempted = false;
            bool commandAvailable = false;

            do
            {
                currentState = Volatile.Read(ref _state);

                // If already disposed, exit immediately
                if ((currentState & DisposedFlag) != 0)
                    return false;

                // If command is already registered, return false (no work needed)
                if ((currentState & CommandRegisteredFlag) != 0)
                    return false;

                // Check if ConsoleCommands is available before proceeding
                var commands = helper?.ConsoleCommands;
                if (commands == null)
                {
                    monitor?.Log("Command registration deferred: ConsoleCommands is not yet available.", LogLevel.Trace);
                    return false;
                }

                // Set the flag and proceed with registration
                newState = currentState | CommandRegisteredFlag;
                
                // Attempt to set the state atomically
                registrationAttempted = Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
                
                if (registrationAttempted)
                {
                    try
                    {
                        // Actually register the command
                        commands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                        monitor?.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);
                        commandAvailable = true;
                    }
                    catch (Exception)
                    {
                        // If registration failed after setting the flag, reset the flag to maintain consistency
                        monitor?.Log("Error occurred while registering console command 'lr_version'.", LogLevel.Error);
                        
                        // Reset the command registered flag to maintain state consistency
                        Interlocked.And(ref _state, ~CommandRegisteredFlag);
                        return false;
                    }
                }
            }
            while (!registrationAttempted);

            return commandAvailable;
        }

        private void PrintVersion(string command, string[] args)
        {
            // Early exit if disposed - single atomic check at the beginning using volatile read
            if (IsDisposed())
            {
                return; // Skip execution if disposed
            }

            // Snapshot dependencies to local variables to avoid NullReferenceExceptions
            var monitor = _monitor;
            var manifest = _manifest;

            try
            {
                // Add null check for args parameter and use case-insensitive comparison
                args = args ?? Array.Empty<string>();

                // Filter out whitespace-only arguments to normalize the input
                var normalizedArgs = args.Where(arg => !string.IsNullOrWhiteSpace(arg)).ToArray();

                // Check if any argument matches a help flag
                if (normalizedArgs.Any(arg => HelpFlags.Contains(arg)))
                {
                    monitor?.Log("Usage: lr_version", LogLevel.Info);
                    monitor?.Log("Shows the Living Roots mod version and UniqueID.", LogLevel.Info);
                    return;
                }

                // Use the standard version.ToString() method which provides consistent output
                var version = manifest?.Version;
                string versionString = version?.ToString() ?? "unknown";

                monitor?.Log($"Living Roots Mod Version: {versionString} (UniqueID: {manifest?.UniqueID ?? "unknown"})", LogLevel.Info);
            }
            catch (Exception)
            {
                _monitor?.Log("Error occurred while executing version command.", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            // Use TrySetStateFlag to ensure disposal flag is only set once
            if (!TrySetStateFlag(DisposedFlag))
            {
                // Check if the disposal message has already been logged
                int currentState = Volatile.Read(ref _state);
                if ((currentState & DisposedMessageLoggedFlag) == 0)
                {
                    // Try to set the flag to indicate we've logged the disposal message
                    int newState = currentState | DisposedMessageLoggedFlag;
                    if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                    {
                        // Only log if we successfully set the flag (meaning this is the first time logging after disposal)
                        _monitor.Log("Controller is already disposed.", LogLevel.Trace);
                    }
                }
                return; // Already disposed
            }

            // If we reach this point, we successfully set the disposed flag and can proceed with cleanup
            _monitor.Log("Controller disposed successfully.", LogLevel.Trace);
            
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

                // Use Interlocked.Exchange to safely get and clear the handlers
                var gameLaunchedHandler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                var saveLoadedHandler = Interlocked.Exchange(ref _onSaveLoadedHandler, null); // NEW
                var savingHandler = Interlocked.Exchange(ref _onSavingHandler, null); // CORRIGIDO: Era _onSavedHandler // NEW

                if (gameLoop != null && gameLaunchedHandler != null)
                {
                    try
                    {
                        gameLoop.GameLaunched -= gameLaunchedHandler;
                    }
                    catch (Exception)
                    {
                        monitor?.Log("Error occurred while unregistering GameLaunched event.", LogLevel.Error);
                    }
                }

                // Unregister new handlers as well
                if (gameLoop != null && saveLoadedHandler != null) // NEW
                {
                    try
                    {
                        gameLoop.SaveLoaded -= saveLoadedHandler;
                    }
                    catch (Exception)
                    {
                        monitor?.Log("Error occurred while unregistering SaveLoaded event.", LogLevel.Error);
                    }
                }
                
                if (gameLoop != null && savingHandler != null) // CORRIGIDO: Era _onSavedHandler // NEW
                {
                    try
                    {
                        gameLoop.Saving -= savingHandler; // CORRIGIDO: Era gameLoop.Saved // NEW
                    }
                    catch (Exception)
                    {
                        monitor?.Log("Error occurred while unregistering Saving event.", LogLevel.Error);
                    }
                }

                // Ensure the disposed flag remains set and clear other flags
                // Clear other state flags, preserving the disposed flag.
                Interlocked.And(ref _state, ~(EventsRegisteredFlag | CommandRegisteredFlag));
            }
            finally
            {
                // No re-registration after this point; avoid further cleanup that touches SMAPI
            }
        }

        // Helper methods for state checking using volatile reads
        private bool IsDisposed()
        {
            return (Volatile.Read(ref _state) & DisposedFlag) != 0;
        }

        private bool IsEventsRegistered()
        {
            return (Volatile.Read(ref _state) & EventsRegisteredFlag) != 0;
        }

        private bool IsCommandRegistered()
        {
            return (Volatile.Read(ref _state) & CommandRegisteredFlag) != 0;
        }

        /// <summary>
        /// Attempts to set a specific state flag, ensuring thread safety.
        /// This method uses atomic operations to update the state.
        /// </summary>
        /// <param name="flag">The flag to set</param>
        /// <returns>True if the flag was set, false if it was already set</returns>
        private bool TrySetStateFlag(int flag)
        {
            int currentState, newState;
            bool wasSet = false;

            do
            {
                currentState = Volatile.Read(ref _state);

                // If flag is already set, return false
                if ((currentState & flag) != 0)
                    return false;

                // If disposed and trying to set non-disposal flags, return false
                if ((flag != DisposedFlag) && ((currentState & DisposedFlag) != 0))
                    return false;

                newState = currentState | flag;
                wasSet = Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;
            }
            while (!wasSet);

            return wasSet;
        }
    }
}
