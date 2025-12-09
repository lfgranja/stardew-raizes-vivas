using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LivingRoots.Domain;
using LivingRoots.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Controllers
{
    public sealed class ModController : IDisposable
    {
        // State flags for thread safety using atomic operations
        private const int EventsRegisteredFlag = 1 << 0;
        private const int DisposedFlag = 1 << 1;
        private const int DisposedMessageLoggedFlag = 1 << 2;
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
            // Fast path: skip if already registered
            if ((Volatile.Read(ref _state) & EventsRegisteredFlag) != 0)
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

                // Only set the flag after all subscriptions succeed
                if (TrySetStateFlag(EventsRegisteredFlag))
                {
                    monitor.Log("Events registered successfully.", LogLevel.Trace);
                }
                else
                {
                    monitor.Log("Events were registered concurrently; skipping duplicate registration.", LogLevel.Trace);
                    // Rollback our subscriptions since another thread beat us
                    try
                    {
                        if (gameLaunchedAdded && _onGameLaunchedHandler != null)
                            gameLoop.GameLaunched -= _onGameLaunchedHandler;
                        if (saveLoadedAdded && _onSaveLoadedHandler != null)
                            gameLoop.SaveLoaded -= _onSaveLoadedHandler;
                        if (savingAdded && _onSavingHandler != null)
                            gameLoop.Saving -= _onSavingHandler;
                    }
                    catch
                    {
                        monitor.Log("Error during concurrent registration rollback.", LogLevel.Trace);
                    }
                    return;
                }
            }
            catch (Exception)
            {
                // Log error and reset the flag if registration failed - ensure disposed flag is preserved
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
                catch // Changed from catch (Exception rollbackEx) to catch to remove unused variable
                { 
                    monitor.Log("Error during event subscription rollback.", LogLevel.Trace); 
                    /* avoid masking original failure */ 
                }

                _onGameLaunchedHandler = null;
                _onSaveLoadedHandler = null;
                _onSavingHandler = null;

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
            catch (Exception)
            {
                // Log error but don't rethrow to prevent disposal chain interruption
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
            }
            catch (Exception)
            {
                _monitor.Log("Error occurred in game launched event handler.", LogLevel.Error);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

            // Capture the save ID once to prevent race conditions
            string? saveId = null;
            try
            {
                saveId = Constants.SaveFolderName;
            }
            catch
            {
                // If Constants.SaveFolderName is unavailable, skip loading
            }

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
            catch (Exception)
            {
                _monitor.Log($"Error occurred while loading soil health data for save '{saveId}'.", LogLevel.Error);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // Skip if controller has been disposed
            if (IsDisposed())
                return;

            // Capture the save ID once to prevent race conditions
            string? saveId = null;
            try
            {
                saveId = Constants.SaveFolderName;
            }
            catch
            {
                // If Constants.SaveFolderName is unavailable, skip saving
            }

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
            catch (Exception)
            {
                _monitor.Log($"Error occurred while saving soil health data for save '{saveId}'.", LogLevel.Error);
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
