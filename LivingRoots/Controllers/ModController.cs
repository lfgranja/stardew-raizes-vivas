using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        // Re-entrancy guard flags for event handlers
        private const int OnSaveLoadedExecutingFlag = 1 << 3;
        private const int OnSavingExecutingFlag = 1 << 4;
        
        // Dependencies
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _manifest;
        private readonly IModDataService _modDataService;
        private readonly ISoilHealthService _soilHealthService;
        private readonly ISaveIdProvider _saveIdProvider;

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
            "-help-"
        };

        public ModController(
            IModHelper helper, 
            IMonitor monitor, 
            IManifest manifest, 
            IModDataService modDataService,
            ISoilHealthService soilHealthService,
            ISaveIdProvider saveIdProvider)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
            _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
            _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));
        }

        public void RegisterEvents()
        {
            // Early disposal check: prevent registration if controller is already disposed
            if (IsDisposed())
            {
                _monitor.Log("Controller is disposed, skipping event registration.", LogLevel.Trace);
                return;
            }

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
            // Use Interlocked.Exchange to implement an atomic 'clear-and-claim' pattern
            // This ensures that only one thread can successfully claim the unregistration operation
            // The approach: atomically claim the operation by setting a special state, then properly handle
            
            // First, we'll implement the clear-and-claim pattern by using a strategy that leverages
            // Interlocked.Exchange to atomically read and potentially modify state.
            // Since we need to preserve other flags while clearing EventsRegisteredFlag,
            // we'll use CompareExchange in a loop as this is the most appropriate approach.
            
            int currentState, newState;
            do
            {
                currentState = Volatile.Read(ref _state);
                
                // Check if events were registered before proceeding with the clear operation
                if ((currentState & EventsRegisteredFlag) == 0)
                {
                    // Events were not registered, so nothing to unregister
                    _monitor.Log("Events were not registered or already unregistered, skipping unregistration.", LogLevel.Trace);
                    return;
                }
                
                // Calculate new state with EventsRegisteredFlag cleared while preserving other flags
                newState = currentState & ~EventsRegisteredFlag;
            }
            // Use CompareExchange in a loop to atomically update the state
            // This implements the 'clear-and-claim' pattern by ensuring only one thread
            // can successfully clear the EventsRegisteredFlag
            while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            // At this point, we have successfully claimed the unregistration operation
            // and cleared the EventsRegisteredFlag. Now we need to atomically clear the event handlers
            // using Interlocked.Exchange to ensure thread safety

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;

            // Capture GameLoop once for consistent unsubscribe
            var gameLoop = helper?.Events?.GameLoop;
            if (gameLoop == null)
            {
                monitor.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Warn);
                // Even when gameLoop is null, we've already cleared the flag,
                // so just nullify the event handler fields to ensure clean state
                _onGameLaunchedHandler = null;
                _onSaveLoadedHandler = null;
                _onSavingHandler = null;
                return;
            }

            try
            {
                // Atomically clear each event handler using Interlocked.Exchange to prevent
                // race conditions where multiple threads might try to access these handlers
                var gameLaunchedHandler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                var saveLoadedHandler = Interlocked.Exchange(ref _onSaveLoadedHandler, null);
                var savingHandler = Interlocked.Exchange(ref _onSavingHandler, null);

                // Safely unsubscribe from events with null checks and individual exception handling
                if (gameLaunchedHandler != null)
                {
                    gameLoop.GameLaunched -= gameLaunchedHandler;
                }
                
                if (saveLoadedHandler != null)
                {
                    gameLoop.SaveLoaded -= saveLoadedHandler;
                }
                
                if (savingHandler != null)
                {
                    gameLoop.Saving -= savingHandler;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                monitor.Log("Error occurred while unregistering game events.", LogLevel.Error);
            }
            finally
            {
                // The event handler fields have already been set to null via Interlocked.Exchange
                monitor.Log("Events unregistered successfully.", LogLevel.Trace);
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
            // Early disposal check: prevent registration if controller is already disposed
            if (IsDisposed())
            {
                _monitor.Log("Controller is disposed, skipping console command registration.", LogLevel.Trace);
                return;
            }

            // Use a lock to ensure thread safety when registering the command
            lock (_commandLock)
            {
                // Check if command is already registered using the state flag
                if ((Volatile.Read(ref _state) & CommandRegisteredFlag) != 0)
                {
                    return; // Already registered
                }

                // Double-check disposed state inside the lock to prevent race condition
                if (IsDisposed())
                {
                    _monitor.Log("Controller disposed during command registration. Skipping command registration.", LogLevel.Trace);
                    return;
                }

                try
                {
                    // Add null check for _helper and _helper.ConsoleCommands to prevent NullReferenceException
                    if (_helper?.ConsoleCommands == null)
                    {
                        _monitor.Log("ConsoleCommands is null, cannot register console command 'lr_version'.", LogLevel.Error);
                        return;
                    }
                    
                    _helper.ConsoleCommands.Add("lr_version", "Shows the Living Roots version.", PrintVersion);
                    _monitor.Log("Console command 'lr_version' registered successfully.", LogLevel.Trace);

                    // Set the command registered flag atomically - only after successful registration
                    Interlocked.Or(ref _state, CommandRegisteredFlag);
                }
                catch (Exception ex)
                {
                    // Log error but don't expose raw exception message for security
                    _monitor.Log("Error occurred while registering console command 'lr_version'.", LogLevel.Error);
                    
                    // Ensure the CommandRegisteredFlag is not set if registration failed
                    // This is important to maintain atomic state - if an exception occurs during registration,
                    // we don't want the flag to indicate success when it actually failed
                    Interlocked.And(ref _state, ~CommandRegisteredFlag);
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

                // Check if any non-whitespace argument matches a help flag using LINQ Any()
                if (args.Any(arg => !string.IsNullOrWhiteSpace(arg) && HelpFlags.Contains(arg)))
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
            // Use Interlocked.CompareExchange to implement a thread-safe re-entrancy guard
            // This ensures only one execution of OnSaveLoaded can happen at a time
            int currentState, newState;
            do
            {
                currentState = Volatile.Read(ref _state);
                
                // If the OnSaveLoadedExecutingFlag is already set, this method is already running, so return
                if ((currentState & OnSaveLoadedExecutingFlag) != 0)
                {
                    _monitor.Log("OnSaveLoaded is already executing, skipping concurrent execution.", LogLevel.Trace);
                    return;
                }
                
                // Calculate new state with the OnSaveLoadedExecutingFlag set
                newState = currentState | OnSaveLoadedExecutingFlag;
            } 
            while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            try
            {
                // Double-check locking pattern: check if controller is disposed after acquiring execution flag
                if (IsDisposed())
                {
                    _monitor.Log("Controller disposed after acquiring OnSaveLoaded execution flag, skipping execution.", LogLevel.Trace);
                    return;
                }

                // Get the save ID using the abstraction (monitor is already available in the provider)
                string? saveId = _saveIdProvider.GetSaveId();

                if (string.IsNullOrWhiteSpace(saveId))
                {
                    _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; skipping soil health load.", LogLevel.Warn);
                    return;
                }

                // Load data using the save folder name as unique ID
                _soilHealthService.LoadData(saveId);
                _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while loading soil health data for save.", LogLevel.Error);
            }
            finally
            {
                // Clear the executing flag when done to allow future executions
                Interlocked.And(ref _state, ~OnSaveLoadedExecutingFlag);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // Use Interlocked.CompareExchange to implement a thread-safe re-entrancy guard
            // This ensures only one execution of OnSaving can happen at a time
            int currentState, newState;
            do
            {
                currentState = Volatile.Read(ref _state);
                
                // If the OnSavingExecutingFlag is already set, this method is already running, so return
                if ((currentState & OnSavingExecutingFlag) != 0)
                {
                    _monitor.Log("OnSaving is already executing, skipping concurrent execution.", LogLevel.Trace);
                    return;
                }
                
                // Calculate new state with the OnSavingExecutingFlag set
                newState = currentState | OnSavingExecutingFlag;
            } 
            while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            try
            {
                // Double-check locking pattern: check if controller is disposed after acquiring execution flag
                if (IsDisposed())
                {
                    _monitor.Log("Controller disposed after acquiring OnSaving execution flag, skipping execution.", LogLevel.Trace);
                    return;
                }

                // Get the save ID using the abstraction (monitor is already available in the provider)
                string? saveId = _saveIdProvider.GetSaveId();

                if (string.IsNullOrWhiteSpace(saveId))
                {
                    _monitor.Log("OnSaving: SaveFolderName unavailable; skipping soil health save.", LogLevel.Warn);
                    return;
                }

                // Save data before the game saves/exits (using the saving event)
                _soilHealthService.SaveData(saveId);
                _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while saving soil health data for save.", LogLevel.Error);
            }
            finally
            {
                // Clear the executing flag when done to allow future executions
                Interlocked.And(ref _state, ~OnSavingExecutingFlag);
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

            // The UnregisterEvents() method already handles setting the event handlers to null
            // in a thread-safe manner using Interlocked.Exchange, so these redundant assignments
            // are not needed and have been removed to follow DRY principle
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
            int currentState;
            int newState;
            do
            {
                currentState = Volatile.Read(ref _state);
                
                // Check if already disposed when trying to set other flags
                if ((flag & ~DisposedFlag) != 0 && (currentState & DisposedFlag) != 0)
                    return false; // Don't set other flags if disposed

                // If the flag is already set, return false
                if ((currentState & flag) != 0)
                    return false;

                // Calculate new state with the flag set
                newState = currentState | flag;

            } while (Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            return true; // Successfully set the flag
        }
    }
}
