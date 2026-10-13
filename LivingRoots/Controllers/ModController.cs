using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Domain.Services;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Controllers
{
    public sealed class ModController(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ISoilHealthService soilHealthService,
        ISaveIdProvider saveIdProvider,
        ICompostingBinService compostingBinService,
        ISoilDecayService soilDecayService,
        ICompostApplicationService? compostApplicationService = null,
        IVisualizationService? visualizationService = null,
        IVisualizationConfigurationService? visualizationConfigService = null,
        IHostAtomicLockService? hostAtomicLockService = null,
        IPlayerProvider? playerProvider = null,
        IColorInterpolationService? colorInterpolationService = null) : IDisposable
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
        private readonly ICompostingBinService _compostingBinService = compostingBinService ?? throw new ArgumentNullException(nameof(compostingBinService));
        private readonly ISoilDecayService _soilDecayService = soilDecayService ?? throw new ArgumentNullException(nameof(soilDecayService));
        private readonly ICompostApplicationService? _compostApplicationService = compostApplicationService;
        private readonly IVisualizationService? _visualizationService = visualizationService;
        private readonly IVisualizationConfigurationService? _visualizationConfigService = visualizationConfigService;

        // T082 (F3, FR-003, US3/AC4): the held-item source of truth.
        // The SAME IPlayerProvider.CurrentItem accessor
        // CompostApplicationService consumes — no second
        // held-item accessor is forked here. Null (not
        // injected) carries no held-item information,
        // which counts as "no hoe equipped".
        private readonly IPlayerProvider? _playerProvider = playerProvider;

        // T069 (FR-004): the color interpolation seam the
        // configuration's custom category palette reaches
        // through (SetCategoryColors) when the render frame
        // observes a configuration change. Null (not
        // injected) skips the palette application — the
        // interpolator keeps its ModConstants defaults —
        // and no other behavior depends on it.
        private readonly IColorInterpolationService? _colorInterpolationService = colorInterpolationService;

        // T069: the configuration instance the render frame
        // last applied. The configuration service swaps the
        // active instance on every change (load, in-game
        // update, reset), so reference inequality against
        // this field is the change detector — a field-level
        // comparison is neither needed nor correct (a
        // partial application is the mixed-state condition
        // FR-019 forbids).
        private VisualizationConfiguration? _lastAppliedVisualizationConfiguration;

        // T140 disposition (retained, post-T069/T083): this
        // flag is NOT the read gate — the read gate is the
        // service-side cache-coordination flag
        // (VisualizationConfigurationService.
        // _configurationCacheInvalidated, T083), which
        // NotifyVisualizationConfigurationChanged flips
        // through the concrete-service forward below. What
        // this flag records is that a change was signalled
        // out of band, so the render frame logs the
        // application and so a failed reload is retried on
        // the following frame rather than silently dropped.
        // Volatile because it is written from the SMAPI
        // event thread (NotifyVisualizationConfiguration-
        // Changed) and read from the render thread. See
        // ReloadVisualizationConfigurationIfChanged for the
        // per-frame poll that covers changes nobody
        // signalled.
        //
        // T140 disposition (wave 2d): the internal
        // InvalidateVisualizationConfigurationCache
        // writer was REMOVED — zero callers post-T069
        // (grep-verified: only its own definition
        // matched). The render frame reloads
        // unconditionally through
        // ReloadVisualizationConfigurationIfChanged,
        // and the out-of-band path is
        // NotifyVisualizationConfigurationChanged's
        // concrete-service forward, so the method could
        // only ever duplicate the flag the public
        // signal already sets; nothing gated a read on
        // the controller flag.
        private volatile bool _visualizationConfigDirty;

        // T081b (Constitution V, FR-019): the composition root
        // (ModEntry) creates ONE HostAtomicLockService and injects
        // the same instance here and into CompostingBinService, so
        // host-authoritative tile locks are a single registry across
        // the mod. No `?? new` fallback: silently materialising a
        // second, disjoint lock registry here is the T055 hazard —
        // a tile this controller locked would look free to the bin
        // service, and a tile the bin service locked would look
        // free here. A headless double that constructs the
        // controller without one gets a null service, whose
        // treatment T106 splits by blast radius:
        // TryInteractWithBin stays permissive ("no contention
        // to resolve" — acquisition trivially succeeds, release
        // is a no-op, never a registry of its own) because it
        // only picks conflict messaging and ModControllerTests
        // pins that; TryApplyCompostWithLock — the path that
        // actually mutates bin and soil state — fails closed
        // instead, refusing rather than guessing under a write
        // it cannot arbitrate.
        private readonly IHostAtomicLockService? _hostAtomicLockService = hostAtomicLockService;

        // Event handlers - stored as fields to enable proper unsubscription
        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;
        private EventHandler<SaveLoadedEventArgs>? _onSaveLoadedHandler;
        private EventHandler<SavingEventArgs>? _onSavingHandler;
        private EventHandler<DayStartedEventArgs>? _onDayStartedHandler;
        private EventHandler<ButtonPressedEventArgs>? _onButtonPressedHandler;
        private EventHandler<RenderedWorldEventArgs>? _onRenderedWorldHandler;
        private EventHandler<ButtonReleasedEventArgs>? _onButtonReleasedHandler;
        private EventHandler<UpdateTickedEventArgs>? _onUpdateTickedHandler;
        private EventHandler<MenuChangedEventArgs>? _onMenuChangedHandler;
        private EventHandler<RenderedEventArgs>? _onRenderedHandler;
        private EventHandler<SavedEventArgs>? _onSavedHandler;
        // T100 (FR-002): the world object-list
        // change handler, stored as a field so
        // UnregisterEvents detaches the exact
        // delegate instance on disposal (FR-012)
        // — a method-group subscription has no
        // readable handler reference and would
        // leak on every disposal.
        private EventHandler<ObjectListChangedEventArgs>? _onObjectListChangedHandler;

        // GameTime captured from UpdateTicked for use in render events
        private GameTime _lastGameTime = new();

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
            EventHandler<DayStartedEventArgs>? localDayStartedHandler = null;
            EventHandler<RenderedWorldEventArgs>? localRenderedWorldHandler = null;
            EventHandler<ButtonReleasedEventArgs>? localButtonReleasedHandler = null;
            EventHandler<ButtonPressedEventArgs>? localButtonPressedHandler = null;
            EventHandler<MenuChangedEventArgs>? localMenuChangedHandler = null;
            EventHandler<RenderedEventArgs>? localRenderedHandler = null;
            EventHandler<SavedEventArgs>? localSavedHandler = null;
            EventHandler<ObjectListChangedEventArgs>? localObjectListChangedHandler = null;

            try
            {
                // Initialize the handlers once and assign to local references
                localGameLaunchedHandler = _onGameLaunchedHandler ??= OnGameLaunched;
                localSaveLoadedHandler = _onSaveLoadedHandler ??= OnSaveLoaded;
                localSavingHandler = _onSavingHandler ??= OnSaving;
                localDayStartedHandler = _onDayStartedHandler ??= OnDayStarted;

                // Double-check disposed state after setting the flag but before subscribing to prevent race condition
                if (IsDisposed())
                {
                    monitorSnapshot.Log("Controller disposed during registration. Skipping event subscription.", LogLevel.Trace);
                    // Clear the flag since we didn't actually register anything
                    System.Threading.Interlocked.And(ref _state, ~(EventsRegisteredFlag));
                    return;
                }

                // Subscribe to events. ButtonPressed goes through the
                // stored handler field rather than a method group so
                // UnregisterEvents can detach the exact delegate
                // instance on disposal (FR-012): a method-group
                // subscription has no readable handler reference and
                // would leak on every disposal.
                localButtonPressedHandler = _onButtonPressedHandler ??= OnButtonPressed;
                gameLoop.GameLaunched += localGameLaunchedHandler;
                gameLoop.SaveLoaded += localSaveLoadedHandler;
                gameLoop.Saving += localSavingHandler;
                gameLoop.DayStarted += localDayStartedHandler;
                _helper.Events.Input.ButtonPressed += localButtonPressedHandler;

                // T100 (FR-002): subscribe to the
                // world object-list change event so a
                // bin the game itself removes — a tool
                // hit (axe, pickaxe, hoe) or an
                // environmental explosion — prunes the
                // live bin cache in the same frame,
                // without a save reload. The right-
                // click dislodge path already prunes
                // through TryDislodgeBin; this covers
                // the game-driven removals that path
                // never sees. The world event source is
                // optional (a host without it leaves
                // Events.World null), so the
                // subscription is guarded — the
                // placement and dislodge paths above
                // do not depend on it.
                var worldEvents = helperSnapshot?.Events?.World;
                if (worldEvents != null)
                {
                    localObjectListChangedHandler = _onObjectListChangedHandler ??= OnObjectListChanged;
                    worldEvents.ObjectListChanged += localObjectListChangedHandler;
                }

                // Subscribe to visualization events if services are available
                if (_visualizationService != null)
                {
                    localRenderedWorldHandler = _onRenderedWorldHandler ??= OnRenderedWorld;
                    localButtonReleasedHandler = _onButtonReleasedHandler ??= OnButtonReleased;
                    _helper.Events.Display.RenderedWorld += localRenderedWorldHandler;
                    _helper.Events.Input.ButtonReleased += localButtonReleasedHandler;

                    _onUpdateTickedHandler = OnUpdateTicked;
                    gameLoop.UpdateTicked += _onUpdateTickedHandler;

                    // T126 (FR-025, DC-4): pause/resume rendering
                    // around in-game menus. MenuChanged rides the
                    // display event source alongside RenderedWorld.
                    localMenuChangedHandler = _onMenuChangedHandler ??= OnMenuChanged;
                    _helper.Events.Display.MenuChanged += localMenuChangedHandler;

                    // T132 (FR-024, DC-3): the screen-space half
                    // of the render frame (tooltip, hoe floating
                    // text) renders on Display.Rendered, after the
                    // world-space frame above.
                    localRenderedHandler = _onRenderedHandler ??= OnRendered;
                    _helper.Events.Display.Rendered += localRenderedHandler;

                    // T138 (FR-017): the save window runs from
                    // Saving to Saved. OnSaving pauses the frame
                    // before the save; this subscription resumes it
                    // on the dispatch that reports the completed
                    // save, so the pause never outlives the save
                    // itself. The load path keeps its own boundary
                    // (OnSaveLoaded resumes after the refreshed
                    // data is loaded).
                    localSavedHandler = _onSavedHandler ??= OnSaved;
                    gameLoop.Saved += localSavedHandler;
                }

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
                    LocalDayStartedHandler = localDayStartedHandler,
                    LocalButtonPressedHandler = localButtonPressedHandler,
                    LocalRenderedWorldHandler = localRenderedWorldHandler,
                    LocalButtonReleasedHandler = localButtonReleasedHandler,
                    LocalMenuChangedHandler = localMenuChangedHandler,
                    LocalRenderedHandler = localRenderedHandler,
                    LocalSavedHandler = localSavedHandler,
                    LocalObjectListChangedHandler = localObjectListChangedHandler,
                    Monitor = monitorSnapshot,
                    Helper = helperSnapshot
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

            SafeUnsubscribe<DayStartedEventArgs>(
                ctx.Monitor,
                h => ctx.GameLoop.DayStarted -= h,
                ctx.LocalDayStartedHandler,
                "DayStarted");

            SafeUnsubscribe<SavedEventArgs>(
                ctx.Monitor,
                h => ctx.GameLoop.Saved -= h,
                ctx.LocalSavedHandler,
                "Saved");

            if (ctx.Helper != null)
            {
                SafeUnsubscribe<ButtonPressedEventArgs>(
                    ctx.Monitor,
                    h => ctx.Helper.Events.Input.ButtonPressed -= h,
                    ctx.LocalButtonPressedHandler,
                    "ButtonPressed");

                SafeUnsubscribe<RenderedWorldEventArgs>(
                    ctx.Monitor,
                    h => ctx.Helper.Events.Display.RenderedWorld -= h,
                    ctx.LocalRenderedWorldHandler,
                    "RenderedWorld");

                SafeUnsubscribe<ButtonReleasedEventArgs>(
                    ctx.Monitor,
                    h => ctx.Helper.Events.Input.ButtonReleased -= h,
                    ctx.LocalButtonReleasedHandler,
                    "ButtonReleased");

                SafeUnsubscribe<MenuChangedEventArgs>(
                    ctx.Monitor,
                    h => ctx.Helper.Events.Display.MenuChanged -= h,
                    ctx.LocalMenuChangedHandler,
                    "MenuChanged");

                SafeUnsubscribe<RenderedEventArgs>(
                    ctx.Monitor,
                    h => ctx.Helper.Events.Display.Rendered -= h,
                    ctx.LocalRenderedHandler,
                    "Rendered");

                // T100 (FR-002): detach the
                // world object-list handler the
                // same way. The world event
                // source is optional (a host
                // without it leaves Events.World
                // null — the guarded subscription
                // in RegisterEvents), so the
                // detach is guarded identically;
                // a null handler is a no-op
                // (SafeUnsubscribe treats a null
                // handler as nothing to detach).
                var rollbackWorldEvents = ctx.Helper.Events?.World;
                if (rollbackWorldEvents != null)
                {
                    SafeUnsubscribe<ObjectListChangedEventArgs>(
                        ctx.Monitor,
                        h => rollbackWorldEvents.ObjectListChanged -= h,
                        ctx.LocalObjectListChangedHandler,
                        "ObjectListChanged");
                }
            }

            // Clear handler references to prevent memory leaks
            System.Threading.Interlocked.Exchange(ref _onGameLaunchedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onSaveLoadedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onSavingHandler, null);
            System.Threading.Interlocked.Exchange(ref _onDayStartedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onButtonPressedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onRenderedWorldHandler, null);
            System.Threading.Interlocked.Exchange(ref _onButtonReleasedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onUpdateTickedHandler, null);
            System.Threading.Interlocked.Exchange(ref _onObjectListChangedHandler, null);

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
                if (unsubscribeResults.UpdateTickedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onUpdateTickedHandler, null, eventUnregisterContext.UpdateTickedHandler);
                if (unsubscribeResults.MenuChangedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onMenuChangedHandler, null, eventUnregisterContext.MenuChangedHandler);
                if (unsubscribeResults.RenderedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onRenderedHandler, null, eventUnregisterContext.RenderedHandler);
                if (unsubscribeResults.SavedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onSavedHandler, null, eventUnregisterContext.SavedHandler);
                if (unsubscribeResults.ObjectListChangedRemoved)
                    System.Threading.Interlocked.CompareExchange(ref _onObjectListChangedHandler, null, eventUnregisterContext.ObjectListChangedHandler);

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
                        SavingHandler = eventUnregisterContext.SavingHandler,
                        DayStartedRemoved = unsubscribeResults.DayStartedRemoved,
                        DayStartedHandler = eventUnregisterContext.DayStartedHandler,
                        ButtonPressedRemoved = unsubscribeResults.ButtonPressedRemoved,
                        ButtonPressedHandler = eventUnregisterContext.ButtonPressedHandler
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
            var dayStartedHandler = System.Threading.Volatile.Read(ref _onDayStartedHandler);
            var buttonPressedHandler = System.Threading.Volatile.Read(ref _onButtonPressedHandler);
            var renderedWorldHandler = System.Threading.Volatile.Read(ref _onRenderedWorldHandler);
            var buttonReleasedHandler = System.Threading.Volatile.Read(ref _onButtonReleasedHandler);
            var updateTickedHandler = System.Threading.Volatile.Read(ref _onUpdateTickedHandler);
            var menuChangedHandler = System.Threading.Volatile.Read(ref _onMenuChangedHandler);
            var renderedHandler = System.Threading.Volatile.Read(ref _onRenderedHandler);
            var savedHandler = System.Threading.Volatile.Read(ref _onSavedHandler);
            var objectListChangedHandler = System.Threading.Volatile.Read(ref _onObjectListChangedHandler);
            var currentState = System.Threading.Volatile.Read(ref _state);
            var wasRegistered = (currentState & EventsRegisteredFlag) != 0;

            // Check if any handlers are non-null for best-effort cleanup
            var hasHandlers = gameLaunchedHandler != null || saveLoadedHandler != null || savingHandler != null
                || dayStartedHandler != null || buttonPressedHandler != null || renderedWorldHandler != null || buttonReleasedHandler != null || updateTickedHandler != null || menuChangedHandler != null || renderedHandler != null || savedHandler != null || objectListChangedHandler != null;

            return new EventUnregisterContext
            {
                GameLaunchedHandler = gameLaunchedHandler,
                SaveLoadedHandler = saveLoadedHandler,
                SavingHandler = savingHandler,
                DayStartedHandler = dayStartedHandler,
                ButtonPressedHandler = buttonPressedHandler,
                RenderedWorldHandler = renderedWorldHandler,
                ButtonReleasedHandler = buttonReleasedHandler,
                UpdateTickedHandler = updateTickedHandler,
                MenuChangedHandler = menuChangedHandler,
                RenderedHandler = renderedHandler,
                SavedHandler = savedHandler,
                ObjectListChangedHandler = objectListChangedHandler,
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
            var dayStartedRemoved = SafeUnsubscribe<DayStartedEventArgs>(monitor, h => gameLoop.DayStarted -= h, context.DayStartedHandler, "DayStarted");
            var updateTickedRemoved = SafeUnsubscribe<UpdateTickedEventArgs>(monitor, h => gameLoop.UpdateTicked -= h, context.UpdateTickedHandler, "UpdateTicked");
            var savedRemoved = SafeUnsubscribe<SavedEventArgs>(monitor, h => gameLoop.Saved -= h, context.SavedHandler, "Saved");

            // Unsubscribe from input/display events
            var buttonPressedRemoved = false;
            var renderedWorldRemoved = false;
            var buttonReleasedRemoved = false;
            var menuChangedRemoved = false;
            var renderedRemoved = false;
            // T100 (FR-002): the world object-list
            // detach flag lives at method scope —
            // the world event source is optional
            // (a host without it leaves Events.World
            // null, the guarded subscription in
            // RegisterEvents), so the flag stays
            // false (nothing detached) when the
            // source is absent, exactly like the
            // input/display flags.
            var objectListChangedRemoved = false;
            if (_helper != null)
            {
                buttonPressedRemoved = SafeUnsubscribe<ButtonPressedEventArgs>(monitor, h => _helper.Events.Input.ButtonPressed -= h, context.ButtonPressedHandler, "ButtonPressed");
                renderedWorldRemoved = SafeUnsubscribe<RenderedWorldEventArgs>(monitor, h => _helper.Events.Display.RenderedWorld -= h, context.RenderedWorldHandler, "RenderedWorld");
                buttonReleasedRemoved = SafeUnsubscribe<ButtonReleasedEventArgs>(monitor, h => _helper.Events.Input.ButtonReleased -= h, context.ButtonReleasedHandler, "ButtonReleased");
                menuChangedRemoved = SafeUnsubscribe<MenuChangedEventArgs>(monitor, h => _helper.Events.Display.MenuChanged -= h, context.MenuChangedHandler, "MenuChanged");
                renderedRemoved = SafeUnsubscribe<RenderedEventArgs>(monitor, h => _helper.Events.Display.Rendered -= h, context.RenderedHandler, "Rendered");

                // T100 (FR-002): detach the world
                // object-list handler. The detach
                // re-reads the world event source
                // (the guarded subscription in
                // RegisterEvents); a null handler
                // is a no-op (SafeUnsubscribe
                // treats a null handler as nothing
                // to detach, which keeps the
                // all-subscribed computation honest).
                var unregisterWorldEvents = _helper.Events?.World;
                if (unregisterWorldEvents != null)
                {
                    objectListChangedRemoved = SafeUnsubscribe<ObjectListChangedEventArgs>(monitor, h => unregisterWorldEvents.ObjectListChanged -= h, context.ObjectListChangedHandler, "ObjectListChanged");
                }
            }

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
                (context.SavingHandler == null || savingRemoved) &&
                (context.DayStartedHandler == null || dayStartedRemoved) &&
                (context.ButtonPressedHandler == null || buttonPressedRemoved) &&
                (context.RenderedWorldHandler == null || renderedWorldRemoved) &&
                (context.ButtonReleasedHandler == null || buttonReleasedRemoved) &&
                (context.UpdateTickedHandler == null || updateTickedRemoved) &&
                (context.MenuChangedHandler == null || menuChangedRemoved) &&
                (context.RenderedHandler == null || renderedRemoved) &&
                (context.SavedHandler == null || savedRemoved) &&
                (context.ObjectListChangedHandler == null || objectListChangedRemoved);

            return new UnsubscribeResults
            {
                GameLaunchedRemoved = gameLaunchedRemoved,
                SaveLoadedRemoved = saveLoadedRemoved,
                SavingRemoved = savingRemoved,
                DayStartedRemoved = dayStartedRemoved,
                ButtonPressedRemoved = buttonPressedRemoved,
                UpdateTickedRemoved = updateTickedRemoved,
                MenuChangedRemoved = menuChangedRemoved,
                RenderedRemoved = renderedRemoved,
                SavedRemoved = savedRemoved,
                ObjectListChangedRemoved = objectListChangedRemoved,
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

            // Step 6: Implement rollback logic for DayStarted handler
            // If dayStartedRemoved is true and handler exists, try to re-subscribe
            if (ctx.DayStartedRemoved && ctx.DayStartedHandler != null)
            {
                try
                {
                    ctx.GameLoop.DayStarted += ctx.DayStartedHandler;
                    System.Threading.Interlocked.CompareExchange(ref _onDayStartedHandler, ctx.DayStartedHandler, null);
                    ctx.Monitor.Log("DayStarted handler re-subscribed during rollback.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    ctx.Monitor.Log("Failed to re-subscribe DayStarted handler during rollback.", LogLevel.Error);
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

                // T069 (true-global configuration scope,
                // spec Session 2026-10-06, T027/T031): the
                // mod-folder configuration is loaded once
                // here, at initialization — not per save
                // (see OnSaveLoaded/OnSaving, which no
                // longer touch it). The render frame
                // re-reads through ReloadVisualization
                // Configuration, so a save ID that does
                // not exist yet at game launch simply
                // loads the defaults and the first frame
                // after a save loads completes the read
                // through the same path.
                ReloadVisualizationConfiguration();
            }
            catch (Exception ex)
            {
                // Log error but don't expose raw exception message for security
                _monitor.Log("Error occurred in game launched event handler.", LogLevel.Error);

                // Add trace-level exception details for debugging
                _monitor.Log($"OnGameLaunched exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
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
                // T056 (U4, FR-012, SC-004): bin state,
                // processing progress and batch provenance
                // ride the same save/load boundary as the
                // soil health cache — without this call the
                // bins reset to Empty on every reload.
                _compostingBinService.LoadData(saveId);
                // T069: the visualization configuration is
                // deliberately NOT loaded here — its scope
                // is the mod folder, shared by every save
                // (true-global per spec Session 2026-10-06,
                // T027/T031), so save transitions must not
                // load or save it. The render frame's
                // ReloadVisualizationConfiguration owns
                // the load (see OnGameLaunched for the
                // initialization load).
                _visualizationService?.ResumeRendering();
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

                // Pause rendering before save, save data, then resume after load
                _visualizationService?.PauseRendering();

                // Save data before the game saves/exits (using the saving event)
                _soilHealthService.SaveData(saveId);
                // T056 (U4, FR-012, SC-004): the bin
                // state travels with the soil health
                // cache through the same save boundary,
                // so a reload restores every placed
                // bin's state and batch progress.
                _compostingBinService.SaveData(saveId);
                // T069: the visualization configuration is
                // deliberately NOT saved here — its scope
                // is the mod folder, shared by every save
                // (true-global per spec Session 2026-10-06,
                // T027/T031). SaveConfiguration fires on
                // configuration change only, through
                // ApplyVisualizationConfigurationChange
                // (see ReloadVisualizationConfiguration).
                _monitor.Log("Soil health data saved successfully.", LogLevel.Trace);
            });
        }

        /// <summary>
        /// Resumes visualization rendering when the save the
        /// controller paused for completes (T138, FR-017).
        /// The spec bounds the save window from <c>Saving</c>
        /// to <c>Saved</c>: <see cref="OnSaving"/> pauses the
        /// frame before the game writes the save, and this
        /// handler resumes it on the very dispatch that
        /// reports the completed save, so the pause spans the
        /// save and nothing else — a render frame is never
        /// held back until the next <c>SaveLoaded</c> (which
        /// is the load path's own boundary, resuming after
        /// the refreshed data is loaded).
        /// </summary>
        private void OnSaved(object? sender, SavedEventArgs e)
        {
            if (IsDisposed() || _visualizationService == null) return;

            try
            {
                _visualizationService.ResumeRendering();
            }
            catch (Exception ex)
            {
                _monitor.Log("Error resuming rendering after save.", LogLevel.Warn);
                _monitor.Log($"Save completion exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (IsDisposed() || _visualizationService == null) return;

            try
            {
                // FR-005 hot-reload (FR-019, SC-003): an external
                // edit of the persisted configuration must take
                // effect before anything renders, and atomically.
                // T140 disposition: the rate limit this comment
                // once described (a ModConstants.
                // ConfigHotReloadPollIntervalMs that never existed
                // and was removed with it) is deliberately not
                // reintroduced — the poll is unconditional by
                // design, because a rate-limited poll breaks
                // FR-005 for exactly the unsignalled external
                // edit it exists to cover (the full rationale
                // and the known per-frame cost are on
                // ReloadVisualizationConfigurationIfChanged).
                // See ReloadVisualizationConfigurationIfChanged.
                ReloadVisualizationConfigurationIfChanged();

                // Supply the tile health snapshot before any texture-dependent
                // draw call so the overlay pipeline has data even when
                // rendering itself cannot proceed (FR-001, FR-010).
                UpdateTileHealthData();

                var whiteTexture = Services.Visualization.VisualizationTextures.WhiteTexture;
                if (whiteTexture == null)
                {
                    whiteTexture = new Microsoft.Xna.Framework.Graphics.Texture2D(e.SpriteBatch.GraphicsDevice, 1, 1);
                    whiteTexture.SetData(new[] { Microsoft.Xna.Framework.Color.White });
                    Services.Visualization.VisualizationTextures.WhiteTexture = whiteTexture;
                }

                // Install the shared white texture on the visualization
                // service before the first draw call (FR-001): until
                // Initialize has run, GetTexture throws on every frame and
                // no overlay renders at all.
                _visualizationService.Initialize(whiteTexture);

                // FR-016/FR-018: derive the tile-space viewport from the
                // live camera every frame, so a window resize or camera pan
                // changes the culled tile range within one frame. The map
                // bounds for the FR-018 clamp come from the active
                // location. Game1.currentLocation dereferences the Game1
                // singleton, which headless hosts (unit tests without the
                // game booted) leave unallocated, so the property throws
                // instead of returning null there — the same hazard
                // UpdateTileHealthData (T063) defends against. When the map
                // bounds cannot be read, none are supplied: only the
                // renderer's own origin clamp (T066) then applies,
                // which keeps culling correct, so the render frame
                // still proceeds.
                var mapWidthTiles = 0;
                var mapHeightTiles = 0;
                try
                {
                    var map = StardewValley.Game1.currentLocation?.map;
                    if (map != null)
                    {
                        mapWidthTiles = map.DisplayWidth / ModConstants.TileSize;
                        mapHeightTiles = map.DisplayHeight / ModConstants.TileSize;
                    }
                }
                catch (Exception ex)
                {
                    _monitor.Log("Map bounds unavailable for tile viewport derivation.", LogLevel.Warn);
                    _monitor.Log($"Map bounds exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                }

                // ComputeTileViewport is the pure pixel->tile derivation
                // seam (T059/T067): floor division of the pixel viewport by
                // ModConstants.TileSize, then the FR-018 one-tile margin and
                // the map-bounds clamp. It runs every frame so the derived
                // cull rectangle is recomputed as the camera moves (FR-016),
                // and its map-extent component feeds the clamp handed to the
                // renderer below. Game1.viewport is an xTile.Dimensions.
                // Rectangle, so it is copied into the
                // Microsoft.Xna.Framework.Rectangle the visualization pipeline
                // consumes (same X/Y/Width/Height fields).
                var pixelViewport = new Rectangle(
                    StardewValley.Game1.viewport.X,
                    StardewValley.Game1.viewport.Y,
                    StardewValley.Game1.viewport.Width,
                    StardewValley.Game1.viewport.Height);
                var tileViewport = ComputeTileViewport(
                    pixelViewport,
                    ModConstants.TileSize,
                    mapWidthTiles,
                    mapHeightTiles);
                var visibleTileViewport = new Microsoft.Xna.Framework.Rectangle(
                    pixelViewport.X / ModConstants.TileSize,
                    pixelViewport.Y / ModConstants.TileSize,
                    pixelViewport.Width / ModConstants.TileSize,
                    pixelViewport.Height / ModConstants.TileSize);

                // FR-018: ComputeTileViewport is the pure pixel->tile
                // derivation seam (FR-016 recomputes it every frame). Its
                // margin-extended result is intentionally NOT what reaches
                // the renderer: OverlayRenderer.ComputeCullingBounds already
                // applies the one-tile margin and the origin clamp itself,
                // so passing the extended rectangle would double-apply the
                // margin (effective +/-2 tiles) and shift DrawOverlay's
                // tile->screen conversion by a full tile. The renderer
                // therefore receives the raw visible tile rect above.
                //
                // The map-bounds clamp, though, is applied ONLY by
                // ComputeCullingBounds, and only when it is supplied - it
                // previously never was on the real render path, so the
                // FR-018 clamp ran solely behind the GetOverlays unit-test
                // seam. The map extent in TILE units is what belongs here,
                // matching the semantics ComputeCullingBounds assumes
                // (it clamps maxX to bounds.Right, i.e. a map extent).
                //
                // Supplying `tileViewport` instead was tried and is wrong:
                // ComputeCullingBounds is not idempotent for that input.
                // Exhaustively checked over 2835 combinations of map size,
                // pixel origin and pixel extent, it disagreed with the
                // intended rectangle in 693 of them - e.g. a 10x10-tile map
                // with the camera below it yields a bounds rect clamped to
                // Top=10, while the cull result comes back at Top=19, so
                // the renderer would cull against the wrong row band.
                //
                // Bounds are supplied only when the active location actually
                // exposed map dimensions; otherwise the derivation degenerates
                // to an empty rect, and empty bounds would cull every tile out
                // and blank the frame, so the renderer keeps its
                // origin-clamp-only behaviour. The IsEmpty guard covers the
                // same case geometrically: when the camera sits entirely
                // off-map, ComputeTileViewport already returns an empty rect
                // and there is nothing to cull against a map extent.
                Rectangle? mapBounds = null;
                if (mapWidthTiles > 0 && mapHeightTiles > 0 && !tileViewport.IsEmpty)
                {
                    mapBounds = new Rectangle(0, 0, mapWidthTiles, mapHeightTiles);
                }

                var gameTime = _lastGameTime;
                _visualizationService.RenderOverlays(e.SpriteBatch, visibleTileViewport, gameTime, mapBounds);
                _visualizationService.RenderHoeFeedback(e.SpriteBatch, gameTime);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in visualization rendering.", LogLevel.Warn);
                _monitor.Log($"Render exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Re-reads the persisted visualization configuration at the
        /// start of every render frame, so an external edit of the
        /// persisted store is applied within the next frame (FR-005
        /// hot-reload, FR-019, SC-003). The reload runs before any
        /// draw call, so the whole frame renders against one complete
        /// configuration snapshot: LoadConfiguration validates the
        /// entire persisted document and swaps the active
        /// configuration in a single assignment, which is what makes
        /// the application atomic — no frame can observe a mix of
        /// old and new fields. This is a per-frame poll rather than
        /// a FileSystemWatcher because the configuration is persisted
        /// per-save through IModDataService, whose contract exposes
        /// no modification time or content signature — a cheaper
        /// change detector would still have to perform the same
        /// storage read this method performs, and the payload is a
        /// single small validated document, negligible next to the
        /// overlay work the frame already does. A failed reload keeps
        /// the previously active configuration: the failure is logged
        /// (type and HResult only, per the log-security rule) and
        /// never allowed to skip the frame. The save ID is resolved
        /// per frame through ISaveIdProvider; when it is unavailable
        /// there is no save-scoped configuration to reload, so the
        /// frame keeps the active configuration — the same skip
        /// OnSaveLoaded applies.
        /// </summary>
        private void ReloadVisualizationConfiguration()
        {
            if (_visualizationConfigService == null)
            {
                return;
            }

            var saveId = _saveIdProvider.GetSaveId();
            if (string.IsNullOrWhiteSpace(saveId))
            {
                return;
            }

            try
            {
                _visualizationConfigService.LoadConfiguration(saveId);
                ApplyVisualizationConfigurationChange(saveId);
            }
            catch (Exception ex)
            {
                _visualizationConfigDirty = true;
                _monitor.Log("Visualization configuration hot-reload failed; keeping the active configuration.", LogLevel.Warn);
                _monitor.Log($"Configuration hot-reload exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Applies a configuration change the render frame just
        /// observed (T069, FR-004, FR-010). The configuration
        /// service swaps the active <see cref="VisualizationConfiguration"/>
        /// instance on every change — a persisted-file reload, an
        /// in-game update, a reset — so reference inequality
        /// against the last applied instance detects any of them,
        /// and detecting a change by comparing fields is neither
        /// needed nor correct (a partial application is the
        /// mixed-state condition FR-019 forbids). On a change:
        /// the new category palette — including the configured
        /// unknown color (T165, FR-003/FR-014) — reaches the
        /// color interpolator through <see cref="IColorInterpolationService.
        /// SetCategoryColors"/> (the FR-004 custom-color hook,
        /// which invalidates the interpolation cache so the next
        /// frame resolves through the new palette), and the
        /// validated configuration is persisted back to the
        /// mod-folder store — the only production
        /// <c>SaveConfiguration</c> call, so the configuration
        /// is saved on change only, never on a save event. The
        /// application runs inside the frame's reload, before
        /// any draw call, so the frame renders against one
        /// complete configuration snapshot.
        /// </summary>
        /// <param name="saveId">The identifier the configuration was loaded through.</param>
        private void ApplyVisualizationConfigurationChange(string saveId)
        {
            var active = _visualizationConfigService!.GetConfiguration();
            if (ReferenceEquals(active, _lastAppliedVisualizationConfiguration))
            {
                return;
            }

            _lastAppliedVisualizationConfiguration = active;

            _colorInterpolationService?.SetCategoryColors(
                ToColor(active.PoorColor),
                ToColor(active.ModerateColor),
                ToColor(active.HealthyColor),
                ToColor(active.UnknownColor));

            _visualizationConfigService.SaveConfiguration(saveId);
        }

        /// <summary>
        /// Converts a persisted configuration color (the
        /// serializable <see cref="ColorDTO"/>) to the XNA
        /// color the interpolator consumes.
        /// </summary>
        /// <param name="color">The persisted color.</param>
        /// <returns>The equivalent XNA color.</returns>
        private static Color ToColor(ColorDTO color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Entry point used by the render frame to apply a pending configuration
        /// change before anything renders (FR-005, FR-019, SC-003).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This reloads unconditionally rather than on an interval. FR-005
        /// requires an external edit to be applied "within the next render
        /// frame", and a change made outside the game cannot be signalled in
        /// advance, so time- or tick-based polling cannot satisfy it without
        /// adding a window in which the visible configuration is stale.
        /// </para>
        /// <para>
        /// Cost note (T083/T069, landed): the per-frame call is cheap by
        /// design. `LoadConfiguration` serves the in-memory validated cache
        /// without touching `IModDataService` unless the file-change signal
        /// was raised (`NotifyConfigurationChanged`), so unchanged frames
        /// perform no disk read inside the 16.67ms budget
        /// (ModConstants.MaxRenderTimeMs) — verified by
        /// `RenderedWorld_RepeatedFramesWithUnchangedPersistedConfig_PerformAtMostOneUnderlyingLoad`.
        /// A signaled external edit still applies within the next frame,
        /// satisfying FR-005 for the unsignalled case.
        /// </para>
        /// </remarks>
        private void ReloadVisualizationConfigurationIfChanged()
        {
            // A signalled change is always reloaded, and always cleared first so
            // a failed reload is retried on the following frame rather than
            // being silently dropped.
            var signalled = _visualizationConfigDirty;
            _visualizationConfigDirty = false;

            ReloadVisualizationConfiguration();

            if (signalled)
            {
                _monitor.Log("Visualization configuration change applied.", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Applies a configuration change that was signalled out of band, so the
        /// next rendered frame reflects it (FR-005, SC-003). This is the seam the
        /// SMAPI-side path uses; the render frame also polls, which is what covers
        /// an edit made while the game is running.
        /// </summary>
        public void NotifyVisualizationConfigurationChanged()
        {
            _visualizationConfigDirty = true;

            // T069/T083: forward the signal to the configuration
            // service itself, whose cache-coordination flag is
            // what makes the frame's LoadConfiguration re-read
            // the persisted store instead of serving the
            // in-memory cache. The seam is the concrete service
            // (the IVisualizationConfigurationService contract
            // predates the signal), so a decorator-wrapped
            // service is signalled only through the controller
            // flag the frame polls — the initial cache
            // invalidation covers the first frame regardless.
            if (_visualizationConfigService is VisualizationConfigurationService concreteConfigService)
            {
                concreteConfigService.NotifyConfigurationChanged();
            }
        }

        /// <summary>
        /// Supplies the visualization service with a fresh snapshot of soil
        /// health for every tilled tile of the active location (FR-001, FR-010).
        /// A tile is tilled when its terrain feature is a
        /// <see cref="StardewValley.TerrainFeatures.HoeDirt"/> — bare or
        /// crop-covered; every other feature (grass, objects, untilled soil)
        /// is excluded from the snapshot.
        /// Health values are read through the host-authoritative
        /// <see cref="ISoilHealthService"/> under the farm-scoped location name,
        /// so soil health stays a property of the farm rather than the player
        /// (Constitution VI) and no second copy of farm state is forked.
        /// The snapshot is best effort: <c>Game1.currentLocation</c> dereferences
        /// the <c>Game1</c> singleton, which headless hosts (unit tests without
        /// the game booted) leave unallocated, so the property throws instead of
        /// returning null there. When the active location cannot be read, the
        /// last snapshot is kept so the render pipeline still has data.
        /// When every FR-007 toggle is disabled the snapshot is skipped as a
        /// zero-allocation no-op instead (Q4): the snapshot feeds only the
        /// visualization's own derived work, never the producer (FR-022).
        /// </summary>
        private void UpdateTileHealthData()
        {
            // FR-007 (Q4): the per-frame tile-health snapshot exists solely to
            // feed the visualization's own derived work — overlays, tooltips,
            // and hoe feedback all consume it. When every toggle is disabled no
            // consumer remains, so the snapshot and its per-frame Dictionary
            // allocation are suppressed as an early-exit no-op. The gate keys on
            // all three toggles because the snapshot is shared by all three
            // features: it is skipped only when none of them will read it. The
            // SoilHealthService producer is never suppressed — soil health stays
            // host-authoritative and persisted (FR-022 read-only projection), so
            // re-enabling any toggle takes effect on the next render frame
            // (SC-003) with no restart, the configuration being re-read every
            // frame. A null configuration (unconfigured service) fails open and
            // keeps feeding.
            var config = _visualizationConfigService?.GetConfiguration();
            if (config is not null
                && !config.OverlaysEnabled
                && !config.TooltipsEnabled
                && !config.HoeFeedbackEnabled)
            {
                return;
            }

            StardewValley.GameLocation location;
            try
            {
                location = StardewValley.Game1.currentLocation;
            }
            catch (Exception ex)
            {
                _monitor.Log("Active location unavailable for visualization snapshot.", LogLevel.Warn);
                _monitor.Log($"Snapshot exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                return;
            }

            if (location == null || _visualizationService == null)
            {
                return;
            }

            var locationName = location.Name;
            var tileHealthData = new Dictionary<Microsoft.Xna.Framework.Point, float>();
            foreach (var terrainFeature in location.terrainFeatures.Pairs)
            {
                if (terrainFeature.Value is not StardewValley.TerrainFeatures.HoeDirt)
                {
                    continue;
                }

                var health = _soilHealthService.GetSoilHealth(locationName, terrainFeature.Key);
                tileHealthData[terrainFeature.Key.ToPoint()] = health;
            }

            _visualizationService.SetTileHealthData(tileHealthData);
        }

        /// <summary>
        /// Converts a pixel-space viewport (Game1.viewport) into the
        /// tile-space cull rectangle for the visualization pipeline
        /// (FR-016, FR-018). Pure function; no Game1 access inside.
        /// Conversion rule: integer floor division of each pixel bound
        /// by <paramref name="tileSize"/> (integer division floors for
        /// the non-negative pixel coordinates a Stardew viewport uses;
        /// 1280x720 px at 64 px/tile is 20x11 tiles, since
        /// 720/64 = 11.25 floors to 11), then the FR-018 one-tile
        /// margin on all four edges (start = raw - 1, end = raw +
        /// extent + 1), then a clamp to
        /// [0, <paramref name="mapWidthTiles"/>] x
        /// [0, <paramref name="mapHeightTiles"/>] with non-negative
        /// extents. Start bounds are capped at the map maximum as well
        /// so a viewport entirely past the map edge degenerates to an
        /// empty rectangle at the edge instead of escaping the map
        /// bounds, and end bounds are widened to <see cref="long"/>
        /// before the addition so extreme viewports cannot trip the
        /// project-wide overflow check.
        /// </summary>
        internal static Rectangle ComputeTileViewport(
            Rectangle pixelViewport,
            int tileSize,
            int mapWidthTiles,
            int mapHeightTiles)
        {
            var rawX = pixelViewport.X / tileSize;
            var rawY = pixelViewport.Y / tileSize;
            var rawWidth = pixelViewport.Width / tileSize;
            var rawHeight = pixelViewport.Height / tileSize;

            var startX = Math.Min(Math.Max(0, rawX - 1), mapWidthTiles);
            var startY = Math.Min(Math.Max(0, rawY - 1), mapHeightTiles);
            var endX = (int)Math.Min((long)rawX + rawWidth + 1, mapWidthTiles);
            var endY = (int)Math.Min((long)rawY + rawHeight + 1, mapHeightTiles);

            return new Rectangle(
                startX,
                startY,
                Math.Max(0, endX - startX),
                Math.Max(0, endY - startY));
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (IsDisposed()) return;

            // T133-M (FR-002, FR-015): the frame clock
            // the render path hands downstream is the
            // game's own clock. UpdateTickedEventArgs
            // carries no GameTime, so the Game1
            // singleton the dispatch runs on is the
            // source of truth; fabricating a zero-time
            // clock starves the FR-015 throttle (every
            // elapsed delta is zero, so every same-tile
            // tooltip refresh is suppressed forever).
            _lastGameTime = StardewValley.Game1.currentGameTime;
        }

        /// <summary>
        /// Pauses and resumes visualization rendering around
        /// in-game menus (T126, FR-025, DC-4, SC-003). Any menu
        /// opening pauses the whole visualization frame — overlay,
        /// tooltip and hoe feedback — so no world-space draw call
        /// lands behind a menu, and tooltips are hidden with the
        /// FR-002 cursor-leave semantics (the tracked cursor tile
        /// is cleared, exactly as though the cursor left the tile).
        /// Closing the LAST open menu resumes rendering within the
        /// same frame, the SC-003 bound; a menu-to-menu switch or
        /// the close of a non-last menu leaves a menu open and so
        /// keeps rendering paused. The dispatch is synchronous with
        /// the menu transition, which is what makes "within one
        /// frame" hold by construction.
        /// </summary>
        /// <remarks>
        /// FR-015's tooltip throttle window must not survive a
        /// menu open/close cycle: closing the last menu resets
        /// the per-tile throttle window (through
        /// <see cref="Domain.IVisualizationService.ResetTooltipThrottle"/>)
        /// so the leading-edge rule applies fresh to the next
        /// hover. The reset belongs to the visualization service
        /// (it owns the throttle state), so the handler demands
        /// that seam and invokes it on the last-menu-close
        /// branch. The pause itself is not gated on any
        /// configuration: FR-025 forbids deferring
        /// configuration hot-reload while a menu is open, and
        /// the render frame reloads the persisted configuration
        /// as its first statement regardless of the pause state.
        /// </remarks>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (IsDisposed() || _visualizationService == null) return;

            try
            {
                if (e.NewMenu != null)
                {
                    // Any menu opened (including a menu-to-menu
                    // switch): pause the visualization frame.
                    _visualizationService.PauseRendering();

                    // FR-002 acceptance scenario 2: hide tooltips
                    // with the cursor-leave semantics while the
                    // menu is open.
                    _visualizationService.ClearCursorTile();
                }
                else if (e.OldMenu != null)
                {
                    // The last open menu closed: resume within the
                    // same frame (SC-003), and reset the FR-015
                    // tooltip throttle window — the window must not
                    // survive a menu open/close cycle, so the
                    // leading-edge rule applies fresh to the next
                    // hover.
                    _visualizationService.ResumeRendering();
                    _visualizationService.ResetTooltipThrottle();
                }
            }
            catch (Exception ex)
            {
                _monitor.Log("Error handling menu change.", LogLevel.Warn);
                _monitor.Log($"Menu change exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// The screen-space half of the render frame (T132,
        /// FR-024, DC-3): the features that must draw above
        /// all vanilla content — the HUD, open menus and the
        /// cursor — render on <c>Display.Rendered</c>, which
        /// SMAPI dispatches after the world-space frame
        /// (<see cref="OnRenderedWorld"/>), so the tile
        /// snapshot and the shared white texture the tooltip
        /// draws through are already installed for the frame.
        /// </summary>
        /// <remarks>
        /// The tooltip is the screen-space feature assigned
        /// here today. FR-024 assigns the hoe floating text
        /// here too, through the dedicated
        /// <see cref="Domain.IVisualizationService.
        /// RenderHoeFeedbackFloatingText"/> seam: the flash
        /// half of hoe feedback stays on
        /// <see cref="Domain.IVisualizationService.RenderHoeFeedback"/>
        /// / <c>Display.RenderedWorld</c>, so the single
        /// RenderHoeFeedback method cannot carry both
        /// screen-space and world-space effects. No
        /// white-texture dance happens here: the frame's
        /// world-space half runs first and has already
        /// installed it.
        /// </remarks>
        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (IsDisposed() || _visualizationService == null) return;

            try
            {
                var gameTime = _lastGameTime;

                // T087 (FR-002, DC-5): the tooltip
                // anchor is the real cursor position,
                // and the tracked cursor tile is
                // resolved at render time through the
                // platform-provided mapping (DC-5:
                // ICursorPosition.Tile — the game
                // reports the tile under the cursor,
                // so no manual screen-to-tile
                // conversion exists here). The anchor
                // is GetScaledScreenPixels — the
                // cursor in UI-scaled viewport pixels,
                // the same space the FR-002 viewport
                // clamp (Game1.viewport, and therefore
                // TooltipRenderer.ComputeTooltipPosition)
                // and the UI-scaled FR-002 offset
                // operate in; the raw pixel positions
                // are not UI-scaled and Tile itself is
                // tile-index units, not pixels, so
                // neither may anchor the panel. Both
                // reads are null-safe: a host without
                // the input events wired (a headless
                // test harness) yields the zero anchor
                // and the zero tile, which the tooltip
                // path already treats as "no tile".
                var cursor = _helper?.Input?.GetCursorPosition();
                _visualizationService.UpdateCursorTile(
                    cursor?.Tile.ToPoint() ?? Microsoft.Xna.Framework.Point.Zero);
                var cursorPosition = cursor?.GetScaledScreenPixels()
                    ?? Microsoft.Xna.Framework.Vector2.Zero;
                _visualizationService.RenderTooltip(e.SpriteBatch, cursorPosition, gameTime);

                // FR-024 (T132): the hoe floating text
                // renders here, through the
                // RenderHoeFeedbackFloatingText seam —
                // screen space, above all vanilla content.
                // The flash half of hoe feedback stays
                // on RenderHoeFeedback /
                // Display.RenderedWorld.
                _visualizationService.RenderHoeFeedbackFloatingText(e.SpriteBatch, gameTime);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in screen-space visualization rendering.", LogLevel.Warn);
                _monitor.Log($"Screen-space render exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (IsDisposed() || _visualizationService == null) return;

            try
            {
                var cursorPos = _helper?.Input.GetCursorPosition();
                var cursorTile = cursorPos?.GrabTile.ToPoint() ?? Microsoft.Xna.Framework.Point.Zero;
                _visualizationService.UpdateCursorTile(cursorTile);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in visualization input handling.", LogLevel.Warn);
                _monitor.Log($"Input exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (IsDisposed()) return;

            try
            {
                // T074 (FR-004, T063): the SMAPI DayStarted
                // event fires once per day for the whole host —
                // not once per location — so a day-start path
                // that only processes Game1.currentLocation
                // silently starves every other farm location's
                // bins and soil. The mod's bins live on the
                // farm and in the Greenhouse (FR-002, T057),
                // the complete set of farm locations a save
                // holds, so the day tick reaches the location
                // the host occupies plus the farm locations it
                // does not: the Greenhouse, and the farm
                // itself, whose location name is "Farm" for
                // every farm type. Each service keeps its own
                // exactly-once-per-day, per-location guard
                // (T056 in CompostingBinService, T068 in
                // SoilDecayService), so a location named twice
                // on the same day — the occupied farm overlaps
                // the literal pair — is ticked once.
                var farmLocationNames = new HashSet<string>();
                var currentLocationName = StardewValley.Game1.currentLocation?.Name;
                if (!string.IsNullOrEmpty(currentLocationName))
                {
                    farmLocationNames.Add(currentLocationName);
                }

                farmLocationNames.Add("Greenhouse");
                farmLocationNames.Add("Farm");

                foreach (var locationName in farmLocationNames)
                {
                    // T134 (FR-012/FR-014): narrow the day-start try per location,
                    // so a corrupt record in one location cannot abort the day
                    // tick for the others — the Greenhouse still matures when
                    // the Farm's tick throws.
                    try
                    {
                        _compostingBinService.ProcessDayStart(locationName);
                        _soilDecayService.ProcessDayStart(locationName);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"Error processing day start for location '{locationName}'.", LogLevel.Error);
                        _monitor.Log($"Day start exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in day started handler.", LogLevel.Error);
                _monitor.Log($"Day started exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Prunes the live bin cache when the game
        /// itself removes objects from a location
        /// (T100, FR-002). A placed bin is taken
        /// out of <see cref="StardewValley.GameLocation.Objects"/>
        /// by the game on a tool hit (axe,
        /// pickaxe, hoe) or an environmental
        /// explosion; the mod learns of the
        /// removal through this event. Every
        /// composting bin among the removed
        /// objects prunes its live cache entry
        /// through <see cref="ICompostingBinService.OnObjectRemoved"/>,
        /// so a stale record never acts as a
        /// ghost bin — the same prune the
        /// right-click dislodge path performs
        /// (TryDislodgeBin), but for the
        /// game-driven removals that path never
        /// sees, and without requiring a save
        /// reload. The world object is already
        /// gone by the time the event fires (the
        /// game removed it), so this handler only
        /// clears the service-side record; the
        /// location is read from the event args,
        /// not from the active location, so a
        /// removal in any loaded location prunes
        /// that location's cache.
        /// </summary>
        private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
        {
            if (IsDisposed()) return;

            try
            {
                foreach (var removed in e.Removed)
                {
                    if (removed.Value.ItemId == ModConstants.CompostingBinItemId)
                    {
                        // T127 (FR-005c): reconstruct dislodged bin carrying progress
                        // modData BEFORE the record is pruned, so the batch
                        // (InputId/DaysRemaining) survives removal instead of being
                        // destroyed with the pruned record.
                        var dislodgedItem = _compostingBinService.CreateDislodgedBinItem(e.Location.Name, removed.Key);
                        _compostingBinService.OnObjectRemoved(e.Location.Name, removed.Key);
                        if (dislodgedItem == null)
                        {
                            continue;
                        }

                        // T131 (FR-005c): route the reconstructed bin back into the
                        // player's inventory when there is space, and drop debris
                        // only when the inventory refused it. A removed bin is never
                        // silently destroyed.
                        bool addedToInventory = false;
                        try
                        {
                            var player = StardewValley.Game1.player;
                            if (player != null)
                            {
                                addedToInventory = player.addItemToInventoryBool(dislodgedItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Headless hosts (Game1.player unallocated) take the debris
                            // path below. Sanitized per FR-015.
                            _monitor.Log($"Dislodged bin inventory return unavailable: {ex.GetType().FullName}, HResult: {ex.HResult}", LogLevel.Trace);
                        }

                        if (!addedToInventory && StardewValley.Game1.currentLocation != null)
                        {
                            // Inventory full (or unavailable): floating debris retains
                            // the item's modData (FR-005c), so progress is not lost.
                            StardewValley.Game1.createItemDebris(
                                dislodgedItem,
                                removed.Key * (float)ModConstants.TileSize,
                                -1,
                                e.Location);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in object list changed handler.", LogLevel.Error);
                _monitor.Log($"Object list changed exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        private void OnButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (IsDisposed()) return;

            // Hoe usage is a left-button press (US3, FR-003, FR-011):
            // feed the visualization service when it lands on tilled soil.
            if (e.Button == StardewModdingAPI.SButton.MouseLeft)
            {
                HandleHoeUsage();
                return;
            }

            if (e.Button != StardewModdingAPI.SButton.MouseRight) return;

            try
            {
                var cursorPos = _helper?.Input.GetCursorPosition();
                var tile = cursorPos?.GrabTile;
                var location = StardewValley.Game1.currentLocation;

                if (!tile.HasValue || location == null) return;

                var binTile = tile.Value;

                // T082 (F3, FR-003, US3/AC4): the held
                // item and the acting farmer are read
                // through the injected IPlayerProvider —
                // the same held-item accessor
                // CompostApplicationService consumes — so
                // no second source of truth exists. A null
                // provider (not injected) carries no
                // held-item information, which counts as
                // empty hands.
                var heldItem = _playerProvider?.CurrentItem;
                // T128 (FR-019): the farmer id is the multiplayer identity the
                // host-authoritative lock keys on. An unresolved provider or
                // player yields no id at all — it is NOT coerced into a valid
                // actor: the downstream fail-closed guards (TryInteractWithBin,
                // TryApplyCompostWithLock) treat id 0 as "no actor" and refuse,
                // rather than letting a collapsed id act as a second farmer.
                // T128 (FR-019): the farmer id is the multiplayer identity the
                // host-authoritative lock keys on. An unresolved provider or
                // player yields no id at all — it is NOT coerced into a valid
                // actor: the fail-closed guards downstream treat a missing id
                // as "no actor" and refuse, instead of letting a collapsed id
                // act as a second farmer. GetValueOrDefault keeps the "no
                // actor" value explicit rather than hiding it in a coalesce.
                long? resolvedFarmerId = _playerProvider?.CurrentPlayer?.UniqueMultiplayerID
                    ?? StardewValley.Game1.player?.UniqueMultiplayerID;
                long farmerId = resolvedFarmerId.GetValueOrDefault();

                if (heldItem != null &&
                    (heldItem.QualifiedItemId == ModConstants.CompostItemId ||
                     heldItem.QualifiedItemId == ModConstants.QualifiedCompostItemId ||
                     heldItem.ItemId == ModConstants.CompostItemId))
                {
                    if (TryApplyCompostWithLock(location, binTile, farmerId, msg => StardewValley.Game1.showRedMessage(TranslateRefusalMessage(msg))))
                    {
                        return;
                    }
                }

                // T073 (FR-002, FR-005c): a right-click
                // holding a composting bin places it through
                // the mod's own placement path — the path
                // that restores a carried batch's progress
                // into the runtime cache (OnBinPlaced) and
                // keeps the cache and the world object in
                // lockstep. The player's own inventory is
                // passed so the depleted stack slot is
                // cleared. A refused placement (water, an
                // occupied tile, a non-farm map) falls
                // through to the interaction path below.
                if (heldItem != null && heldItem.ItemId == ModConstants.CompostingBinItemId)
                {
                    if (TryPlaceBin(location, binTile, heldItem, _playerProvider?.CurrentPlayer?.Items))
                    {
                        return;
                    }
                }

                // T131 FR-005c: route dislodged bin into inventory when space is available
                // (debris only when full). Pass a PlayerInventory instance so the dislodged
                // bin carrying progress lands in inventory rather than always becoming debris.
                if (heldItem == null && TryDislodgeBin(location, binTile, inventory: new PlayerInventory(), onDropDebris: null))
                {
                    return;
                }

                TryInteractWithBin(location, binTile, farmerId, heldItem, null, msg => StardewValley.Game1.showRedMessage(TranslateRefusalMessage(msg)));
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in button pressed handler.", LogLevel.Error);
                _monitor.Log($"Button pressed exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Resolves a refusal translation key (T054, FR-008a) through
        /// the mod helper's <see cref="ITranslationHelper"/> so the red
        /// HUD message rendered by the <see cref="OnButtonPressed"/>
        /// refusal closures shows the player's own language instead of
        /// the raw key. The refusal handlers invoked by
        /// <see cref="TryInteractWithBin"/> /
        /// <see cref="TryApplyCompostWithLock"/> still receive the key —
        /// translation happens at the production display boundary, so
        /// test-supplied handlers keep asserting on the key contract.
        /// Falls back to the raw key when no translation pipeline is
        /// available (headless hosts). Resolved per call, never cached —
        /// the locale can change mid-session.
        /// </summary>
        private string TranslateRefusalMessage(string messageKey)
        {
            var translation = _helper?.Translation;
            if (translation == null)
            {
                return messageKey;
            }

            return translation.Get(messageKey);
        }

        /// <summary>
        /// Feeds hoe feedback to the visualization service for the tile under
        /// the cursor when that tile is tilled soil (US3, FR-003, FR-011).
        /// Hoe usage is a left-button press while the equipped tool is a
        /// hoe (T082, F3, US3/AC4): the held item is read through the
        /// injected <see cref="IPlayerProvider.CurrentItem"/> — the same
        /// held-item accessor <see cref="Services.CompostApplicationService"/>
        /// consumes, so no second source of truth exists — and only a
        /// <see cref="StardewValley.Tools.Hoe"/> counts. Every other held
        /// item (a seed pouch, a watering can, any other tool, or an empty
        /// hand) is not hoe usage, so the press never reaches
        /// <see cref="Domain.IVisualizationService.TriggerHoeFeedback"/>.
        /// A tile is tilled when its terrain feature is a
        /// <see cref="StardewValley.TerrainFeatures.HoeDirt"/> — the terrain
        /// feature the hoe tool itself creates, which exists only on tilled
        /// soil; every other feature (grass, objects, untilled soil) is
        /// excluded, so a grass tile never reaches
        /// <see cref="Domain.IVisualizationService.TriggerHoeFeedback"/>. Only
        /// the cursor tile is fed back, so adjacent tilled tiles never receive
        /// feedback (FR-011). The visualization service is optional, and the
        /// <see cref="StardewValley.Game1.currentLocation"/> read dereferences
        /// the <c>Game1</c> singleton, which headless hosts leave unallocated
        /// — there the property throws instead of returning null, so the read
        /// degrades gracefully and the last snapshot is kept.
        /// </summary>
        private void HandleHoeUsage()
        {
            if (_visualizationService == null)
            {
                return;
            }

            // T082 (F3, FR-003, US3/AC4): gate on the equipped tool
            // before any game-state read — the cheapest check first.
            // A null provider (not injected) carries no held-item
            // information, which is equally not a hoe.
            var heldItem = _playerProvider?.CurrentItem;
            if (heldItem is not StardewValley.Tools.Hoe)
            {
                return;
            }

            try
            {
                StardewValley.GameLocation location;
                try
                {
                    location = StardewValley.Game1.currentLocation;
                }
                catch (Exception ex)
                {
                    _monitor.Log("Active location unavailable for hoe feedback.", LogLevel.Warn);
                    _monitor.Log($"Hoe feedback location exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    return;
                }

                if (location == null)
                {
                    return;
                }

                var cursorPos = _helper?.Input.GetCursorPosition();
                var tile = cursorPos?.GrabTile;
                if (!tile.HasValue)
                {
                    return;
                }

                var tileVector = tile.Value;
                if (!location.terrainFeatures.TryGetValue(tileVector, out var terrainFeature)
                    || terrainFeature is not StardewValley.TerrainFeatures.HoeDirt)
                {
                    return;
                }

                var locationName = location.Name ?? "Unknown";
                var health = _soilHealthService.GetSoilHealth(locationName, tileVector);
                _visualizationService.TriggerHoeFeedback(tileVector.ToPoint(), health);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error in hoe feedback handling.", LogLevel.Warn);
                _monitor.Log($"Hoe feedback handling exception details: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
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

        /// <summary>
        /// Validates terrain and map criteria and places a composting bin at the target tile.
        /// Refuses placement on invalid terrain (water, occupied tiles, non-farm/Greenhouse
        /// maps), leaving zero ghost objects and leaving inventory untouched (FR-002).
        /// </summary>
        /// <param name="location">The target game location.</param>
        /// <param name="tile">The tile coordinate for placement.</param>
        /// <param name="binItem">The bin item to place.</param>
        /// <param name="inventory">Optional inventory from which to deduct the item.</param>
        /// <returns>True if placement succeeded; false if refused.</returns>
        public bool TryPlaceBin(StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tile, StardewValley.Item binItem, IList<StardewValley.Item?>? inventory = null)
        {
            // T057 decision (FR-002, FR-005c): intercept —
            // route (b), not full native placement (route a).
            // The bin is a native big craftable (T053), but
            // the mod's own placement wiring stays: only this
            // path restores a carried batch's progress into
            // the runtime cache (OnBinPlaced, FR-005c) and
            // keeps the cache and the world object in
            // lockstep — vanilla placement would place the
            // machine without either. Three fixes land at
            // T166 / FR-002: allow farm / Greenhouse with HoeDirt; relax invalid-terrain refusal
            if (IsDisposed())
            {
                return false;
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (binItem == null)
            {
                throw new ArgumentNullException(nameof(binItem));
            }

            if (binItem.ItemId != ModConstants.CompostingBinItemId)
            {
                return false;
            }

            // T105 (FR-002): FR-002 names
            // "the Greenhouse", but a modded
            // greenhouse - a custom greenhouse
            // map or an added greenhouse
            // variant - carries its own name.
            // Placement is accepted for any
            // greenhouse-like location (a
            // case-insensitive "greenhouse"
            // substring in the location name),
            // not just the literal name; every
            // other non-farm interior stays
            // refused (only water, occupied
            // tiles, buildings and non-farm,
            // non-greenhouse interiors are
            // invalid terrain).
            bool locationValid = location.IsFarm || IsGreenhouseLikeLocation(location);

            // T129 (FR-002): refuse placement on building footprint tiles
            if (locationValid)
            {
                foreach (var building in location.buildings)
                {
                    var bTileX = building.tileX.Value;
                    var bTileY = building.tileY.Value;
                    if (tile.X >= bTileX && tile.X < bTileX + building.tilesWide.Value &&
                        tile.Y >= bTileY && tile.Y < bTileY + building.tilesHigh.Value)
                    {
                        return false;
                    }
                }
            }

            if (!locationValid)
            {
                // Non-farm/non-greenhouse locations are refused
                return false;
            }

            if (location.Objects.ContainsKey(tile))
            {
                return false;
            }

            // T057 (FR-002): water is the only invalid
            // terrain left once the HoeDirt-only
            // restriction is relaxed — the native big
            // craftable places on any clear farm or
            // Greenhouse tile, tilled or not. The
            // water map's public array carries no
            // dimension accessors, so the bounds come
            // from the array itself, and a tile reads
            // as water through the indexer's implicit
            // WaterTileData-to-bool conversion.
            var waterMap = location.waterTiles;
            if (waterMap != null &&
                (int)tile.X >= 0 && (int)tile.Y >= 0 &&
                (int)tile.X < waterMap.waterTiles.GetLength(0) &&
                (int)tile.Y < waterMap.waterTiles.GetLength(1) &&
                waterMap[(int)tile.X, (int)tile.Y])
            {
                return false;
            }

            // T098 (FR-001): the placed bin is the native
            // big craftable the crafting recipe yields
            // (recipe segment 3 is `true`), so the world
            // object must set the big-craftable flag —
            // the same shape CompostingBinService.
            // CreateDislodgedBinItem produces (T076).
            // bigCraftable.Value flips QualifiedItemId from
            // (O) to (BC) (verified against the game
            // assembly), so the placed machine resolves as
            // (BC)LivingRoots.CompostingBin — the item the
            // Data/Machines rules key on — and not a
            // nonexistent (O) object. Without the flag the
            // native machine interface never engages: the
            // deposit path gates on the world object being
            // the bin big craftable, and an (O)-shaped
            // object would read as a different item.
            var placedObject = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
            placedObject.bigCraftable.Value = true;

            // T142/T005c-1 (FR-005c): the world object carries the batch
            // progress the held item serialized, so the placement is itself
            // evidence of the batch — the dislodge path reads the service
            // record, but the world object is what the game persists, and a
            // placed bin whose record disagrees with its object is the
            // duplication hazard FR-002 exists to prevent.
            if (binItem.modData != null)
            {
                // T142/FR-005c: Item.modData is a KeyValuePair-shaped map in
                // Stardew 1.6, so read it through Pairs and write each entry
                // through the placed object's indexer.
                foreach (var entry in binItem.modData.Pairs)
                {
                    placedObject.modData[entry.Key] = entry.Value;
                }
            }

            location.Objects.Add(tile, placedObject);

            _compostingBinService.OnBinPlaced(location.Name, tile, binItem);

            binItem.Stack--;
            if (binItem.Stack <= 0 && inventory != null)
            {
                int index = inventory.IndexOf(binItem);
                if (index != -1)
                {
                    inventory[index] = null;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates terrain and map criteria and places a composting bin at the target tile on behalf of a farmer.
        /// </summary>
        /// <param name="location">The target game location.</param>
        /// <param name="tile">The tile coordinate for placement.</param>
        /// <param name="farmer">The farmer placing the bin.</param>
        /// <param name="binItem">The bin item to place.</param>
        /// <returns>True if placement succeeded; false if refused.</returns>
        public bool TryPlaceBin(StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tile, StardewValley.Farmer farmer, StardewValley.Item binItem)
        {
            if (farmer == null)
            {
                throw new ArgumentNullException(nameof(farmer));
            }

            return TryPlaceBin(location, tile, binItem, farmer.Items);
        }

        /// <summary>
        /// Determines whether a location is
        /// greenhouse-like (T105, FR-002):
        /// the location name contains
        /// "greenhouse" under a case-
        /// insensitive comparison, so the
        /// literal Greenhouse and any modded
        /// greenhouse map or variant (a
        /// custom greenhouse map, an added
        /// greenhouse variant) both qualify.
        /// A name without the fragment is
        /// not a greenhouse, so non-farm
        /// interiors without it stay refused.
        /// </summary>
        /// <remarks>
        /// T197 (FR-002): the rule itself lives
        /// in <see cref="GreenhouseLocationRule"/>
        /// so the placement gate and the compost
        /// application gate cannot disagree —
        /// a bin placed here must have its
        /// compost applicable here too. Only the
        /// game-type seam is kept here.
        /// </remarks>
        /// <param name="location">The location to classify.</param>
        /// <returns>True when the location name is greenhouse-like.</returns>
        private static bool IsGreenhouseLikeLocation(StardewValley.GameLocation location)
        {
            return GreenhouseLocationRule.IsGreenhouseLike(location);
        }

        /// <summary>
        /// Dislodges a composting bin from the world via tool hit or explosion.
        /// Preserves processing progress in modData and leaves zero orphaned state (FR-002, FR-005c).
        /// If inventory is full, drops the bin as floating world debris retaining its modData.
        /// </summary>
        /// <param name="location">The location containing the bin.</param>
        /// <param name="tile">The tile coordinate of the bin.</param>
        /// <param name="inventory">Optional player inventory to receive the dislodged item.</param>
        /// <param name="onDropDebris">Optional callback when inventory is full to drop floating debris.</param>
        /// <returns>True if bin was dislodged; false if no bin existed at tile.</returns>
        public bool TryDislodgeBin(
            StardewValley.GameLocation location,
            Microsoft.Xna.Framework.Vector2 tile,
            Domain.IPlayerInventory? inventory = null,
            Action<StardewValley.Item, Microsoft.Xna.Framework.Vector2, StardewValley.GameLocation>? onDropDebris = null)
        {
            if (IsDisposed())
            {
                return false;
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (!location.Objects.TryGetValue(tile, out var obj) || obj.ItemId != ModConstants.CompostingBinItemId)
            {
                return false;
            }

            var dislodgedItem = _compostingBinService.CreateDislodgedBinItem(location.Name, tile);

            location.Objects.Remove(tile);
            _compostingBinService.OnObjectRemoved(location.Name, tile);

            if (dislodgedItem == null)
            {
                // No progress record to preserve — nothing to reconstruct.
                return true;
            }

            bool added = false;
            if (inventory != null)
            {
                added = inventory.TryAddItem(dislodgedItem);
            }

            if (!added)
            {
                if (onDropDebris != null)
                {
                    onDropDebris(dislodgedItem, tile, location);
                }
                else if (StardewValley.Game1.currentLocation != null)
                {
                    StardewValley.Game1.createItemDebris(dislodgedItem, tile * (float)ModConstants.TileSize, -1, location);
                }
            }

            return true;
        }

        /// <summary>
        /// Dislodges a composting bin from the world on behalf of a farmer.
        /// </summary>
        /// <param name="location">The location containing the bin.</param>
        /// <param name="tile">The tile coordinate of the bin.</param>
        /// <param name="farmer">The farmer performing the dislodgement.</param>
        /// <returns>True if bin was dislodged; false if no bin existed at tile.</returns>
        /// <summary>
        /// Validates terrain and map criteria and dislodges a composting bin at
        /// the target tile on behalf of a farmer. T131 (FR-005c): the
        /// reconstructed bin is routed into the farmer's inventory whenever
        /// there is space, and only drops as floating debris when the inventory
        /// refused it — so a removed bin is never silently destroyed.
        /// </summary>
        public bool TryDislodgeBin(StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tile, StardewValley.Farmer farmer)
        {
            if (farmer == null)
            {
                throw new ArgumentNullException(nameof(farmer));
            }

            return TryDislodgeBin(location, tile, new FarmerInventoryAdapter(farmer), null);
        }

        /// <summary>
        /// Attempts to interact with a composting bin (depositing waste or collecting compost)
        /// with host-authoritative atomic coordinate locking to resolve multiplayer action conflicts (FR-019).
        /// </summary>
        /// <param name="location">The game location containing the bin.</param>
        /// <param name="tile">The tile coordinate of the bin.</param>
        /// <param name="farmerId">The unique multiplayer ID of the interacting farmer.</param>
        /// <param name="heldItem">The item currently held by the farmer, or null if hands are empty.</param>
        /// <param name="playerInventory">
        /// Retained for call-site compatibility. T075 (FR-005a): the
        /// compost yield is credited solely by
        /// <see cref="ICompostingBinService.CollectCompost"/>, which grants
        /// through the service's own injected player inventory — the
        /// composition root injects the same inventory into both layers —
        /// so the controller no longer grants a second item here.
        /// </param>
        /// <param name="refusalHandler">Optional callback invoked with a refusal translation key when interaction fails.</param>
        /// <returns>True if the interaction was successfully processed; otherwise, false.</returns>
        public bool TryInteractWithBin(
            StardewValley.GameLocation location,
            Microsoft.Xna.Framework.Vector2 tile,
            long farmerId,
            StardewValley.Item? heldItem,
            Domain.IPlayerInventory? playerInventory = null,
            Action<string>? refusalHandler = null)
        {
            if (location == null)
            {
                return false;
            }

            // T057 (FR-002, FR-005c): intercept only a
            // physical bin — the world object at the
            // tile must be the composting bin big
            // craftable. The service cache alone cannot
            // tell a placed bin from a ghost record (a
            // cache entry whose world object is gone),
            // so every deposit and collection is gated
            // on the machine itself.
            if (!location.Objects.TryGetValue(tile, out var worldObject) ||
                worldObject.ItemId != ModConstants.CompostingBinItemId)
            {
                return false;
            }

            // T128 (FR-019): fail-closed guard — interaction refused (not silently
            // allowed) when no host-authoritative atomic lock service is available,
            // when the farmer identifier collapsed to the no-actor value (T128 guards this),
            // or when the lock is already held by another farmer. The refusal
            // handler receives the conflict message key; zero mutation occurs.
            if (farmerId == 0L)
            {
                refusalHandler?.Invoke(ModConstants.TranslationKeyCompostActionConflict);
                return false;
            }

            if (_hostAtomicLockService == null ||
                !_hostAtomicLockService.TryAcquireLock(location.Name, tile, farmerId))
            {
                // T118 FR-019: refuse when lock service absent or acquisition fails
                refusalHandler?.Invoke(ModConstants.TranslationKeyCompostActionConflict);
                return false;
            }

            try
            {
                var state = _compostingBinService.GetBinState(location.Name, tile);

                if (heldItem != null)
                {
                    if (state == Domain.CompostingBinState.Empty)
                    {
                        _compostingBinService.AddWaste(location.Name, tile, heldItem);

                        // T057 (FR-003, FR-005c): a
                        // deposit the bin accepted
                        // consumes exactly one unit
                        // of the held stack — the
                        // same one-in rule the
                        // compost application path
                        // (FR-007) enforces. Without
                        // the consumption the same
                        // unit could be offered to
                        // the bin an unbounded number
                        // of times (infinite waste).
                        // The post-deposit state read
                        // is the acceptance signal:
                        // a rejected item leaves the
                        // bin Empty.
                        if (_compostingBinService.GetBinState(location.Name, tile) != Domain.CompostingBinState.Empty)
                        {
                            heldItem.Stack--;
                        }

                        return true;
                    }

                    // T077 (FR-008a scoping, T066):
                    // a deposit refused because the
                    // bin is already processing a
                    // batch is an ordinary invalid
                    // bin state — a vanilla machine
                    // cue, not a concurrency
                    // collision — so the refusal
                    // handler receives nothing.
                    return false;
                }
                else if (state == Domain.CompostingBinState.Ready)
                {
                    // T075 (FR-005a):
                    // CollectCompost is the sole
                    // compost credit — it grants
                    // the yield through the
                    // service's injected player
                    // inventory, so the controller
                    // must not grant a second item
                    // into the caller's inventory
                    // (a duplicated compost item
                    // wherever the composition root
                    // injects the same inventory
                    // into both layers).
                    int yield = _compostingBinService.CollectCompost(location.Name, tile);
                    return yield > 0;
                }

                // T077 (FR-008a scoping, T066):
                // collecting from a bin that is
                // not Ready — empty, or still
                // processing an unfinished batch —
                // is an ordinary invalid bin state,
                // not a concurrency collision, so
                // the refusal handler receives
                // nothing.
                return false;
            }
            finally
            {
                _hostAtomicLockService?.ReleaseLock(location.Name, tile, farmerId);
            }
        }

        /// <summary>
        /// Attempts to apply compost to a tile with host-authoritative atomic coordinate locking (FR-019).
        /// If the lock cannot be acquired due to an action conflict, refuses the action, triggers the refusal handler,
        /// and leaves player items untouched.
        /// </summary>
        public bool TryApplyCompostWithLock(
            StardewValley.GameLocation location,
            Microsoft.Xna.Framework.Vector2 tile,
            long farmerId,
            Action<string>? refusalHandler = null)
        {
            if (location == null)
            {
                return false;
            }

            if (_compostApplicationService == null)
            {
                return false;
            }

            // T106 (Constitution V, FR-019): the
            // single shared lock instance the
            // composition root creates and injects
            // here and into CompostingBinService
            // (T055) is the one registry — the
            // `?? new HostAtomicLockService()`
            // second-registry hazard was removed
            // at T081b and must never return, and
            // the production controller never
            // carries a null lock service (T096
            // verifies both invariants by
            // reflection). A controller built
            // without a lock service — only a
            // headless double can, the parameter
            // keeps its nullable default for
            // those doubles — now fails closed:
            // the application is refused rather
            // than silently allowed, because soil
            // health is host-authoritative shared
            // farm state (Constitution VI) and an
            // uncoordinated write is exactly the
            // FR-019 contention hazard, with no
            // registry to resolve it through.
            // Production never reaches this branch
            // (ModEntry always injects the shared
            // instance), so the fail-closed guard
            // changes no production behaviour.
            if (_hostAtomicLockService == null)
            {
                _monitor.Log("Compost application refused: no host atomic lock service is injected, so tile contention cannot be resolved (FR-019).", LogLevel.Warn);

                // T122 (FR-008a): a refusal the player cannot see is a refusal
                // they cannot understand. The conflict is reported through the
                // refusal handler, which the input boundary translates into the
                // game's own on-screen message — never silence (FR-019: the
                // refused player must be told).
                refusalHandler?.Invoke(ModConstants.TranslationKeyCompostActionConflict);
                return false;
            }

            if (!_hostAtomicLockService.TryAcquireLock(location.Name, tile, farmerId))
            {
                // T077 (FR-008a, FR-019): a tile
                // lock already held by another
                // farmer is a genuine concurrency
                // collision — the conflict key.
                refusalHandler?.Invoke(ModConstants.TranslationKeyCompostActionConflict);
                return false;
            }

            try
            {
                return _compostApplicationService.TryApplyCompost(location, tile);
            }
            finally
            {
                _hostAtomicLockService?.ReleaseLock(location.Name, tile, farmerId);
            }
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
        public EventHandler<DayStartedEventArgs>? LocalDayStartedHandler { get; init; }
        public EventHandler<ButtonPressedEventArgs>? LocalButtonPressedHandler { get; init; }
        public EventHandler<RenderedWorldEventArgs>? LocalRenderedWorldHandler { get; init; }
        public EventHandler<ButtonReleasedEventArgs>? LocalButtonReleasedHandler { get; init; }
        public EventHandler<MenuChangedEventArgs>? LocalMenuChangedHandler { get; init; }
        public EventHandler<RenderedEventArgs>? LocalRenderedHandler { get; init; }
        public EventHandler<SavedEventArgs>? LocalSavedHandler { get; init; }
        public EventHandler<ObjectListChangedEventArgs>? LocalObjectListChangedHandler { get; init; }
        public IMonitor Monitor { get; init; }
        public IModHelper? Helper { get; init; }
    }

    /// <summary>
    /// Context class to encapsulate parameters for event unregistration
    /// </summary>
    internal readonly struct EventUnregisterContext
    {
        public EventHandler<GameLaunchedEventArgs>? GameLaunchedHandler { get; init; }
        public EventHandler<SaveLoadedEventArgs>? SaveLoadedHandler { get; init; }
        public EventHandler<SavingEventArgs>? SavingHandler { get; init; }
        public EventHandler<DayStartedEventArgs>? DayStartedHandler { get; init; }
        public EventHandler<ButtonPressedEventArgs>? ButtonPressedHandler { get; init; }
        public EventHandler<RenderedWorldEventArgs>? RenderedWorldHandler { get; init; }
        public EventHandler<ButtonReleasedEventArgs>? ButtonReleasedHandler { get; init; }
        public EventHandler<UpdateTickedEventArgs>? UpdateTickedHandler { get; init; }
        public EventHandler<MenuChangedEventArgs>? MenuChangedHandler { get; init; }
        public EventHandler<RenderedEventArgs>? RenderedHandler { get; init; }
        public EventHandler<SavedEventArgs>? SavedHandler { get; init; }
        public EventHandler<ObjectListChangedEventArgs>? ObjectListChangedHandler { get; init; }
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
        public bool DayStartedRemoved { get; init; }
        public bool ButtonPressedRemoved { get; init; }
        public bool UpdateTickedRemoved { get; init; }
        public bool MenuChangedRemoved { get; init; }
        public bool RenderedRemoved { get; init; }
        public bool SavedRemoved { get; init; }
        public bool ObjectListChangedRemoved { get; init; }
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
        public bool DayStartedRemoved { get; init; }
        public EventHandler<DayStartedEventArgs>? DayStartedHandler { get; init; }
        public bool ButtonPressedRemoved { get; init; }
        public EventHandler<ButtonPressedEventArgs>? ButtonPressedHandler { get; init; }
    }
}
