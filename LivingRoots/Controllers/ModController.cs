using LivingRoots.Domain;
using LivingRoots.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Controllers
{
    public sealed class ModController(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ISoilHealthService soilHealthService,
        ISaveIdProvider saveIdProvider) : IDisposable
    {
        // State flags for thread safety using atomic operations
        internal const int EventsRegisteredFlag = 1 << 0;
        internal const int CommandRegisteredFlag = 1 << 1;
        internal const int DisposedFlag = 1 << 2;
        internal const int OnSaveLoadedExecutingFlag = 1 << 3;
        internal const int OnSavingExecutingFlag = 1 << 4;
        internal const int UnregisteringFlag = 1 << 5;
        internal const int RegisteringFlag = 1 << 6;
        internal int _state = 0; // Combine flags in single volatile field

        // Warning flag for preventing repeated log spam - using Interlocked operations for thread safety
        internal int _saveIdUnavailableWarningShownOnSaveLoaded = 0; // 0 = false, 1 = true
        internal int _saveIdUnavailableWarningShownOnSaving = 0; // 0 = false, 1 = true

        // Dependencies
        private readonly IModHelper _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        private readonly IManifest _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
        private readonly ISaveIdProvider _saveIdProvider = saveIdProvider ?? throw new ArgumentNullException(nameof(saveIdProvider));

        // Event handlers - stored as fields to enable proper unsubscription
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;
        private EventHandler<SaveLoadedEventArgs>? _onSaveLoadedHandler;
        private EventHandler<SavingEventArgs>? _onSavingHandler;

        // Console command registration state
        private readonly object _commandLock = new();

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

        public void RegisterEvents()
        {
            // Early disposal check: check if controller is disposed before accessing dependencies
            if (IsDisposed())
            {
                _monitor.Log("Controller is disposed, skipping event registration.", LogLevel.Trace);
                return;
            }

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitorSnapshot = _monitor;
            var helperSnapshot = _helper;
            // Capture GameLoop once for consistent subscribe/rollback
            var gameLoop = helperSnapshot?.Events?.GameLoop;
            if (gameLoop == null)
            {
                monitorSnapshot.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                return;
            }

            if (!TryEnterRegisteringState(monitorSnapshot))
            {
                return;
            }

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

                // Double-check disposed state after setting the flag but before subscribing to prevent race condition
                if (IsDisposed())
                {
                    monitorSnapshot.Log("Controller disposed during registration. Skipping event subscription.", LogLevel.Trace);
                    // Clear the flag since we didn't actually register anything
                    System.Threading.Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                    return;
                }

                // Subscribe to events
                gameLoop.GameLaunched += localGameLaunchedHandler;
                gameLoop.SaveLoaded += localSaveLoadedHandler;
                gameLoop.Saving += localSavingHandler;

                // now that everything succeeded, publish the "registered" state
                System.Threading.Interlocked.Or(ref _state, EventsRegisteredFlag);
                monitorSnapshot.Log("Events registered successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                HandleRegistrationError(new RegistrationContext
                {
                    Exception = ex,
                    GameLoop = gameLoop,
                    LocalGameLaunchedHandler = localGameLaunchedHandler,
                    LocalSaveLoadedHandler = localSaveLoadedHandler,
                    LocalSavingHandler = localSavingHandler,
                    Monitor = monitorSnapshot
                });
            }
            finally
            {
                // always clear "registering" claim
                System.Threading.Interlocked.And(ref _state, ~RegisteringFlag);
            }
        }

        private bool TryEnterRegisteringState(IMonitor monitor)
        {
            int currentState, newState;
            do
            {
                currentState = System.Threading.Volatile.Read(ref _state);
                if ((currentState & DisposedFlag) != 0)
                {
                    monitor.Log("Controller is disposed, skipping event registration.", LogLevel.Trace);
                    return false;
                }
                if ((currentState & UnregisteringFlag) != 0)
                {
                    monitor.Log("Event unregistration in progress, skipping registration.", LogLevel.Trace);
                    return false;
                }
                if ((currentState & (EventsRegisteredFlag | RegisteringFlag)) != 0)
                {
                    monitor.Log("Events are already registered or registering, skipping registration.", LogLevel.Trace);
                    return false;
                }
                // claim "registering" to block concurrent attempts without lying about success
                newState = currentState | RegisteringFlag;
            } while (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            return true;
        }

        private void HandleRegistrationError(RegistrationContext ctx)
        {
            ctx.Monitor.Log("Error occurred while registering game events.", LogLevel.Error);
            ctx.Monitor.Log($"RegisterEvents exception type: {ctx.Exception.GetType().FullName} (HResult: 0x{ctx.Exception.HResult:X8})", LogLevel.Trace);
#if DEBUG
            ctx.Monitor.Log(ctx.Exception.StackTrace ?? "RegisterEvents stack trace unavailable.", LogLevel.Trace);
#endif

            // Execute rollback safely; don't let rollback failures mask original error.
            SafeUnsubscribe<GameLaunchedEventArgs>(
                ctx.Monitor,
                h => ctx.GameLoop.GameLaunched -= h,
                ctx.LocalGameLaunchedHandler,
                "GameLaunched");

            SafeUnsubscribe<SaveLoadedEventArgs>(
                ctx.Monitor,
                h => ctx.GameLoop.SaveLoaded -= h,
                ctx.LocalSaveLoadedHandler,
                "SaveLoaded");

            SafeUnsubscribe<SavingEventArgs>(
                ctx.Monitor,
                h => ctx.GameLoop.Saving -= h,
                ctx.LocalSavingHandler,
                "Saving");

            // Clear handler references to prevent memory leaks
            System.Threading.Interlocked.Exchange(ref _onGameLaunchedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onSaveLoadedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onSavingHandler, null);

            System.Threading.Interlocked.And(ref _state, ~(EventsRegisteredFlag));
        }

        public void UnregisterEvents()
        {
            // Early exit: if nothing registered and nothing to cleanup, return immediately
            if (!ShouldAttemptUnregister())
            {
                return;
            }

            // State-claiming loop: atomically claim UnregisteringFlag before proceeding
            if (!TryEnterUnregisteringState())
            {
                return;
            }

            // Capture handler references AFTER UnregisteringFlag has been atomically set
            // This prevents race condition where newly registered events could be leaked
            var eventUnregisterContext = CreateUnregisterContext();

            // Create snapshots of dependencies to avoid errors if disposed mid-execution
            var monitorSnapshot = _monitor;
            var helperSnapshot = _helper;

            // Declare the rollback tracking variable at method scope so it's accessible in finally block
            var mayStillBeSubscribed = false;

            try
            {
                // Capture GameLoop once for consistent unsubscribe
                var gameLoop = helperSnapshot?.Events?.GameLoop;
                if (gameLoop == null)
                {
                    mayStillBeSubscribed = eventUnregisterContext.WasRegistered || eventUnregisterContext.HasHandlers;
                    monitorSnapshot.Log("Helper or Events or GameLoop is null, cannot unregister events.", LogLevel.Warn);

                    // If we're disposing, clear handler references to avoid leaks even if we
                    // can't detach from SMAPI events.
                    if (IsDisposed())
                    {
                        System.Threading.Interlocked.Exchange(ref _onGameLaunchedHandler, null);
                        System.Threading.Interlocked.Exchange(ref _onSaveLoadedHandler, null);
                        System.Threading.Interlocked.Exchange(ref _onSavingHandler, null);
                        mayStillBeSubscribed = false;
                    }

                    return;
                }

                var unsubscribeResults = AttemptUnregistration(monitorSnapshot, gameLoop, eventUnregisterContext);

                var allUnsubscribed = unsubscribeResults.AllUnsubscribed;

                // Subscription state should be derived from unsubscribe results, not "had handlers".
                mayStillBeSubscribed = !allUnsubscribed;

                // Only nullify handler fields after successful unsubscription to prevent memory leaks
                // and inconsistent state. Use CompareExchange to ensure we only nullify if the
                // handler hasn't changed since we captured it.
                if (unsubscribeResults.GameLaunchedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onGameLaunchedHandler, null, eventUnregisterContext.GameLaunchedHandler);
                if (unsubscribeResults.SaveLoadedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onSaveLoadedHandler, null, eventUnregisterContext.SaveLoadedHandler);
                if (unsubscribeResults.SavingRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onSavingHandler, null, eventUnregisterContext.SavingHandler);

                HandleUnregistrationResult(monitorSnapshot, gameLoop, unsubscribeResults, eventUnregisterContext, allUnsubscribed, ref mayStillBeSubscribed);
            }
            finally
            {
                RestoreStateAfterUnregistration(mayStillBeSubscribed);
            }
        }

        private void HandleUnregistrationResult(IMonitor monitor, IGameLoopEvents gameLoop, UnsubscribeResults unsubscribeResults, EventUnregisterContext eventUnregisterContext, bool allUnsubscribed, ref bool mayStillBeSubscribed)
        {
            if (allUnsubscribed)
            {
                monitor.Log("Events unregistered successfully.", LogLevel.Trace);
            }
            else
            {
                monitor.Log("Event unregistration partially failed. Some handlers may remain subscribed.", LogLevel.Warn);

                // Check if the controller is disposed - if so, skip rollback to prevent resource leaks
                if (IsDisposed())
                {
                    monitor.Log("Controller is disposed, skipping rollback of event handlers.", LogLevel.Trace);
                    // When disposed, set mayStillBeSubscribed to false to indicate that cleanup is best-effort only
                    mayStillBeSubscribed = false;
                }
                else
                {
                    // IMPLEMENT ROLLBACK MECHANISM: If any unsubscription fails, re-subscribe the successfully removed handlers
                    // This ensures an all-or-nothing operation to maintain state consistency
                    var rollbackContext = new RollbackContext
                    {
                        Monitor = monitor,
                        GameLoop = gameLoop,
                        GameLaunchedRemoved = unsubscribeResults.GameLaunchedRemoved,
                        GameLaunchedHandler = eventUnregisterContext.GameLaunchedHandler,
                        SaveLoadedRemoved = unsubscribeResults.SaveLoadedRemoved,
                        SaveLoadedHandler = eventUnregisterContext.SaveLoadedHandler,
                        SavingRemoved = unsubscribeResults.SavingRemoved,
                        SavingHandler = eventUnregisterContext.SavingHandler
                    };
                    var rollbackSucceeded = ExecuteRollback(rollbackContext);

                    if (rollbackSucceeded)
                    {
                        // Restore the EventsRegisteredFlag since state is restored to the registered state
                        System.Threading.Interlocked.Or(ref _state, EventsRegisteredFlag);
                        monitor.Log("Rollback completed successfully. State restored to registered.", LogLevel.Warn);
                    }
                    else
                    {
                        monitor.Log("Rollback partially failed. State may be inconsistent.", LogLevel.Error);
                    }
                }
            }
        }

        private UnsubscribeResults AttemptUnregistration(IMonitor monitor, IGameLoopEvents gameLoop, EventUnregisterContext eventUnregisterContext)
        {
            var unsubscribeResults = PerformUnsubscribes(monitor, gameLoop, eventUnregisterContext);
            return unsubscribeResults;
        }

        private EventUnregisterContext CreateUnregisterContext()
        {
            var gameLaunchedHandler = System.Threading.Volatile.Read(ref _onGameLaunchedHandler);
            var saveLoadedHandler = System.Threading.Volatile.Read(ref _onSaveLoadedHandler);
            var savingHandler = System.Threading.Volatile.Read(ref _onSavingHandler);
            var currentState = System.Threading.Volatile.Read(ref _state);
            var wasRegistered = (currentState & EventsRegisteredFlag) != 0;

            // Check if any handlers are non-null for best-effort cleanup
            var hasHandlers = gameLaunchedHandler != null || saveLoadedHandler != null || savingHandler != null;

            return new EventUnregisterContext
            {
                GameLaunchedHandler = gameLaunchedHandler,
                SaveLoadedHandler = saveLoadedHandler,
                SavingHandler = savingHandler,
                WasRegistered = wasRegistered,
                HasHandlers = hasHandlers
            };
        }

        private UnsubscribeResults PerformUnsubscribes(IMonitor monitor, IGameLoopEvents gameLoop, EventUnregisterContext context)
        {
            // Track which events were successfully removed for rollback
            var gameLaunchedRemoved = SafeUnsubscribe<GameLaunchedEventArgs>(monitor, h => gameLoop.GameLaunched -= h, context.GameLaunchedHandler, "GameLaunched");
            var saveLoadedRemoved = SafeUnsubscribe<SaveLoadedEventArgs>(monitor, h => gameLoop.SaveLoaded -= h, context.SaveLoadedHandler, "SaveLoaded");
            var savingRemoved = SafeUnsubscribe<SavingEventArgs>(monitor, h => gameLoop.Saving -= h, context.SavingHandler, "Saving");

            // Be conservative: if we thought we were registered but lost handler references, assume we may still be subscribed.
            var missingHandlerWhileRegistered =
                context.WasRegistered &&
                (context.GameLaunchedHandler == null || context.SaveLoadedHandler == null || context.SavingHandler == null);

            // Handle wedged registered state: log warning but don't clear handler fields
            // This prevents resource leaks and potential crashes from callbacks on disposed objects
            if (missingHandlerWhileRegistered)
            {
                monitor.Log("Event handlers are missing despite being registered. Assuming subscriptions may still exist to prevent resource leaks.", LogLevel.Warn);
            }

            var allUnsubscribed =
                !missingHandlerWhileRegistered &&
                (context.GameLaunchedHandler == null || gameLaunchedRemoved) &&
                (context.SaveLoadedHandler == null || saveLoadedRemoved) &&
                (context.SavingHandler == null || savingRemoved);

            return new UnsubscribeResults
            {
                GameLaunchedRemoved = gameLaunchedRemoved,
                SaveLoadedRemoved = saveLoadedRemoved,
                SavingRemoved = savingRemoved,
                AllUnsubscribed = allUnsubscribed
            };
        }

        private bool ExecuteRollback(RollbackContext ctx)
        {
            return ExecuteRollbackInternal(ctx);
        }

        private bool ExecuteRollbackInternal(RollbackContext ctx)
        {
            var rollbackSucceeded = true;

            // Step 3: Implement rollback logic for GameLaunched handler
            // If gameLaunchedRemoved is true and handler exists, try to re-subscribe
            if (ctx.GameLaunchedRemoved && ctx.GameLaunchedHandler != null)
            {
                try
                {
                    ctx.GameLoop.GameLaunched += ctx.GameLaunchedHandler;
                    System.Threading.Interlocked.CompareExchange(ref _onGameLaunchedHandler, ctx.GameLaunchedHandler, null);
                    ctx.Monitor.Log("GameLaunched handler re-subscribed during rollback.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    ctx.Monitor.Log("Failed to re-subscribe GameLaunched handler during rollback.", LogLevel.Error);
                    ctx.Monitor.Log($"Rollback exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    rollbackSucceeded = false;
                }
            }

            // Step 4: Implement rollback logic for SaveLoaded handler
            // If saveLoadedRemoved is true and handler exists, try to re-subscribe
            if (ctx.SaveLoadedRemoved && ctx.SaveLoadedHandler != null)
            {
                try
                {
                    ctx.GameLoop.SaveLoaded += ctx.SaveLoadedHandler;
                    System.Threading.Interlocked.CompareExchange(ref _onSaveLoadedHandler, ctx.SaveLoadedHandler, null);
                    ctx.Monitor.Log("SaveLoaded handler re-subscribed during rollback.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    ctx.Monitor.Log("Failed to re-subscribe SaveLoaded handler during rollback.", LogLevel.Error);
                    ctx.Monitor.Log($"Rollback exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    rollbackSucceeded = false;
                }
            }

            // Step 5: Implement rollback logic for Saving handler
            // If savingRemoved is true and handler exists, try to re-subscribe
            if (ctx.SavingRemoved && ctx.SavingHandler != null)
            {
                try
                {
                    ctx.GameLoop.Saving += ctx.SavingHandler;
                    System.Threading.Interlocked.CompareExchange(ref _onSavingHandler, ctx.SavingHandler, null);
                    ctx.Monitor.Log("Saving handler re-subscribed during rollback.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    ctx.Monitor.Log("Failed to re-subscribe Saving handler during rollback.", LogLevel.Error);
                    ctx.Monitor.Log($"Rollback exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    rollbackSucceeded = false;
                }
            }

            return rollbackSucceeded;
        }

        private bool ShouldAttemptUnregister()
        {
            if ((System.Threading.Volatile.Read(ref _state) & (EventsRegisteredFlag | UnregisteringFlag)) == 0)
            {
                // Check if any handlers are non-null for best-effort cleanup
                var earlyGameLaunchedHandler = System.Threading.Volatile.Read(ref _onGameLaunchedHandler);
                var earlySaveLoadedHandler = System.Threading.Volatile.Read(ref _onSaveLoadedHandler);
                var earlySavingHandler = System.Threading.Volatile.Read(ref _onSavingHandler);

                var earlyHasHandlers = earlyGameLaunchedHandler != null || earlySaveLoadedHandler != null || earlySavingHandler != null;

                if (!earlyHasHandlers)
                {
                    _monitor.Log("Events were not registered or already unregistered, skipping unregistration.", LogLevel.Trace);
                    return false;
                }
            }
            return true;
        }

        private bool TryEnterUnregisteringState()
        {
            int currentState, newState;
            while (true)
            {
                currentState = System.Threading.Volatile.Read(ref _state);

                // Avoid racing a registration in progress; otherwise state can become inconsistent.
                if ((currentState & UnregisteringFlag) != 0)
                {
                    _monitor.Log("Event unregistration already in progress, skipping.", LogLevel.Trace);
                    return false;
                }

                var isDisposing = (currentState & DisposedFlag) != 0;

                // If disposing, prefer cleanup over avoiding the race with registration.
                if (!isDisposing && (currentState & RegisteringFlag) != 0)
                {
                    _monitor.Log("Event registration in progress, skipping unregistration.", LogLevel.Trace);
                    return false;
                }

                // Claim unregistration; when disposing, also clear RegisteringFlag to avoid deadlocks.
                newState = (currentState | UnregisteringFlag) & ~EventsRegisteredFlag;
                if (isDisposing)
                    newState &= ~RegisteringFlag;

                if (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                    return true;
            }
        }

        private void RestoreStateAfterUnregistration(bool mayStillBeSubscribed)
        {
            // Step 6: Implement state restoration in finally block
            // Update event-registration flags to maintain consistent state.
            // The EventsRegisteredFlag was cleared at the start of unregistration.
            // If unregistration was incomplete, restore the flag to indicate handlers
            // are still subscribed, preventing duplicate subscriptions
            if (IsDisposed())
            {
                // During disposal, force-clear all lifecycle flags.
                System.Threading.Interlocked.And(ref _state, ~(EventsRegisteredFlag | CommandRegisteredFlag));
            }
            else if (mayStillBeSubscribed)
            {
                // If mayStillBeSubscribed is true, restore EventsRegisteredFlag to indicate
                // that handlers are still subscribed and prevent duplicate subscriptions
                System.Threading.Interlocked.Or(ref _state, EventsRegisteredFlag);
            }
            // If unregistration was complete and successful, the EventsRegisteredFlag remains cleared
            // (as set at the start of the method)

            // Clear "unregistering" claim last, once state is consistent.
            System.Threading.Interlocked.And(ref _state, ~UnregisteringFlag);
        }

        /// <summary>
        /// Safely unsubscribes from an event with individual exception handling to prevent one failure from affecting others.
        /// </summary>
        /// <typeparam name="T">The event args type</typeparam>
        /// <param name="unsubscribeAction">The action to perform the unsubscription</param>
        /// <param name="handler">The event handler to unsubscribe</param>
        /// <param name="eventName">The name of the event for logging purposes</param>
        /// <returns>True if unsubscription was attempted and succeeded, false if unsubscription failed</returns>

        private static bool SafeUnsubscribe<T>(IMonitor monitor, Action<EventHandler<T>> unsubscribeAction, EventHandler<T>? handler, string eventName) where T : EventArgs
        {
            // No handler reference means there's nothing to detach from our perspective.
            // Treat as success to avoid incorrectly restoring "registered" state.
            if (handler == null)
            {
                return true;
            }

            try
            {
                unsubscribeAction(handler);
                return true;
            }
            catch (Exception ex)
            {
                monitor.Log($"Error occurred while unsubscribing from {eventName} event.", LogLevel.Error);
                monitor.Log($"{eventName} unsubscription exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);

#if DEBUG
                monitor.Log(ex.StackTrace ?? $"{eventName} unsubscription stack trace unavailable.", LogLevel.Trace);
#endif

                return false;
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
                var displayName = string.IsNullOrWhiteSpace(_manifest.Name)
                    ? (_manifest.UniqueID ?? "Living Roots")
                    : _manifest.Name;

                _monitor.Log($"The '{displayName}' mod was loaded successfully!", LogLevel.Info);

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
            ExecuteWithConcurrencyGuard(OnSaveLoadedExecutingFlag, "OnSaveLoaded", () =>
            {
                // Get the save ID using the abstraction (monitor is already available in the provider)
                var saveId = _saveIdProvider.GetSaveId();

                if (string.IsNullOrWhiteSpace(saveId))
                {
                    if (System.Threading.Interlocked.CompareExchange(ref _saveIdUnavailableWarningShownOnSaveLoaded, 1, 0) == 0)
                    {
                        _monitor.Log("OnSaveLoaded: SaveFolderName unavailable; skipping load to prevent cross-save data leakage.", LogLevel.Warn);
                    }
                    return;
                }

                // Reset the warning flag when a valid save ID is found
                System.Threading.Interlocked.Exchange(ref _saveIdUnavailableWarningShownOnSaveLoaded, 0);

                // Load data using the save folder name as unique ID
                _soilHealthService.LoadData(saveId);
                _monitor.Log("Soil health data loaded successfully.", LogLevel.Trace);
            });
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            ExecuteWithConcurrencyGuard(OnSavingExecutingFlag, "OnSaving", () =>
            {
                // Get the save ID using the abstraction (monitor is already available in the provider)
                var saveId = _saveIdProvider.GetSaveId();

                if (string.IsNullOrWhiteSpace(saveId))
                {
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
            });
        }

        /// <summary>
        /// Executes the provided action with a concurrency guard using the specified flag
        /// </summary>
        /// <param name="flag">The flag to use for the concurrency guard</param>
        /// <param name="eventName">The name of the event for logging purposes</param>
        /// <param name="action">The action to execute</param>
        private void ExecuteWithConcurrencyGuard(int flag, string eventName, Action action)
        {
            // Use Interlocked.CompareExchange to implement a thread-safe re-entrancy guard
            // This ensures only one execution of the event handler can happen at a time
            int currentState, newState;
            do
            {
                currentState = System.Threading.Volatile.Read(ref _state);

                // If the flag is already set, this method is already running, so return
                if ((currentState & flag) != 0)
                {
                    _monitor.Log($"{eventName} is already executing, skipping concurrent execution.", LogLevel.Trace);
                    return;
                }

                // Calculate new state with the flag set
                newState = currentState | flag;
            }
            while (System.Threading.Interlocked.CompareExchange(ref _state, newState, currentState) != currentState);

            try
            {
                // Double-check locking pattern: check if controller is disposed after acquiring execution flag
                if (IsDisposed())
                {
                    _monitor.Log($"Controller disposed after acquiring {eventName} execution flag, skipping execution.", LogLevel.Trace);
                    return;
                }

                // Execute the provided action
                action();
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log($"Error occurred while executing {eventName} event handler.", LogLevel.Error);

                // Add trace-level exception details for debugging
                _monitor.Log($"{eventName} exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);

                // Add stack trace logging for better diagnostics without exposing sensitive information
#if DEBUG
                _monitor.Log(ex.StackTrace ?? $"{eventName} stack trace unavailable.", LogLevel.Trace);
#endif
            }
            finally
            {
                // Clear the executing flag when done to allow future executions
                System.Threading.Interlocked.And(ref _state, unchecked(~flag));
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
            var monitorSnapshot = _monitor;
            var manifestSnapshot = _manifest;

            try
            {
                // Add null check for args parameter and use case-insensitive comparison
                args = args ?? Array.Empty<string>();

                // Check if any non-whitespace argument matches a help flag using LINQ Any()
                // Trim arguments before matching to handle cases like " /help " or "-help "
                if (args.Any(arg => !string.IsNullOrWhiteSpace(arg) && HelpFlags.Contains(arg.Trim())))
                {
                    monitorSnapshot?.Log("Usage: lr_version", LogLevel.Info);
                    monitorSnapshot?.Log("Shows the Living Roots version and UniqueID.", LogLevel.Info);
                    return;
                }

                // Use the standard version.ToString() method which provides consistent output
                var version = manifestSnapshot?.Version;
                var versionString = version?.ToString() ?? "unknown";

                monitorSnapshot?.Log($"Living Roots Mod Version: {versionString} (UniqueID: {manifestSnapshot?.UniqueID ?? "unknown"})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                monitorSnapshot?.Log("Error occurred while executing version command.", LogLevel.Error);

                // Add trace level exception details for debugging
                monitorSnapshot?.Log($"PrintVersion exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);

                // Add stack trace logging for better diagnostics without exposing sensitive information
#if DEBUG
                monitorSnapshot?.Log(ex.StackTrace ?? "PrintVersion stack trace unavailable.", LogLevel.Trace);
#endif
            }
        }

        public void Dispose()
        {
            // First, atomically mark as disposed to prevent any new work from starting.
            if (!TrySetStateFlag(DisposedFlag))
                return;

            // Now clear any in-flight transient claims so cleanup isn't blocked.
            System.Threading.Interlocked.And(ref _state, ~RegisteringFlag);

            // If we reach this point, we successfully set the disposed flag and can proceed with cleanup
            _monitor.Log("Controller disposed successfully.", LogLevel.Trace);

            // Best-effort cleanup
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

    /// <summary>
    /// Context class to encapsulate parameters for HandleRegistrationError method
    /// to reduce the number of parameters and improve maintainability
    /// </summary>
    internal readonly struct RegistrationContext
    {
        public Exception Exception { get; init; }
        public IGameLoopEvents GameLoop { get; init; }
        public EventHandler<GameLaunchedEventArgs>? LocalGameLaunchedHandler { get; init; }
        public EventHandler<SaveLoadedEventArgs>? LocalSaveLoadedHandler { get; init; }
        public EventHandler<SavingEventArgs>? LocalSavingHandler { get; init; }
        public IMonitor Monitor { get; init; }
    }

    /// <summary>
    /// Context class to encapsulate parameters for event unregistration
    /// </summary>
    internal readonly struct EventUnregisterContext
    {
        public EventHandler<GameLaunchedEventArgs>? GameLaunchedHandler { get; init; }
        public EventHandler<SaveLoadedEventArgs>? SaveLoadedHandler { get; init; }
        public EventHandler<SavingEventArgs>? SavingHandler { get; init; }
        public bool WasRegistered { get; init; }
        public bool HasHandlers { get; init; }
    }

    /// <summary>
    /// Context class to encapsulate results of unsubscribe operations
    /// </summary>
    internal readonly struct UnsubscribeResults
    {
        public bool GameLaunchedRemoved { get; init; }
        public bool SaveLoadedRemoved { get; init; }
        public bool SavingRemoved { get; init; }
        public bool AllUnsubscribed { get; init; }
    }

    /// <summary>
    /// Context class to encapsulate parameters for rollback operations
    /// </summary>
    internal readonly struct RollbackContext
    {
        public IMonitor Monitor { get; init; }
        public IGameLoopEvents GameLoop { get; init; }
        public bool GameLaunchedRemoved { get; init; }
        public EventHandler<GameLaunchedEventArgs>? GameLaunchedHandler { get; init; }
        public bool SaveLoadedRemoved { get; init; }
        public EventHandler<SaveLoadedEventArgs>? SaveLoadedHandler { get; init; }
        public bool SavingRemoved { get; init; }
        public EventHandler<SavingEventArgs>? SavingHandler { get; init; }
    }
}
