using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Threading;

namespace LivingRoots.Controllers
{
    public sealed class ModController : IDisposable
    {
        // State flags for thread safety using atomic operations
        private const int EventsRegisteredFlag = 1 << 0;
        private const int CommandRegisteredFlag = 1 << 1;
        private const int DisposedFlag = 1 << 2;
        private const int OnSaveLoadedExecutingFlag = 1 << 3;
        private const int OnSavingExecutingFlag = 1 << 4;
        private const int UnregisteringFlag = 1 << 5;
        internal int _state = 0; // Combine flags in single volatile field
        
        // Warning flag for preventing repeated log spam - using Interlocked operations for thread safety
        internal int _saveIdUnavailableWarningShownOnSaveLoaded = 0; // 0 = false, 1 = true
        internal int _saveIdUnavailableWarningShownOnSaving = 0; // 0 = false, 1 = true

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

            // Don't register while another thread is actively unregistering.
            if ((Volatile.Read(ref _state) & UnregisteringFlag) != 0)
            {
                _monitor.Log("Event unregistration in progress, skipping registration.", LogLevel.Trace);
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

            // Create local references to handlers for safe access during rollback
            EventHandler<GameLaunchedEventArgs>? localGameLaunchedHandler = null;
            EventHandler<SaveLoadedEventArgs>? localSaveLoadedHandler = null;
            EventHandler<SavingEventArgs>? localSavingHandler = null;

            try
            {
                // Initialize the handlers once and assign to local references
                localGameLaunchedHandler = _onGameLaunchedHandler ??= OnGameLaunched;
                localSaveLoadedHandler = _onSaveLoadedHandler ??= OnSaveLoaded;
                localSavingHandler = _onSavingHandler ??= OnSaving;

                // Double-check disposed state before subscribing to prevent race condition
                if (IsDisposed())
                {
                    monitor.Log("Controller disposed during registration. Skipping event subscription.", LogLevel.Trace);
                    // Clear the flag since we didn't actually register anything
                    Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                    return;
                }

                // Subscribe to events
                gameLoop.GameLaunched += localGameLaunchedHandler;
                gameLaunchedAdded = true;

                // NEW EVENTS - Loading and saving soil health data
                gameLoop.SaveLoaded += localSaveLoadedHandler;
                saveLoadedAdded = true;
                gameLoop.Saving += localSavingHandler;
                savingAdded = true;

                monitor.Log("Events registered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                monitor.Log("Error occurred while registering game events.", LogLevel.Error);
                
                // Add trace-level exception details for debugging without leaking message content
                monitor.Log($"RegisterEvents exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                #if DEBUG
                monitor.Log(ex.StackTrace ?? "RegisterEvents stack trace unavailable.", LogLevel.Trace);
                #endif

                // Attempt to rollback any partial subscriptions with individual exception handling
                try
                {
                    if (gameLoop != null) // Guard against null gameLoop in rollback
                    {
                        // Use local handler references to prevent race conditions during rollback
                        if (gameLaunchedAdded && localGameLaunchedHandler != null)
                            gameLoop.GameLaunched -= localGameLaunchedHandler;
                        if (saveLoadedAdded && localSaveLoadedHandler != null)
                            gameLoop.SaveLoaded -= localSaveLoadedHandler;
                        if (savingAdded && localSavingHandler != null)
                            gameLoop.Saving -= localSavingHandler;
                    }
                }
                catch
                { 
                    monitor.Log("Error during event subscription rollback.", LogLevel.Trace); 
                    /* avoid masking original failure */ 
                }

                // Use Interlocked.Exchange for thread-safe nullification of event handlers
                Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                Interlocked.Exchange(ref _onSaveLoadedHandler, null);
                Interlocked.Exchange(ref _onSavingHandler, null);

                // Clear the flag since registration failed
                Interlocked.And(ref _state, ~(EventsRegisteredFlag));

                // According to code review feedback, we should NOT re-throw the exception to maintain consistency with tests
                // The method should handle failures gracefully without propagating exceptions
                return; // Exit gracefully without re-throwing
            }
        }

        public void UnregisterEvents()
        {
            // Early exit: if nothing registered and nothing to cleanup, return immediately
            if ((Volatile.Read(ref _state) & (EventsRegisteredFlag | UnregisteringFlag)) == 0)
            {
                // Check if any handlers are non-null for best-effort cleanup
                var earlyGameLaunchedHandler = System.Threading.Volatile.Read(ref _onGameLaunchedHandler);
                var earlySaveLoadedHandler = System.Threading.Volatile.Read(ref _onSaveLoadedHandler);
                var earlySavingHandler = System.Threading.Volatile.Read(ref _onSavingHandler);
                
                bool earlyHasHandlers = earlyGameLaunchedHandler != null || earlySaveLoadedHandler != null || earlySavingHandler != null;
                
                if (!earlyHasHandlers)
                {
                    _monitor.Log("Events were not registered or already unregistered, skipping unregistration.", LogLevel.Trace);
                    return;
                }
            }

            // State-claiming loop: always atomically set UnregisteringFlag before proceeding
            int currentState, newState;
            while (true)
            {
                currentState = Volatile.Read(ref _state);
                
                // Check if another thread is already unregistering to prevent concurrent unregistration
                if ((currentState & UnregisteringFlag) != 0)
                {
                    _monitor.Log("Event unregistration already in progress, skipping.", LogLevel.Trace);
                    return;
                }
                
                // Always set UnregisteringFlag to prevent concurrent registration
                // Clear EventsRegisteredFlag if it was set (normal unregistration path)
                newState = (currentState | UnregisteringFlag) & ~EventsRegisteredFlag;

                // Atomically claim unregistration rights
                if (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                {
                    break; // Successfully claimed unregistration
                }
                // CAS failed: another thread modified state, retry
            }

            // Capture handler references AFTER UnregisteringFlag has been atomically set
            // This prevents race condition where newly registered events could be leaked
            var gameLaunchedHandler = System.Threading.Volatile.Read(ref _onGameLaunchedHandler);
            var saveLoadedHandler = System.Threading.Volatile.Read(ref _onSaveLoadedHandler);
            var savingHandler = System.Threading.Volatile.Read(ref _onSavingHandler);

            // Check if any handlers are non-null for best-effort cleanup
            bool hasHandlers = gameLaunchedHandler != null || saveLoadedHandler != null || savingHandler != null;

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitor = _monitor;
            var helper = _helper;
            bool allUnsubscribed = true;

            try
            {
                // Capture GameLoop once for consistent unsubscribe
                var gameLoop = helper?.Events?.GameLoop;
                if (gameLoop == null)
                {
                    monitor.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Warn);
                    allUnsubscribed = false;
                    return;
                }

                // IMPLEMENT DRY PRINCIPLE: Use SafeUnsubscribe helper method to reduce code duplication
                bool gameLaunchedRemoved = SafeUnsubscribe<GameLaunchedEventArgs>(h => gameLoop.GameLaunched -= h, gameLaunchedHandler, "GameLaunched");
                bool saveLoadedRemoved = SafeUnsubscribe<SaveLoadedEventArgs>(h => gameLoop.SaveLoaded -= h, saveLoadedHandler, "SaveLoaded");
                bool savingRemoved = SafeUnsubscribe<SavingEventArgs>(h => gameLoop.Saving -= h, savingHandler, "Saving");

                allUnsubscribed = gameLaunchedRemoved && saveLoadedRemoved && savingRemoved;

                // Only nullify handler fields after successful unsubscription to prevent memory leaks
                // and inconsistent state. Use CompareExchange to ensure we only nullify if the
                // handler hasn't changed since we captured it.
                if (gameLaunchedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onGameLaunchedHandler, null, gameLaunchedHandler);
                if (saveLoadedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onSaveLoadedHandler, null, saveLoadedHandler);
                if (savingRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onSavingHandler, null, savingHandler);

                if (allUnsubscribed)
                {
                    monitor.Log("Events unregistered successfully.", LogLevel.Trace);
                }
                else
                {
                    monitor.Log("Event unregistration partially failed. Some handlers may remain subscribed.", LogLevel.Warn);
                }
            }
            finally
            {
                // Update event-registration flags to maintain consistent state.
                // The EventsRegisteredFlag was cleared at the start of unregistration.
                // If unregistration was incomplete, restore the flag to indicate handlers
                // are still subscribed, preventing duplicate subscriptions.
                if (IsDisposed())
                {
                    // During disposal, force-clear all lifecycle flags.
                    System.Threading.Interlocked.And(ref _state, ~(EventsRegisteredFlag | CommandRegisteredFlag));
                }
                else if (!allUnsubscribed)
                {
                    // If unregistration was incomplete, restore EventsRegisteredFlag.
                    // This indicates that handlers are still subscribed and prevents
                    // duplicate subscriptions if RegisterEvents is called again.
                    System.Threading.Interlocked.Or(ref _state, EventsRegisteredFlag);
                    monitor.Log("EventsRegisteredFlag restored due to incomplete unregistration.", LogLevel.Trace);
                }
                // If allUnsubscribed is true, EventsRegisteredFlag remains cleared (as set at start)

                // Clear "unregistering" claim last, once state is consistent.
                System.Threading.Interlocked.And(ref _state, ~UnregisteringFlag);
            }
        }

        /// <summary>
        /// Safely unsubscribes from an event with individual exception handling to prevent one failure from affecting others.
        /// </summary>
        /// <typeparam name="T">The event args type</typeparam>
        /// <param name="unsubscribeAction">The action to perform the unsubscription</param>
        /// <param name="handler">The event handler to unsubscribe</param>
        /// <param name="eventName">The name of the event for logging purposes</param>
        /// <returns>True if unsubscription was attempted and succeeded, false if unsubscription failed</returns>
        private bool SafeUnsubscribe<T>(Action<EventHandler<T>> unsubscribeAction, EventHandler<T>? handler, string eventName) where T : EventArgs
        {
            // Nothing to unsubscribe; treat as success to avoid false failure signals.
            if (handler == null) return true;
            
            try
            {
                unsubscribeAction(handler);
                return true;
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while unsubscribing from {eventName} event.", LogLevel.Error);
                
                // Add trace-level exception details for debugging
                _monitor.Log($"{eventName} unsubscription exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? $"{eventName} unsubscription stack trace unavailable.", LogLevel.Trace);
                #endif
                return false; // Indicate that unsubscription failed
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
                
                // Add trace-level exception details for debugging
                _monitor.Log($"OnGameLaunched exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? "OnGameLaunched stack trace unavailable.", LogLevel.Trace);
                #endif
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Use Interlocked.CompareExchange to implement a thread-safe re-entrancy guard
            // This ensures only one execution of OnSaveLoaded can happen at a time
            int currentState, newState;
            do
            {
                currentState = System.Threading.Volatile.Read(ref _state);
                
                // If the OnSaveLoadedExecutingFlag is already set, this method is already running, so return
                if ((currentState & OnSaveLoadedExecutingFlag) != 0)
                {
                    _monitor.Log("OnSaveLoaded is already executing, skipping concurrent execution.", LogLevel.Trace);
                    return;
                }
                
                // Calculate new state with the OnSaveLoadedExecutingFlag set
                newState = currentState | OnSaveLoadedExecutingFlag;
            } 
            while (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

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
                    // Don't call LoadData with an invalid/empty ID (could load/save under a bogus key).
                    // Prefer an explicit reset/clear API in the soil-health service to prevent cross-save leakage.
                    _soilHealthService.Reset();

                    if (System.Threading.Interlocked.CompareExchange(ref _saveIdUnavailableWarningShownOnSaveLoaded, 1, 0) == 0)
                    {
                        _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; soil health state reset to prevent cross-save data leakage.", LogLevel.Warn);
                    }
                    return;
                }

                // Reset the warning flag when a valid save ID is found
                if (System.Threading.Interlocked.CompareExchange(ref _saveIdUnavailableWarningShownOnSaveLoaded, 0, 1) == 1)
                {
                    // The flag was previously set to 1 (true), so we reset it to 0 (false)
                    // This means the warning was previously shown and is now being reset
                }

                // Load data using the save folder name as unique ID
                _soilHealthService.LoadData(saveId);
                _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error occurred while loading soil health data for save.", LogLevel.Error);
                
                // Add trace-level exception details for debugging
                _monitor.Log($"OnSaveLoaded exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? "OnSaveLoaded stack trace unavailable.", LogLevel.Trace);
                #endif
            }
            finally
            {
                // Clear the executing flag when done to allow future executions
                System.Threading.Interlocked.And(ref _state, ~OnSaveLoadedExecutingFlag);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // Use Interlocked.CompareExchange to implement a thread-safe re-entrancy guard
            // This ensures only one execution of OnSaving can happen at a time
            int currentState, newState;
            do
            {
                currentState = System.Threading.Volatile.Read(ref _state);
                
                // If the OnSavingExecutingFlag is already set, this method is already running, so return
                if ((currentState & OnSavingExecutingFlag) != 0)
                {
                    _monitor.Log("OnSaving is already executing, skipping concurrent execution.", LogLevel.Trace);
                    return;
                }
                
                // Calculate new state with the OnSavingExecutingFlag set
                newState = currentState | OnSavingExecutingFlag;
            } 
            while (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

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
                    // Only show warning once to prevent log spam using Interlocked operations
                    if (System.Threading.Interlocked.CompareExchange(ref _saveIdUnavailableWarningShownOnSaving, 1, 0) == 0)
                    {
                        // The flag was previously 0 (false), so we set it to 1 (true) and show the warning
                        _monitor.Log("OnSaving: SaveFolderName unavailable; skipping soil health save.", LogLevel.Warn);
                    }
                    return;
                }

                // Reset the warning flag when a valid save ID is found
                if (System.Threading.Interlocked.CompareExchange(ref _saveIdUnavailableWarningShownOnSaving, 0, 1) == 1)
                {
                    // The flag was previously set to 1 (true), so we reset it to 0 (false)
                    // This means the warning was previously shown and is now being reset
                }

                // Save data before the game saves/exits (using the saving event)
                _soilHealthService.SaveData(saveId);
                _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error occurred while saving soil health data for save.", LogLevel.Error);
                
                // Add trace-level exception details for debugging
                _monitor.Log($"OnSaving exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor.Log(ex.StackTrace ?? "OnSaving stack trace unavailable.", LogLevel.Trace);
                #endif
            }
            finally
            {
                // Clear the executing flag when done to allow future executions
                System.Threading.Interlocked.And(ref _state, ~OnSavingExecutingFlag);
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
                if ((System.Threading.Volatile.Read(ref _state) & CommandRegisteredFlag) != 0)
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
                    
                    // Set the command registered flag atomically only on successful registration
                    // This prevents repeated registration attempts and ensures the flag reflects actual registration state
                    System.Threading.Interlocked.Or(ref _state, CommandRegisteredFlag);
                }
                catch (Exception ex)
                {
                    // Log error but don't expose raw exception message for security
                    _monitor.Log("Error occurred while registering console command 'lr_version'.", LogLevel.Error);
                    
                    // Add trace-level exception details for debugging
                    _monitor.Log($"RegisterConsoleCommand exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    
                    // Add stack trace logging for better diagnostics without exposing sensitive information
                    #if DEBUG
                    _monitor.Log(ex.StackTrace ?? "RegisterConsoleCommand stack trace unavailable.", LogLevel.Trace);
                    #endif
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
                // Trim arguments before matching to handle cases like " /help " or "-help "
                if (args.Any(arg => !string.IsNullOrWhiteSpace(arg) && HelpFlags.Contains(arg.Trim())))
                {
                    monitor?.Log("Usage: lr_version", LogLevel.Info);
                    monitor?.Log("Shows the Living Roots version and UniqueID.", LogLevel.Info);
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
                
                // Add trace-level exception details for debugging
                _monitor?.Log($"PrintVersion exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                
                // Add stack trace logging for better diagnostics without exposing sensitive information
                #if DEBUG
                _monitor?.Log(ex.StackTrace ?? "PrintVersion stack trace unavailable.", LogLevel.Trace);
                #endif
            }
        }

        public void Dispose()
        {
            // Use TrySetStateFlag to ensure disposal flag is only set once
            if (!TrySetStateFlag(DisposedFlag))
            {
                // If already disposed, return to prevent duplicate disposal processing
                return; // Already disposed
            }

            // If we reach this point, we successfully set the disposed flag and can proceed with cleanup
            _monitor.Log("Controller disposed successfully.", LogLevel.Trace);

            // Unregister events to prevent memory leaks
            UnregisterEvents();
        }

        /// <summary>
        /// Checks if the controller has been disposed of.
        /// </summary>
        /// <returns>True if the controller is disposed, false otherwise</returns>
        internal bool IsDisposed()
        {
            return (System.Threading.Volatile.Read(ref _state) & DisposedFlag) != 0;
        }

        /// <summary>
        /// Attempts to set a state flag atomically, ensuring thread safety.
        /// This method uses CompareExchange to prevent race conditions when
        /// setting flags like EventsRegisteredFlag or DisposedFlag.
        /// </summary>
        /// <param name="flag">The flag to set</param>
        /// <returns>True if the flag was set, false if it was already set</returns>
        internal bool TrySetStateFlag(int flag)
        {
            int currentState;
            int newState;
            do
            {
                currentState = System.Threading.Volatile.Read(ref _state);
                
                // Check if already disposed when trying to set other flags
                if ((flag & ~DisposedFlag) != 0 && (currentState & DisposedFlag) != 0)
                    return false; // Don't set other flags if disposed

                // If the flag is already set, return false
                if ((currentState & flag) != 0)
                    return false;

                // Calculate new state with the flag set
                newState = currentState | flag;

            } while (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            return true; // Successfully set the flag
        }
    }
}
