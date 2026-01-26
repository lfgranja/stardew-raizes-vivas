using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Integration tests for SoilHealthVisualizationService.
    /// Tests service functionality, event management, and state management.
    /// </summary>
    public class SoilHealthVisualizationServiceTests
    {
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<IVisualizationConfig> _mockConfig;
        private readonly Mock<IColorMapper> _mockColorMapper;
        private readonly Mock<ITileOverlayRenderer> _mockTileOverlayRenderer;
        private readonly Mock<ITooltipRenderer> _mockTooltipRenderer;
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IPlayerEvents> _mockPlayerEvents;
        private readonly Mock<IInputEvents> _mockInputEvents;
        private readonly Mock<IDisplayEvents> _mockDisplayEvents;
        private readonly Mock<IGameLoopEvents> _mockGameLoopEvents;
        private readonly SoilHealthVisualizationService _service;

        public SoilHealthVisualizationServiceTests()
        {
            _mockMonitor = new Mock<IMonitor>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockConfig = new Mock<IVisualizationConfig>();
            _mockColorMapper = new Mock<IColorMapper>();
            _mockTileOverlayRenderer = new Mock<ITileOverlayRenderer>();
            _mockTooltipRenderer = new Mock<ITooltipRenderer>();
            _mockHelper = new Mock<IModHelper>();
            _mockPlayerEvents = new Mock<IPlayerEvents>();
            _mockInputEvents = new Mock<IInputEvents>();
            _mockDisplayEvents = new Mock<IDisplayEvents>();
            _mockGameLoopEvents = new Mock<IGameLoopEvents>();

            // Setup Events property to return mock events
            _mockHelper
                .Setup(h => h.Events)
                .Returns(
                    new ModEvents(
                        _mockInputEvents.Object,
                        _mockPlayerEvents.Object,
                        _mockDisplayEvents.Object,
                        _mockGameLoopEvents.Object
                    )
                );

            _service = new SoilHealthVisualizationService(
                _mockMonitor.Object,
                _mockSoilHealthService.Object,
                _mockConfig.Object,
                _mockColorMapper.Object,
                _mockTileOverlayRenderer.Object,
                _mockTooltipRenderer.Object,
                _mockHelper.Object
            );
        }

        [Fact]
        public void Constructor_NullMonitor_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    null!,
                    _mockSoilHealthService.Object,
                    _mockConfig.Object,
                    _mockColorMapper.Object,
                    _mockTileOverlayRenderer.Object,
                    _mockTooltipRenderer.Object,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullSoilHealthService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    null!,
                    _mockConfig.Object,
                    _mockColorMapper.Object,
                    _mockTileOverlayRenderer.Object,
                    _mockTooltipRenderer.Object,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    _mockSoilHealthService.Object,
                    null!,
                    _mockColorMapper.Object,
                    _mockTileOverlayRenderer.Object,
                    _mockTooltipRenderer.Object,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullColorMapper_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    _mockSoilHealthService.Object,
                    _mockConfig.Object,
                    null!,
                    _mockTileOverlayRenderer.Object,
                    _mockTooltipRenderer.Object,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullTileOverlayRenderer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    _mockSoilHealthService.Object,
                    _mockConfig.Object,
                    _mockColorMapper.Object,
                    null!,
                    _mockTooltipRenderer.Object,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullTooltipRenderer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    _mockSoilHealthService.Object,
                    _mockConfig.Object,
                    _mockColorMapper.Object,
                    _mockTileOverlayRenderer.Object,
                    null!,
                    _mockHelper.Object
                )
            );
        }

        [Fact]
        public void Constructor_NullHelper_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SoilHealthVisualizationService(
                    _mockMonitor.Object,
                    _mockSoilHealthService.Object,
                    _mockConfig.Object,
                    _mockColorMapper.Object,
                    _mockTileOverlayRenderer.Object,
                    _mockTooltipRenderer.Object,
                    null!
                )
            );
        }

        [Fact]
        public void IsEnabled_Initially_ReturnsFalse()
        {
            // Act
            bool isEnabled = _service.IsEnabled;

            // Assert
            Assert.False(isEnabled);
        }

        [Fact]
        public void Enable_SetsIsEnabledToTrue()
        {
            // Act
            _service.Enable();

            // Assert
            Assert.True(_service.IsEnabled);
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("enabled")), LogLevel.Info),
                Times.Once
            );
        }

        [Fact]
        public void Enable_WhenAlreadyEnabled_DoesNotChangeState()
        {
            // Arrange
            _service.Enable();

            // Act
            _service.Enable();

            // Assert
            Assert.True(_service.IsEnabled);
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("already enabled")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void Disable_SetsIsEnabledToFalse()
        {
            // Arrange
            _service.Enable();

            // Act
            _service.Disable();

            // Assert
            Assert.False(_service.IsEnabled);
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("disabled")), LogLevel.Info),
                Times.Once
            );
        }

        [Fact]
        public void Disable_WhenAlreadyDisabled_DoesNotChangeState()
        {
            // Act
            _service.Disable();

            // Assert
            Assert.False(_service.IsEnabled);
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("already disabled")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void Disable_ClearsHoverState()
        {
            // Arrange
            _service.Enable();
            _service.UpdateHoverTile(new Vector2(5, 5));

            // Act
            _service.Disable();

            // Assert
            Assert.Null(_service.GetHoverTile());
        }

        [Fact]
        public void RegisterEvents_RegistersAllEvents()
        {
            // Act
            _service.RegisterEvents();

            // Assert - Verify that Events property was accessed (may be accessed multiple times)
            _mockHelper.Verify(h => h.Events, Times.AtLeastOnce);
            _mockMonitor.Verify(
                m =>
                    m.Log(It.Is<string>(s => s.Contains("registered successfully")), LogLevel.Info),
                Times.Once
            );
        }

        [Fact]
        public void RegisterEvents_WhenAlreadyRegistered_DoesNotRegisterAgain()
        {
            // Arrange
            _service.RegisterEvents();

            // Act
            _service.RegisterEvents();

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("already registered")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void UnregisterEvents_UnregistersAllEvents()
        {
            // Arrange
            _service.RegisterEvents();

            // Act
            _service.UnregisterEvents();

            // Assert
            _mockMonitor.Verify(
                m =>
                    m.Log(
                        It.Is<string>(s => s.Contains("unregistered successfully")),
                        LogLevel.Trace
                    ),
                Times.Once
            );
        }

        [Fact]
        public void UnregisterEvents_WhenNotRegistered_DoesNotUnregister()
        {
            // Act
            _service.UnregisterEvents();

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("not registered")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void UpdateHoverTile_SetsHoverTile()
        {
            // Arrange
            var tile = new Vector2(5, 10);

            // Act
            _service.UpdateHoverTile(tile);

            // Assert
            Assert.Equal(tile, _service.GetHoverTile());
        }

        [Fact]
        public void UpdateHoverTile_WithInvalidTile_DoesNotSetHoverTile()
        {
            // Arrange
            var invalidTile = new Vector2(float.NaN, 10);

            // Act
            _service.UpdateHoverTile(invalidTile);

            // Assert
            Assert.Null(_service.GetHoverTile());
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Invalid hover tile")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void GetHoverTile_WhenNotSet_ReturnsNull()
        {
            // Act
            var hoverTile = _service.GetHoverTile();

            // Assert
            Assert.Null(hoverTile);
        }

        [Fact]
        public void GetSoilHealth_ValidTile_ReturnsHealth()
        {
            // Arrange
            var tile = new Vector2(5, 10);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("TestLocation", tile)).Returns(75f);

            // Act
            float health = _service.GetSoilHealth("TestLocation", tile);

            // Assert
            Assert.Equal(75f, health);
        }

        [Fact]
        public void GetSoilHealth_InvalidTile_ReturnsZero()
        {
            // Arrange
            var invalidTile = new Vector2(float.NaN, 10);

            // Act
            float health = _service.GetSoilHealth("TestLocation", invalidTile);

            // Assert
            Assert.Equal(0f, health); // Returns 0f for invalid tile (visualization service error handling)
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Invalid tile")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void GetSoilHealth_NegativeHealth_ClampsToZero()
        {
            // Arrange
            var tile = new Vector2(5, 10);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("TestLocation", tile)).Returns(-10f);

            // Act
            float health = _service.GetSoilHealth("TestLocation", tile);

            // Assert
            Assert.Equal(0f, health); // Returns 0f when health is negative (clamped to 0 by VisualizationHelpers.ClampHealth)
        }

        [Fact]
        public void GetSoilHealth_HealthAbove100_ClampsTo100()
        {
            // Arrange
            var tile = new Vector2(5, 10);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("TestLocation", tile)).Returns(150f);

            // Act
            float health = _service.GetSoilHealth("TestLocation", tile);

            // Assert
            Assert.Equal(100f, health);
        }

        [Fact]
        public void GetColorForHealth_ValidHealth_ReturnsColor()
        {
            // Arrange
            var expectedColor = new Color(100, 150, 200);
            _mockColorMapper.Setup(c => c.GetHealthColor(50f)).Returns(expectedColor);

            // Act
            Color color = _service.GetColorForHealth(50f);

            // Assert
            Assert.Equal(expectedColor, color);
        }

        [Fact]
        public void RenderTileOverlay_WhenEnabled_RendersOverlay()
        {
            // Arrange
            _service.Enable();
            var location = It.IsAny<GameLocation>();
            var tile = new Vector2(5, 10);

            // Act
            _service.RenderTileOverlay(It.IsAny<SpriteBatch>(), location, tile, 75f);

            // Assert
            _mockTileOverlayRenderer.Verify(
                r => r.RenderTileOverlay(It.IsAny<SpriteBatch>(), location, tile, 75f),
                Times.Once
            );
        }

        [Fact]
        public void RenderTileOverlay_WhenDisabled_DoesNotRender()
        {
            // Arrange
            _service.Disable();
            var location = It.IsAny<GameLocation>();
            var tile = new Vector2(5, 10);

            // Act
            _service.RenderTileOverlay(It.IsAny<SpriteBatch>(), location, tile, 75f);

            // Assert
            _mockTileOverlayRenderer.Verify(
                r =>
                    r.RenderTileOverlay(
                        It.IsAny<SpriteBatch>(),
                        It.IsAny<GameLocation>(),
                        It.IsAny<Vector2>(),
                        It.IsAny<float>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public void RenderAllVisibleOverlays_WhenEnabled_RendersOverlays()
        {
            // Arrange
            _service.Enable();
            var location = It.IsAny<GameLocation>();

            // Act
            _service.RenderAllVisibleOverlays(It.IsAny<SpriteBatch>(), location);

            // Assert
            _mockTileOverlayRenderer.Verify(
                r => r.RenderAllVisibleOverlays(It.IsAny<SpriteBatch>(), location),
                Times.Once
            );
        }

        [Fact]
        public void RenderAllVisibleOverlays_WhenDisabled_DoesNotRender()
        {
            // Arrange
            _service.Disable();
            var location = It.IsAny<GameLocation>();

            // Act
            _service.RenderAllVisibleOverlays(It.IsAny<SpriteBatch>(), location);

            // Assert
            _mockTileOverlayRenderer.Verify(
                r => r.RenderAllVisibleOverlays(It.IsAny<SpriteBatch>(), It.IsAny<GameLocation>()),
                Times.Never
            );
        }

        [Fact]
        public void RenderHoverTooltip_WhenEnabled_RendersTooltip()
        {
            // Arrange
            _service.Enable();
            var cursorPosition = new Vector2(100, 100);

            // Act
            _service.RenderHoverTooltip(It.IsAny<SpriteBatch>(), cursorPosition, 75f);

            // Assert
            _mockTooltipRenderer.Verify(
                r => r.RenderHoverTooltip(It.IsAny<SpriteBatch>(), cursorPosition, 75f),
                Times.Once
            );
        }

        [Fact]
        public void RenderHoverTooltip_WhenDisabled_DoesNotRender()
        {
            // Arrange
            _service.Disable();
            var cursorPosition = new Vector2(100, 100);

            // Act
            _service.RenderHoverTooltip(It.IsAny<SpriteBatch>(), cursorPosition, 75f);

            // Assert
            _mockTooltipRenderer.Verify(
                r =>
                    r.RenderHoverTooltip(
                        It.IsAny<SpriteBatch>(),
                        It.IsAny<Vector2>(),
                        It.IsAny<float>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public void RenderHoeFeedback_WhenEnabled_RendersFeedback()
        {
            // Arrange
            _service.Enable();
            var tilePosition = new Vector2(320, 640);

            // Act
            _service.RenderHoeFeedback(It.IsAny<SpriteBatch>(), tilePosition, 75f);

            // Assert
            _mockTooltipRenderer.Verify(
                r => r.RenderHoeFeedback(It.IsAny<SpriteBatch>(), tilePosition, 75f),
                Times.Once
            );
        }

        [Fact]
        public void RenderHoeFeedback_WhenDisabled_DoesNotRender()
        {
            // Arrange
            _service.Disable();
            var tilePosition = new Vector2(320, 640);

            // Act
            _service.RenderHoeFeedback(It.IsAny<SpriteBatch>(), tilePosition, 75f);

            // Assert
            _mockTooltipRenderer.Verify(
                r =>
                    r.RenderHoeFeedback(
                        It.IsAny<SpriteBatch>(),
                        It.IsAny<Vector2>(),
                        It.IsAny<float>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public void RecordHoeAction_ValidTile_RecordsAction()
        {
            // Arrange
            var tile = new Vector2(5, 10);

            // Act
            _service.RecordHoeAction(tile);

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Invalid hoe action")), LogLevel.Trace),
                Times.Never
            );
        }

        [Fact]
        public void RecordHoeAction_InvalidTile_DoesNotRecord()
        {
            // Arrange
            var invalidTile = new Vector2(float.NaN, 10);

            // Act
            _service.RecordHoeAction(invalidTile);

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Invalid hoe action")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void GetSoilHealth_WithException_LogsErrorAndReturnsZero()
        {
            // Arrange
            _mockSoilHealthService
                .Setup(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
                .Throws(new Exception("Service error"));

            // Act
            float health = _service.GetSoilHealth("TestLocation", new Vector2(5, 10));

            // Assert
            Assert.Equal(0f, health); // Returns 0f when exception occurs (visualization service error handling)
            _mockMonitor.Verify(
                m =>
                    m.Log(
                        It.Is<string>(s => s.Contains("Error getting soil health")),
                        LogLevel.Error
                    ),
                Times.Once
            );
        }

        [Fact]
        public void GetColorForHealth_WithException_LogsErrorAndReturnsGray()
        {
            // Arrange
            _mockColorMapper
                .Setup(c => c.GetHealthColor(It.IsAny<float>()))
                .Throws(new Exception("Mapper error"));

            // Act
            Color color = _service.GetColorForHealth(50f);

            // Assert
            Assert.Equal(Color.Gray, color);
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Error getting color")), LogLevel.Error),
                Times.Once
            );
        }

        // Helper class for mocking ModEvents
        private class ModEvents : IModEvents
        {
            public IInputEvents Input { get; }
            public IPlayerEvents Player { get; }
            public IDisplayEvents Display { get; }
            public IGameLoopEvents GameLoop { get; }
            public IContentEvents Content { get; }
            public IMultiplayerEvents Multiplayer { get; }
            public IWorldEvents World { get; }
            public ISpecializedEvents Specialized { get; }

            public ModEvents(
                IInputEvents input,
                IPlayerEvents player,
                IDisplayEvents display,
                IGameLoopEvents gameLoop
            )
            {
                Input = input;
                Player = player;
                Display = display;
                GameLoop = gameLoop;
                Content = Mock.Of<IContentEvents>();
                Multiplayer = Mock.Of<IMultiplayerEvents>();
                World = Mock.Of<IWorldEvents>();
                Specialized = Mock.Of<ISpecializedEvents>();
            }
        }
    }
}
