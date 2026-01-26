using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Unit tests for VisualizationConfig service.
    /// Tests configuration management, persistence, and validation.
    /// </summary>
    public class VisualizationConfigTests
    {
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly VisualizationConfig _config;

        public VisualizationConfigTests()
        {
            _mockMonitor = new Mock<IMonitor>();
            _mockModDataService = new Mock<IModDataService>();
            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns((VisualizationConfigData?)null);

            _config = new VisualizationConfig(_mockMonitor.Object, _mockModDataService.Object);
        }

        [Fact]
        public void Constructor_NullMonitor_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VisualizationConfig(null!, _mockModDataService.Object)
            );
        }

        [Fact]
        public void Constructor_NullModDataService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VisualizationConfig(_mockMonitor.Object, null!)
            );
        }

        [Fact]
        public void Constructor_LoadsConfiguration()
        {
            // Assert - Constructor should call Load
            _mockModDataService.Verify(
                d => d.LoadData<VisualizationConfigData>("visualization_config"),
                Times.Once
            );
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            // Assert
            Assert.True(_config.ShowTileOverlays);
            Assert.True(_config.ShowHoverTooltips);
            Assert.True(_config.ShowHoeFeedback);
            Assert.Equal(0.3f, _config.OverlayOpacity);
            Assert.False(_config.UseCustomColors);
        }

        [Fact]
        public void DefaultColors_AreCorrect()
        {
            // Assert
            Assert.Equal(new Color(139, 69, 19), _config.PoorHealthColor);
            Assert.Equal(new Color(218, 165, 32), _config.ModerateHealthColor);
            Assert.Equal(new Color(85, 107, 47), _config.HealthyHealthColor);
        }

        [Fact]
        public void Load_NoExistingData_UsesDefaults()
        {
            // Arrange
            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns((VisualizationConfigData?)null);

            // Act
            _config.Load();

            // Assert
            Assert.True(_config.ShowTileOverlays);
            Assert.True(_config.ShowHoverTooltips);
            Assert.True(_config.ShowHoeFeedback);
            Assert.Equal(0.3f, _config.OverlayOpacity);
        }

        [Fact]
        public void Load_WithValidData_LoadsConfiguration()
        {
            // Arrange
            var configData = new VisualizationConfigData
            {
                ShowTileOverlays = false,
                ShowHoverTooltips = false,
                ShowHoeFeedback = false,
                OverlayOpacity = 0.5f,
                UseCustomColors = true,
                PoorHealthColor = new LivingRoots.Services.ColorData(new Color(255, 0, 0)),
                ModerateHealthColor = new LivingRoots.Services.ColorData(new Color(0, 255, 0)),
                HealthyHealthColor = new LivingRoots.Services.ColorData(new Color(0, 0, 255)),
            };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert - Note: Load is called in constructor, so we need to verify the data was loaded
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("loaded successfully")), LogLevel.Trace),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public void Load_WithInvalidOpacity_ClampsToValidRange()
        {
            // Arrange
            var configData = new VisualizationConfigData
            {
                OverlayOpacity = 1.5f, // Invalid: > 1.0
            };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert - Opacity should be clamped to 1.0
            Assert.InRange(_config.OverlayOpacity, 0.0f, 1.0f);
        }

        [Fact]
        public void Load_WithNegativeOpacity_ClampsToZero()
        {
            // Arrange
            var configData = new VisualizationConfigData
            {
                OverlayOpacity = -0.5f, // Invalid: < 0.0
            };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert - Opacity should be clamped to 0.0
            Assert.Equal(0.0f, _config.OverlayOpacity);
        }

        [Fact]
        public void Load_WithNullColors_UsesDefaults()
        {
            // Arrange
            var configData = new VisualizationConfigData
            {
                PoorHealthColor = null,
                ModerateHealthColor = null,
                HealthyHealthColor = null,
            };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert - Should use default colors
            Assert.Equal(new Color(139, 69, 19), _config.PoorHealthColor);
            Assert.Equal(new Color(218, 165, 32), _config.ModerateHealthColor);
            Assert.Equal(new Color(85, 107, 47), _config.HealthyHealthColor);
        }

        [Fact]
        public void Save_SavesConfiguration()
        {
            // Act
            _config.Save();

            // Assert
            _mockModDataService.Verify(
                d => d.SaveData(It.IsAny<VisualizationConfigData>(), "visualization_config"),
                Times.Once
            );
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("saved successfully")), LogLevel.Trace),
                Times.Once
            );
        }

        [Fact]
        public void Save_WithException_LogsError()
        {
            // Arrange
            _mockModDataService
                .Setup(d => d.SaveData(It.IsAny<VisualizationConfigData>(), It.IsAny<string>()))
                .Throws(new Exception("Save failed"));

            // Act
            _config.Save();

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Error saving")), LogLevel.Error),
                Times.Once
            );
        }

        [Fact]
        public void ResetToDefaults_ResetsAllValues()
        {
            // Act
            _config.ResetToDefaults();

            // Assert
            Assert.True(_config.ShowTileOverlays);
            Assert.True(_config.ShowHoverTooltips);
            Assert.True(_config.ShowHoeFeedback);
            Assert.Equal(0.3f, _config.OverlayOpacity);
            Assert.False(_config.UseCustomColors);
            Assert.Equal(new Color(139, 69, 19), _config.PoorHealthColor);
            Assert.Equal(new Color(218, 165, 32), _config.ModerateHealthColor);
            Assert.Equal(new Color(85, 107, 47), _config.HealthyHealthColor);
        }

        [Fact]
        public void ResetToDefaults_SavesConfiguration()
        {
            // Act
            _config.ResetToDefaults();

            // Assert
            _mockModDataService.Verify(
                d => d.SaveData(It.IsAny<VisualizationConfigData>(), "visualization_config"),
                Times.Once
            );
        }

        [Fact]
        public void ResetToDefaults_LogsInfo()
        {
            // Act
            _config.ResetToDefaults();

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("reset to defaults")), LogLevel.Info),
                Times.Once
            );
        }

        [Fact]
        public void GetHoeFeedbackDuration_Returns1000()
        {
            // Act
            long duration = _config.GetHoeFeedbackDuration();

            // Assert
            Assert.Equal(1000, duration);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(0.1f)]
        [InlineData(0.5f)]
        [InlineData(0.9f)]
        [InlineData(1.0f)]
        public void OpacityValidation_ValidValues_AreAccepted(float opacity)
        {
            // Arrange
            var configData = new VisualizationConfigData { OverlayOpacity = opacity };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert
            Assert.Equal(opacity, _config.OverlayOpacity);
        }

        [Theory]
        [InlineData(-1.0f)]
        [InlineData(-0.5f)]
        [InlineData(1.5f)]
        [InlineData(2.0f)]
        [InlineData(10.0f)]
        public void OpacityValidation_InvalidValues_AreClamped(float opacity)
        {
            // Arrange
            var configData = new VisualizationConfigData { OverlayOpacity = opacity };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert - Should be clamped to [0.0, 1.0]
            Assert.InRange(_config.OverlayOpacity, 0.0f, 1.0f);
        }

        [Fact]
        public void ColorValidation_ValidColor_IsLoaded()
        {
            // Arrange
            var validColor = new LivingRoots.Services.ColorData(new Color(100, 150, 200, 255));
            var configData = new VisualizationConfigData { PoorHealthColor = validColor };

            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Returns(configData);

            // Act
            _config.Load();

            // Assert
            Assert.Equal(100, _config.PoorHealthColor.R);
            Assert.Equal(150, _config.PoorHealthColor.G);
            Assert.Equal(200, _config.PoorHealthColor.B);
            Assert.Equal(255, _config.PoorHealthColor.A);
        }

        [Fact]
        public void Load_WithException_LogsErrorAndUsesDefaults()
        {
            // Arrange
            _mockModDataService
                .Setup(d => d.LoadData<VisualizationConfigData>(It.IsAny<string>()))
                .Throws(new Exception("Load failed"));

            // Act
            _config.Load();

            // Assert
            _mockMonitor.Verify(
                m => m.Log(It.Is<string>(s => s.Contains("Error loading")), LogLevel.Error),
                Times.Once
            );
            // Should still have default values
            Assert.True(_config.ShowTileOverlays);
            Assert.Equal(0.3f, _config.OverlayOpacity);
        }

        [Fact]
        public void ResetToDefaults_WithException_LogsError()
        {
            // Arrange
            _mockModDataService
                .Setup(d => d.SaveData(It.IsAny<VisualizationConfigData>(), It.IsAny<string>()))
                .Throws(new Exception("Save failed"));

            // Act
            _config.ResetToDefaults();

            // Assert
            _mockMonitor.Verify(
                m =>
                    m.Log(
                        It.Is<string>(s => s.Contains("Error saving visualization configuration")),
                        LogLevel.Error
                    ),
                Times.Once
            );
        }

        [Fact]
        public void ColorData_Constructor_CreatesCorrectColor()
        {
            // Arrange
            var color = new Color(128, 64, 32, 255);

            // Act
            var colorData = new ColorData(color);

            // Assert
            Assert.Equal(128, colorData.R);
            Assert.Equal(64, colorData.G);
            Assert.Equal(32, colorData.B);
            Assert.Equal(255, colorData.A);
        }

        [Fact]
        public void ColorData_DefaultAlpha_Is255()
        {
            // Arrange
            var colorData = new ColorData(new Color(100, 100, 100));

            // Assert
            Assert.Equal(255, colorData.A);
        }
    }
}
