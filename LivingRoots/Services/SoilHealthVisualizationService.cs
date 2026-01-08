using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services
{
    /// <summary>
    /// Main visualization service for soil health visualization system.
    /// Coordinates between soil health data retrieval, color mapping, and rendering components.
    /// </summary>
    public class SoilHealthVisualizationService : ISoilHealthVisualizationService
    {
        // Dependencies
        private readonly IMonitor _monitor;
        private readonly ISoilHealthService _soilHealthService;
        private readonly IVisualizationConfig _config;
        private readonly IColorMapper _colorMapper;
        private readonly ITileOverlayRenderer _tileOverlayRenderer;
        private readonly ITooltipRenderer _tooltipRenderer;
        private readonly IModHelper _helper;

        // State management
        private bool _isEnabled;
        private Vector2? _currentHoverTile;
        private Vector2? _hoeActionTile;
        private float? _hoeActionHealth;
        private DateTime? _hoeActionStartTime;

        // Event management
        private bool _eventsRegistered = false;
        private readonly object _eventLock = new();

        // Event handler fields for proper unsubscription
        private EventHandler<CursorMovedEventArgs>? _onCursorMovedHandler;
        private EventHandler<ButtonPressedEventArgs>? _onButtonPressedHandler;
        private EventHandler<RenderingWorldEventArgs>? _onRenderingWorldLayerHandler;
        private EventHandler<RenderedEventArgs>? _onRenderedHandler;
        private EventHandler<UpdateTickedEventArgs>? _onUpdateTickedHandler;

        // Performance monitoring
        private int _renderedTilesLastFrame = 0;
        private DateTime _lastPerformanceLog = DateTime.UtcNow;
        private readonly TimeSpan _performanceLogInterval;

        /// <summary>
        /// Initializes a new instance of SoilHealthVisualizationService.
        /// </summary>
        /// <param name="monitor">Monitor for logging</param>
        /// <param name="soilHealthService">Service for retrieving soil health data</param>
        /// <param name="config">Visualization configuration</param>
        /// <param name="colorMapper">Color mapper for health values</param>
        /// <param name="tileOverlayRenderer">Renderer for tile overlays</param>
        /// <param name="tooltipRenderer">Renderer for tooltips and feedback</param>
        /// <param name="helper">SMAPI helper for event registration</param>
        public SoilHealthVisualizationService(
            IMonitor monitor,
            ISoilHealthService soilHealthService,
            IVisualizationConfig config,
            IColorMapper colorMapper,
            ITileOverlayRenderer tileOverlayRenderer,
            ITooltipRenderer tooltipRenderer,
            IModHelper helper
        )
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _soilHealthService =
                soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _colorMapper = colorMapper ?? throw new ArgumentNullException(nameof(colorMapper));
            _tileOverlayRenderer =
                tileOverlayRenderer ?? throw new ArgumentNullException(nameof(tileOverlayRenderer));
            _tooltipRenderer =
                tooltipRenderer ?? throw new ArgumentNullException(nameof(tooltipRenderer));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));

            _isEnabled = false;

            // Initialize performance monitoring
            _performanceLogInterval = TimeSpan.FromMinutes(
                ModConstants.PerformanceLogIntervalMinutes
            );

            _monitor.Log("SoilHealthVisualizationService initialized.", LogLevel.Trace);
        }

        /// <inheritdoc/>
        public bool IsEnabled => _isEnabled;

        /// <inheritdoc/>
        public IVisualizationConfig Config => _config;

        /// <inheritdoc/>
        public void Enable()
        {
            try
            {
                if (_isEnabled)
                {
                    _monitor.Log("Visualization is already enabled.", LogLevel.Trace);
                    return;
                }

                _isEnabled = true;
                _monitor.Log("Soil health visualization enabled.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error enabling visualization: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <inheritdoc/>
        public void Disable()
        {
            try
            {
                if (!_isEnabled)
                {
                    _monitor.Log("Visualization is already disabled.", LogLevel.Trace);
                    return;
                }

                _isEnabled = false;
                ClearHoverState();
                ClearHoeActionState();
                _monitor.Log("Soil health visualization disabled.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error disabling visualization: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <inheritdoc/>
        public void RegisterEvents()
        {
            lock (_eventLock)
            {
                if (_eventsRegistered)
                {
                    _monitor.Log(
                        "Visualization events are already registered, skipping registration.",
                        LogLevel.Trace
                    );
                    return;
                }

                var registrationResult = new EventRegistrationResult();

                try
                {
                    InitializeEventHandlers();
                    RegisterEventHandlers(registrationResult);

                    _eventsRegistered = true;
                    _monitor.Log("Visualization events registered successfully.", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    HandleRegistrationFailure(ex, registrationResult);
                }
            }
        }

        /// <summary>
        /// Initializes event handler delegates if they haven't been initialized.
        /// </summary>
        private void InitializeEventHandlers()
        {
            _onCursorMovedHandler ??= OnCursorMoved;
            _onButtonPressedHandler ??= OnButtonPressed;
            _onRenderingWorldLayerHandler ??= OnRenderingWorld;
            _onRenderedHandler ??= OnRendered;
            _onUpdateTickedHandler ??= OnUpdateTicked;
        }

        /// <summary>
        /// Registers all event handlers via SMAPI helper.
        /// </summary>
        /// <param name="result">Registration result to track which events were successfully registered</param>
        private void RegisterEventHandlers(EventRegistrationResult result)
        {
            _helper.Events.Input.ButtonPressed += _onButtonPressedHandler;
            result.ButtonPressedAdded = true;

            _helper.Events.Input.CursorMoved += _onCursorMovedHandler;
            result.CursorMovedAdded = true;

            _helper.Events.Display.RenderingWorld += _onRenderingWorldLayerHandler;
            result.RenderingWorldAdded = true;

            _helper.Events.Display.Rendered += _onRenderedHandler;
            result.RenderedAdded = true;

            _helper.Events.GameLoop.UpdateTicked += _onUpdateTickedHandler;
            result.UpdateTickedAdded = true;
        }

        /// <summary>
        /// Handles registration failure by logging error and cleaning up partial registrations.
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="result">Registration result tracking which events were registered</param>
        private void HandleRegistrationFailure(Exception ex, EventRegistrationResult result)
        {
            _monitor.Log($"Error registering visualization events: {ex.Message}", LogLevel.Error);
            _monitor.Log(
                $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                LogLevel.Trace
            );

            UnregisterPartialRegistrations(result);
            _eventsRegistered = false;
            UnregisterEvents();
        }

        /// <summary>
        /// Unregisters any partial event registrations that occurred before the failure.
        /// </summary>
        /// <param name="result">Registration result tracking which events were registered</param>
        private void UnregisterPartialRegistrations(EventRegistrationResult result)
        {
            if (result.ButtonPressedAdded && _onButtonPressedHandler != null)
                _helper.Events.Input.ButtonPressed -= _onButtonPressedHandler;

            if (result.CursorMovedAdded && _onCursorMovedHandler != null)
                _helper.Events.Input.CursorMoved -= _onCursorMovedHandler;

            if (result.RenderingWorldAdded && _onRenderingWorldLayerHandler != null)
                _helper.Events.Display.RenderingWorld -= _onRenderingWorldLayerHandler;

            if (result.RenderedAdded && _onRenderedHandler != null)
                _helper.Events.Display.Rendered -= _onRenderedHandler;

            if (result.UpdateTickedAdded && _onUpdateTickedHandler != null)
                _helper.Events.GameLoop.UpdateTicked -= _onUpdateTickedHandler;
        }

        /// <summary>
        /// Tracks which event handlers were successfully registered during event registration.
        /// </summary>
        private sealed class EventRegistrationResult
        {
            public bool ButtonPressedAdded { get; set; }
            public bool CursorMovedAdded { get; set; }
            public bool RenderingWorldAdded { get; set; }
            public bool RenderedAdded { get; set; }
            public bool UpdateTickedAdded { get; set; }
        }

        /// <inheritdoc/>
        public void UnregisterEvents()
        {
            lock (_eventLock)
            {
                if (!_eventsRegistered)
                {
                    _monitor.Log(
                        "Visualization events are not registered, skipping unregistration.",
                        LogLevel.Trace
                    );
                    return;
                }

                try
                {
                    // Unregister events via SMAPI helper
                    if (_onButtonPressedHandler != null)
                    {
                        _helper.Events.Input.ButtonPressed -= _onButtonPressedHandler;
                    }
                    if (_onCursorMovedHandler != null)
                    {
                        _helper.Events.Input.CursorMoved -= _onCursorMovedHandler;
                    }
                    if (_onRenderingWorldLayerHandler != null)
                    {
                        _helper.Events.Display.RenderingWorld -= _onRenderingWorldLayerHandler;
                    }
                    if (_onRenderedHandler != null)
                    {
                        _helper.Events.Display.Rendered -= _onRenderedHandler;
                    }
                    if (_onUpdateTickedHandler != null)
                    {
                        _helper.Events.GameLoop.UpdateTicked -= _onUpdateTickedHandler;
                    }

                    _eventsRegistered = false;
                    _monitor.Log("Visualization events unregistered successfully.", LogLevel.Trace);

                    // Clear handler references
                    _onButtonPressedHandler = null;
                    _onCursorMovedHandler = null;
                    _onRenderingWorldLayerHandler = null;
                    _onRenderedHandler = null;
                    _onUpdateTickedHandler = null;
                }
                catch (Exception ex)
                {
                    _monitor.Log(
                        $"Error unregistering visualization events: {ex.Message}",
                        LogLevel.Error
                    );
                    _monitor.Log(
                        $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                        LogLevel.Trace
                    );
                }
            }
        }

        /// <summary>
        /// Gets the soil health value for a specific tile.
        /// </summary>
        /// <param name="locationName">The name of the game location</param>
        /// <param name="tile">The tile coordinates</param>
        /// <returns>The soil health value (0-100), or 0 if no data exists</returns>
        public float GetSoilHealth(string locationName, Vector2 tile)
        {
            try
            {
                // Validate tile coordinates
                if (!VisualizationHelpers.IsValidTile(tile))
                {
                    _monitor.Log($"Invalid tile coordinates {tile}, returning 0.", LogLevel.Trace);
                    return 0f;
                }

                // Get health from soil health service
                var health = _soilHealthService.GetSoilHealth(locationName, tile);

                // Clamp to valid range
                return VisualizationHelpers.ClampHealth(health);
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Error getting soil health for tile {tile}: {ex.Message}",
                    LogLevel.Error
                );
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
                return 0f;
            }
        }

        /// <summary>
        /// Gets the color corresponding to a soil health value.
        /// </summary>
        /// <param name="health">The soil health value (0-100)</param>
        /// <returns>Color representing the health level</returns>
        public Color GetColorForHealth(float health)
        {
            try
            {
                // Clamp health to valid range
                health = VisualizationHelpers.ClampHealth(health);

                // Get color from color mapper
                return _colorMapper.GetHealthColor(health);
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Error getting color for health {health}: {ex.Message}",
                    LogLevel.Error
                );
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
                return Color.Gray;
            }
        }

        /// <summary>
        /// Renders a tile overlay for a specific tile.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="location">The game location containing the tile</param>
        /// <param name="tile">The tile coordinates</param>
        /// <param name="health">The soil health value (0-100)</param>
        public void RenderTileOverlay(
            SpriteBatch spriteBatch,
            GameLocation location,
            Vector2 tile,
            float health
        )
        {
            try
            {
                // Check if visualization is enabled
                if (!_isEnabled)
                {
                    return;
                }

                // Delegate to tile overlay renderer
                _tileOverlayRenderer.RenderTileOverlay(spriteBatch, location, tile, health);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering tile overlay: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Renders all visible tile overlays in the current viewport.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="location">The game location containing the tiles</param>
        public void RenderAllVisibleOverlays(SpriteBatch spriteBatch, GameLocation location)
        {
            try
            {
                // Check if visualization is enabled
                if (!_isEnabled)
                {
                    return;
                }

                // Delegate to tile overlay renderer
                _tileOverlayRenderer.RenderAllVisibleOverlays(spriteBatch, location);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering all visible overlays: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Renders a hover tooltip showing soil health information.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="cursorPosition">The current cursor position in screen coordinates</param>
        /// <param name="health">The soil health value (0-100)</param>
        public void RenderHoverTooltip(
            SpriteBatch spriteBatch,
            Vector2 cursorPosition,
            float health
        )
        {
            try
            {
                // Check if visualization is enabled
                if (!_isEnabled)
                {
                    return;
                }

                // Delegate to tooltip renderer
                _tooltipRenderer.RenderHoverTooltip(spriteBatch, cursorPosition, health);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering hover tooltip: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Renders hoe action feedback for a specific tile.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch for rendering</param>
        /// <param name="tilePosition">The tile position in screen coordinates</param>
        /// <param name="health">The soil health value (0-100)</param>
        public void RenderHoeFeedback(SpriteBatch spriteBatch, Vector2 tilePosition, float health)
        {
            try
            {
                // Check if visualization is enabled
                if (!_isEnabled)
                {
                    return;
                }

                // Delegate to tooltip renderer
                _tooltipRenderer.RenderHoeFeedback(spriteBatch, tilePosition, health);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error rendering hoe feedback: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Updates the current hover tile state.
        /// </summary>
        /// <param name="tile">The tile coordinates being hovered</param>
        public void UpdateHoverTile(Vector2 tile)
        {
            try
            {
                // Validate tile coordinates
                if (!VisualizationHelpers.IsValidTile(tile))
                {
                    _monitor.Log($"Invalid hover tile coordinates {tile}.", LogLevel.Trace);
                    return;
                }

                _currentHoverTile = tile;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error updating hover tile: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Gets the current hover tile.
        /// </summary>
        /// <returns>The current hover tile, or null if not hovering</returns>
        public Vector2? GetHoverTile()
        {
            return _currentHoverTile;
        }

        /// <summary>
        /// Records a hoe action on a specific tile.
        /// </summary>
        /// <param name="tile">The tile coordinates where hoe was used</param>
        public void RecordHoeAction(Vector2 tile)
        {
            try
            {
                // Validate tile coordinates
                if (!VisualizationHelpers.IsValidTile(tile))
                {
                    _monitor.Log($"Invalid hoe action tile coordinates {tile}.", LogLevel.Trace);
                    return;
                }

                _hoeActionTile = tile;
                _hoeActionStartTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error recording hoe action: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Clears the current hover tile state.
        /// </summary>
        private void ClearHoverState()
        {
            _currentHoverTile = null;
        }

        /// <summary>
        /// Clears the hoe action state.
        /// </summary>
        private void ClearHoeActionState()
        {
            _hoeActionTile = null;
            _hoeActionHealth = null;
            _hoeActionStartTime = null;
        }

        /// <summary>
        /// Checks if a tile has a HoeDirt terrain feature (tilled soil).
        /// </summary>
        /// <param name="location">The game location containing the tile</param>
        /// <param name="tile">The tile coordinates to check</param>
        /// <returns>True if the tile has HoeDirt (tilled soil), false otherwise</returns>
        private bool IsTileTilled(GameLocation location, Vector2 tile)
        {
            try
            {
                if (location == null)
                {
                    return false;
                }

                // Check if the tile has a HoeDirt terrain feature
                return location.terrainFeatures.TryGetValue(tile, out var feature)
                    && feature is HoeDirt;
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    $"Error checking if tile {tile} is tilled: {ex.Message}",
                    LogLevel.Trace
                );
                return false;
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles cursor movement events to track hover state for tooltips.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Cursor moved event arguments</param>
        private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
        {
            try
            {
                if (!_isEnabled || !_config.ShowHoverTooltips)
                {
                    return;
                }

                var cursorTile = e.NewPosition.Tile;
                _currentHoverTile = cursorTile;

                // Validate tile
                if (!VisualizationHelpers.IsValidTile(cursorTile))
                {
                    _currentHoverTile = null;
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnCursorMoved: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Handles button press events to detect hoe tool usage for feedback.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Button pressed event arguments</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            try
            {
                if (!_isEnabled || !_config.ShowHoeFeedback)
                {
                    return;
                }

                if (!e.Button.IsUseToolButton())
                {
                    return;
                }

                var tool = Game1.player?.CurrentTool;
                if (tool == null || tool.QualifiedItemId != "(T)Hoe")
                {
                    return;
                }

                var location = Game1.currentLocation;
                if (location == null)
                {
                    return;
                }

                // Record the actual tile the player targeted with the hoe
                var targetTile = e.Cursor.Tile;

                // Only show feedback for the targeted tile (and only if it's relevant)
                var tilesWithHealth = new List<(Vector2 tile, float health)>();

                if (VisualizationHelpers.IsValidTile(targetTile))
                {
                    var health = _soilHealthService.GetSoilHealth(
                        location.NameOrUniqueName,
                        targetTile
                    );

                    tilesWithHealth.Add((targetTile, health));

                    _hoeActionTile = targetTile;
                    _hoeActionHealth = health;
                    _hoeActionStartTime = DateTime.UtcNow;

                    _monitor.Log(
                        $"Hoe used on tile {_hoeActionTile} with soil health: {_hoeActionHealth}%",
                        LogLevel.Debug
                    );
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnButtonPressed: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Handles rendering world events to render tile overlays and hoe feedback.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Rendering world event arguments</param>
        private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
        {
            try
            {
                if (!_isEnabled || !_config.ShowTileOverlays)
                {
                    _monitor.Log(
                        "[RENDER] OnRenderingWorld - Early exit due to disabled state",
                        LogLevel.Trace
                    );
                    return;
                }

                var location = Game1.currentLocation;
                if (location == null)
                {
                    _monitor.Log(
                        "[RENDER] OnRenderingWorld - Current location is null",
                        LogLevel.Trace
                    );
                    return;
                }

                var player = Game1.player;
                if (player == null)
                {
                    _monitor.Log("[RENDER] OnRenderingWorld - Player is null", LogLevel.Trace);
                    return;
                }

                try
                {
                    // Render overlays with proper layer depth to appear under characters
                    // Use a depth value that places the overlay above ground but below characters
                    _tileOverlayRenderer.RenderAllVisibleOverlays(e.SpriteBatch, location);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Error rendering tile overlays: {ex.Message}", LogLevel.Error);
                    _monitor.Log(
                        $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                        LogLevel.Trace
                    );
                }

                // Render hoe feedback (flash effect and floating text) in world space
                if (_config.ShowHoeFeedback && _hoeActionTile.HasValue && _hoeActionHealth.HasValue)
                {
                    try
                    {
                        // Convert tile coordinates to world position (no viewport offset)
                        var worldPosition = new Vector2(
                            _hoeActionTile.Value.X * 64f,
                            _hoeActionTile.Value.Y * 64f
                        );
                        _tooltipRenderer.RenderHoeFeedback(
                            e.SpriteBatch,
                            worldPosition,
                            _hoeActionHealth.Value
                        );
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"Error rendering hoe feedback: {ex.Message}", LogLevel.Error);
                    }
                }

                // Log performance metrics periodically
                LogPerformanceIfNeeded();
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnRenderingWorld: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Handles rendered events to render tooltips.
        /// Renders tooltips in both outdoor and indoor locations (including Greenhouse).
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Rendered event arguments</param>
        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            try
            {
                var location = Game1.currentLocation;
                if (location == null)
                {
                    return;
                }

                // Render hover tooltip (works in both outdoor and indoor locations)
                if (_isEnabled && _config.ShowHoverTooltips && _currentHoverTile.HasValue)
                {
                    try
                    {
                        var cursorPosition = Game1.getMousePosition().ToVector2();

                        if (IsTileTilled(location, _currentHoverTile.Value))
                        {
                            var health = _soilHealthService.GetSoilHealth(
                                location.NameOrUniqueName,
                                _currentHoverTile.Value
                            );
                            _tooltipRenderer.RenderHoverTooltip(
                                e.SpriteBatch,
                                cursorPosition,
                                health
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log(
                            $"Error rendering hover tooltip: {ex.Message}",
                            LogLevel.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnRendered: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Handles update ticked events to update hover state and manage feedback timers.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Update ticked event arguments</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            try
            {
                // Clear hoe action feedback after duration
                if (_hoeActionStartTime.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - _hoeActionStartTime.Value).TotalMilliseconds;
                    if (elapsed > _config.GetHoeFeedbackDuration())
                    {
                        _hoeActionTile = null;
                        _hoeActionHealth = null;
                        _hoeActionStartTime = null;
                    }
                }

                // Update hover state
                if (!_isEnabled)
                {
                    _currentHoverTile = null;
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in OnUpdateTicked: {ex.Message}", LogLevel.Error);
                _monitor.Log(
                    $"Exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <summary>
        /// Logs performance metrics if the configured interval has passed.
        /// </summary>
        private void LogPerformanceIfNeeded()
        {
            try
            {
                if (DateTime.UtcNow - _lastPerformanceLog > _performanceLogInterval)
                {
                    _monitor.Log(
                        $"Performance: {_renderedTilesLastFrame} tiles rendered in last interval",
                        LogLevel.Trace
                    );
                    _renderedTilesLastFrame = 0;
                    _lastPerformanceLog = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error logging performance metrics: {ex.Message}", LogLevel.Trace);
            }
        }

        #endregion
    }
}
