using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
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
        private static readonly HashSet<string> HelpFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "--help",
            "-h",
            "/?"
        };

        // Single atomic state for thread-safe management of controller state
        // Using bit flags: 0x01 = events registered, 0x02 = command registered, 0x04 = disposed
        private int _state = 0;

        private EventHandler<GameLaunchedEventArgs>? _onGameLaunchedHandler;

        // State bit flags
        private const int EventsRegisteredFlag = 0x01;
        private const int CommandRegisteredFlag = 0x02;
        private const int DisposedFlag = 0x04;

        public ModController(IModHelper helper, IMonitor monitor, IManifest manifest, IModDataService modDataService)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
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

            try
            {
                var gameLoop = helper?.Events?.GameLoop;
                if (gameLoop == null)
                {
                    monitor.Log("Helper or Events or GameLoop is null, cannot register events.", LogLevel.Error);
                    // Reset flag since registration failed - ensure disposed flag is preserved
                    Interlocked.And(ref _state, ~(EventsRegisteredFlag));
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
                // Log error and reset the flag if registration failed - ensure disposed flag is preserved
                monitor.Log("Error occurred while registering game events.", LogLevel.Error);
                _onGameLaunchedHandler = null;

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

                // Use Interlocked.Exchange to safely get and clear the handler
                var handler = Interlocked.Exchange(ref _onGameLaunchedHandler, null);

                // Always attempt to detach to avoid leaked handlers
                if (handler != null)
                {
                    gameLoop.GameLaunched -= handler;
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
            catch (Exception ex)
            {
                localMonitor.Log("Error occurred while unregistering game events.", LogLevel.Error);
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
            catch (Exception ex)
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
                    catch (Exception ex)
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
            catch (Exception ex)
            {
                _monitor?.Log("Error occurred while executing version command.", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            // Use TrySetStateFlag to ensure disposal flag is only set once
            if (!TrySetStateFlag(DisposedFlag))
            {
                _monitor.Log("Controller is already disposed.", LogLevel.Trace);
                return; // Already disposed
            }

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
                        monitor?.Log("Error occurred while unregistering GameLaunched event.", LogLevel.Error);
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
