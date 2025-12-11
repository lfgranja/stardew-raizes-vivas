using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Controllers
{
    public sealed class ModController : IDisposable
    {
        // State flags for thread safety using atomic operations
        private const int EventsRegisteredFlag = 1 << 0;
        private const int CommandRegisteredFlag = 1 << 1;
        private const int DisposedFlag = 1 << 2;
        private int _state = 0; // Combine flags in single volatile field

        // Dependencies
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _manifest;
        private readonly IModDataService _modDataService;
        private readonly ISoilHealthService _soilHealthService;

        // Event handlers - stored as fields to enable proper unsubscription
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;
        private EventHandler<SaveLoadedEventArgs>? _onSaveLoadedHandler;
        private EventHandler<SavingEventArgs>? _onSavingHandler;

        // Console command registration state
        private readonly object _commandLock = new object();

        // Help flags for console commands
        private static readonly HashSet<string> HelpFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "/?",
            "-?",
            "/help",
            "--help",
            "-h",
            "--h",
            "/help:?",
            "-help:?",
            "/help-",
            "-help-",
            "/?"
        };

        public ModController(
            IModHelper helper, 
            IMonitor monitor, 
            IManifest manifest, 
            IModDataService modDataService,
            ISoilHealthService soilHealthService)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
        }

        public void RegisterEvents()
        {
            // Use TrySetStateFlag to atomically attempt to set the EventsRegisteredFlag
            // This implements the "claim-then-act" pattern to prevent race conditions
            if (!TrySetStateFlag(EventsRegisteredFlag))
            {
                // Another thread already claimed registration rights, so exit gracefully
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
                // Clear the flag since we couldn't actually register anything
                Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                return;
            }

            // Track which events were successfully added for proper rollback
            bool gameLaunchedAdded = false;
            bool saveLoadedAdded = false;
            bool savingAdded = false;

            try
            {
                // Initialize the handlers once
                _onGameLaunchedHandler ??= OnGameLaunched;
                _onSaveLoadedHandler ??= OnSaveLoaded;
                _onSavingHandler ??= OnSaving;

                // Double-check disposed state before subscribing to prevent race condition
                if (IsDisposed())
                {
                    monitor.Log("Controller disposed during registration. Skipping event subscription.", LogLevel.Trace);
                    // Clear the flag since we didn't actually register anything
                    Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                    return;
                }

                // Subscribe to events
                gameLoop.GameLaunched += _onGameLaunchedHandler;
                gameLaunchedAdded = true;

                // NEW EVENTS - Loading and saving soil health data
                gameLoop.SaveLoaded += _onSaveLoadedHandler;
                saveLoadedAdded = true;
                gameLoop.Saving += _onSavingHandler;
                savingAdded = true;

                monitor.Log("Events registered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                monitor.Log("Error occurred while registering game events.", LogLevel.Error);

                // Attempt to rollback any partial subscriptions with individual exception handling
                try
                {
                    if (gameLoop != null) // Guard against null gameLoop in rollback
                    {
                        if (gameLaunchedAdded && _onGameLaunchedHandler != null)
                            gameLoop.GameLaunched -= _onGameLaunchedHandler;
                        if (saveLoadedAdded && _onSaveLoadedHandler != null)
                            gameLoop.SaveLoaded -= _onSaveLoadedHandler;
                        if (savingAdded && _onSavingHandler != null)
                            gameLoop.Saving -= _onSavingHandler;
                    }
                }
                catch
                { 
                    monitor.Log("Error during event subscription rollback.", LogLevel.Trace); 
                    /* avoid masking original failure */ 
                }

                _onGameLaunchedHandler = null;
                _onSaveLoadedHandler = null;
                _onSavingHandler = null;

                // Clear the flag since registration failed
                Interlocked.And(ref _state, ~(EventsRegisteredFlag));

                // According to code review feedback, we should NOT re-throw the exception to maintain consistency with tests
                // The method should handle failures gracefully without propagating exceptions
                return; // Exit gracefully without re-throwing
            }
        }

        public void UnregisterEvents()
        {
            // Fast path: skip if events were never registered
            if ((Volatile.Read(ref _state) & EventsRegisteredFlag) == 0)
            {
                _monitor.Log("Events were not registered, skipping unregistration.", LogLevel.Trace);
                return;
            }

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;

            // Capture GameLoop once for consistent unsubscribe
            var gameLoop = helper?.Events?.GameLoop;
            if (gameLoop == null)
            {
                monitor.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Warn);
                return;
            }

            try
            {
                // Safely unsubscribe from events with null checks and individual exception handling
                if (_onGameLaunchedHandler != null)
                {
                    gameLoop.GameLaunched -= _onGameLaunchedHandler;
                }
                
                if (_onSaveLoadedHandler != null)
                {
                    gameLoop.SaveLoaded -= _onSaveLoadedHandler;
                }
                
                if (_onSavingHandler != null)
                {
                    gameLoop.Saving -= _onSavingHandler;
                }

                // Reset the events registered flag to allow for potential re-registration
                Interlocked.And(ref _state, ~(EventsRegisteredFlag));

                monitor.Log("Events unregistered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                monitor.Log("Error occurred while unregistering game events.", LogLevel.Error);
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

            try
            {
                // Initialize the mod with constants and settings
                _monitor.Log($"The '{_manifest.Name}' mod was loaded successfully!", LogLevel.Info);

                // Register the console command
                RegisterConsoleCommand();
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error occurred in game launched event handler.", LogLevel.Error);
            }
        }

        private void RegisterConsoleCommand()
        {
            // Use a lock to ensure thread safety when registering the command
            lock (_commandLock)
            {
                // Check if command is already registered using the state flag
                if ((Volatile.Read(ref _state) & CommandRegisteredFlag) != 0)
                {
                    return; // Already registered
                }

                try
                {
                    _helper.ConsoleCommands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                    _monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);

                    // Set the command registered flag
                    Interlocked.Or(ref _state, CommandRegisteredFlag);
                }
                catch (Exception ex)
                {
                    // Log error but don't expose raw exception message for security
                    _monitor.Log("Error occurred while registering console command 'lr_version'.", LogLevel.Error);
                }
            }
        }

        private void PrintVersion(string command, string[] args)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

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
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor?.Log("Error occurred while executing version command.", LogLevel.Error);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

            // In SMAPI, the save folder name is available via the game state
            // For testing purposes and reliability, we'll use a fallback approach
            string? saveId = GetSaveIdForDataPersistence();

            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; skipping soil health load.", LogLevel.Warn);
                return;
            }

            try
            {
                // Load data using the save folder name as unique ID
                _soilHealthService.LoadData(saveId);
                _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while loading soil health data for save.", LogLevel.Error);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

            // In SMAPI, the save folder name is available via the game state
            // For testing purposes and reliability, we'll use a fallback approach
            string? saveId = GetSaveIdForDataPersistence();

            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("OnSaving: SaveFolderName unavailable; skipping soil health save.", LogLevel.Warn);
                return;
            }

            try
            {
                // Save data before the game saves/exits (using the saving event)
                _soilHealthService.SaveData(saveId);
                _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while saving soil health data for save.", LogLevel.Error);
            }
        }

        /// <summary>
        /// Gets the save ID for data persistence. This method tries to get the save folder name
        /// from SMAPI context, with fallbacks for test environments.
        /// </summary>
        /// <returns>The save ID or null if unavailable</returns>
        private string? GetSaveIdForDataPersistence()
        {
            try
            {
                // Try to get the save folder name from SMAPI context
                // In SMAPI, this is available through the game state
                var game1Type = Type.GetType("StardewValley.Game1, Stardew Valley");
                if (game1Type != null)
                {
                    var saveFolderField = game1Type.GetField("uniqueIDForThisGame", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (saveFolderField != null)
                    {
                        var value = saveFolderField.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }
                    
                    // Alternative: try to get save folder name if it exists
                    var saveFolderNameField = game1Type.GetField("SaveFolderName", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (saveFolderNameField != null)
                    {
                        var value = saveFolderNameField.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }
                }
                
                // If we're in a test environment or SMAPI context isn't available yet,
                // return null which will be handled by the calling code
                return null;
            }
            catch
            {
                // If anything fails, return null which will be handled by the calling code
                return null;
            }
        }

        public void Dispose()
        {
            // Use TrySetStateFlag to ensure disposal flag is only set once
            if (!TrySetStateFlag(DisposedFlag))
            {
                // If already disposed, check if we need to log the disposal message
                // Only log once if another thread disposed first and we're calling Dispose again
                return; // Already disposed
            }

            // If we reach this point, we successfully set the disposed flag and can proceed with cleanup
            _monitor.Log("Controller disposed successfully.", LogLevel.Trace);

            // Unregister events to prevent memory leaks
            UnregisterEvents();

            // Clean up handlers
            _onGameLaunchedHandler = null;
            _onSaveLoadedHandler = null;
            _onSavingHandler = null;
        }

        /// <summary>
        /// Checks if the controller has been disposed of.
        /// </summary>
        /// <returns>True if the controller is disposed, false otherwise</returns>
        private bool IsDisposed()
        {
            return (Volatile.Read(ref _state) & DisposedFlag) != 0;
        }

        /// <summary>
        /// Attempts to set a state flag atomically, ensuring thread safety.
        /// This method uses CompareExchange to prevent race conditions when
        /// setting flags like EventsRegisteredFlag or DisposedFlag.
        /// </summary>
        /// <param name="flag">The flag to set</param>
        /// <returns>True if the flag was set, false if it was already set</returns>
        private bool TrySetStateFlag(int flag)
        {
            int spinCount = 0;
            while (true)
            {
                int currentState = Volatile.Read(ref _state);
                
                // Check if already disposed when trying to set other flags
                if ((flag & ~DisposedFlag) != 0 && (currentState & DisposedFlag) != 0)
                    return false; // Don't set other flags if disposed

                // If the flag is already set, return false
                if ((currentState & flag) != 0)
                    return false;

                // Calculate new state with the flag set
                int newState = currentState | flag;

                // Attempt to set the state atomically
                int observedState = Interlocked.CompareExchange(ref _state, newState, currentState);

                if (observedState == currentState)
                    return true; // Successfully set the flag

                // Handle spurious failures with exponential backoff
                if (++spinCount > 10)
                {
                    Thread.Yield();
                    spinCount = 0;
                }
                else
                {
                    Thread.SpinWait(1 << spinCount);
                }
            }
        }
    }
}
