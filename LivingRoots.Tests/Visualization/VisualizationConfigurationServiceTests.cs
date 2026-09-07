using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    /// <summary>
    /// Tests for the VisualizationConfigurationService.
    /// Verifies configuration loading, saving, reset, validation, and thread safety.
    /// </summary>
    public class VisualizationConfigurationServiceTests
    {
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;
        private readonly VisualizationConfigurationService _service;

        public VisualizationConfigurationServiceTests()
        {
            _mockModDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();

            // Default setup: sanitization returns the input unchanged
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize(It.IsAny<string?>()))
                .Returns<string?>(input => input);

            _service = new VisualizationConfigurationService(
                _mockModDataService.Object,
                _mockMonitor.Object,
                _mockFileNameSanitizationService.Object);
        }

        // ──────────────────────────────────────────────
        // GetConfiguration never null
        // ──────────────────────────────────────────────

        [Fact]
        public void GetConfiguration_ShouldReturnDefaultConfigInitially()
        {
            // Act
            var config = _service.GetConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.OverlaysEnabled);
            Assert.True(config.TooltipsEnabled);
            Assert.True(config.HoeFeedbackEnabled);
            Assert.Equal(0.5f, config.Opacity);
            Assert.True(config.ShowPatterns);
            Assert.Equal("auto", config.AccessibilityDegradation);
            Assert.Equal(new ColorDTO(255, 0, 0, 255), config.PoorColor);
            Assert.Equal(new ColorDTO(255, 255, 0, 255), config.ModerateColor);
            Assert.Equal(new ColorDTO(0, 255, 0, 255), config.HealthyColor);
            Assert.Equal(new ColorDTO(128, 128, 128, 255), config.UnknownColor);
        }

        // ──────────────────────────────────────────────
        // LoadConfiguration with valid JSON
        // ──────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_WithValidConfig_ShouldLoadCorrectly()
        {
            // Arrange
            var validConfig = new VisualizationConfiguration
            {
                OverlaysEnabled = false,
                TooltipsEnabled = false,
                HoeFeedbackEnabled = false,
                Opacity = 0.75f,
                ShowPatterns = false,
                AccessibilityDegradation = "never",
                PoorColor = new ColorDTO(100, 0, 0, 255),
                ModerateColor = new ColorDTO(0, 100, 0, 255),
                HealthyColor = new ColorDTO(0, 0, 100, 255),
                UnknownColor = new ColorDTO(50, 50, 50, 255)
            };

            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns(validConfig);

            // Act
            _service.LoadConfiguration("test_save");
            var config = _service.GetConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.False(config.OverlaysEnabled);
            Assert.False(config.TooltipsEnabled);
            Assert.False(config.HoeFeedbackEnabled);
            Assert.Equal(0.75f, config.Opacity);
            Assert.False(config.ShowPatterns);
            Assert.Equal("never", config.AccessibilityDegradation);
            Assert.Equal(new ColorDTO(100, 0, 0, 255), config.PoorColor);
            Assert.Equal(new ColorDTO(0, 100, 0, 255), config.ModerateColor);
            Assert.Equal(new ColorDTO(0, 0, 100, 255), config.HealthyColor);
            Assert.Equal(new ColorDTO(50, 50, 50, 255), config.UnknownColor);
        }

        // ──────────────────────────────────────────────
        // LoadConfiguration with invalid fields (FR-006)
        // ──────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_WithInvalidOpacity_ShouldReplaceWithDefault()
        {
            // Arrange
            var configWithInvalidOpacity = new VisualizationConfiguration
            {
                Opacity = 1.5f // Invalid: > 1.0
            };

            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns(configWithInvalidOpacity);

            // Act
            _service.LoadConfiguration("test_save");
            var config = _service.GetConfiguration();

            // Assert
            Assert.Equal(0.5f, config.Opacity); // Should be replaced with default
        }

        [Fact]
        public void LoadConfiguration_WithInvalidAccessibilityDegradation_ShouldReplaceWithDefault()
        {
            // Arrange
            var configWithInvalidDegradation = new VisualizationConfiguration
            {
                AccessibilityDegradation = "invalid_value"
            };

            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns(configWithInvalidDegradation);

            // Act
            _service.LoadConfiguration("test_save");
            var config = _service.GetConfiguration();

            // Assert
            Assert.Equal("auto", config.AccessibilityDegradation); // Should be replaced with default
        }

        [Fact]
        public void LoadConfiguration_WithInvalidFields_ShouldPreserveValidValues()
        {
            // Arrange
            var mixedConfig = new VisualizationConfiguration
            {
                OverlaysEnabled = false, // Valid - should be preserved
                Opacity = 2.0f, // Invalid - should be replaced
                AccessibilityDegradation = "never", // Valid - should be preserved
                ShowPatterns = false // Valid - should be preserved
            };

            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns(mixedConfig);

            // Act
            _service.LoadConfiguration("test_save");
            var config = _service.GetConfiguration();

            // Assert
            Assert.False(config.OverlaysEnabled); // Preserved
            Assert.Equal(0.5f, config.Opacity); // Replaced with default
            Assert.Equal("never", config.AccessibilityDegradation); // Preserved
            Assert.False(config.ShowPatterns); // Preserved
        }

        // ──────────────────────────────────────────────
        // LoadConfiguration with missing file (FR-006)
        // ──────────────────────────────────────────────

        [Fact]
        public void LoadConfiguration_WithMissingFile_ShouldUseDefaults()
        {
            // Arrange
            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns((VisualizationConfiguration)null);

            // Act
            _service.LoadConfiguration("test_save");
            var config = _service.GetConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.OverlaysEnabled);
            Assert.True(config.TooltipsEnabled);
            Assert.True(config.HoeFeedbackEnabled);
            Assert.Equal(0.5f, config.Opacity);
            Assert.True(config.ShowPatterns);
            Assert.Equal("auto", config.AccessibilityDegradation);
        }

        // ──────────────────────────────────────────────
        // SaveConfiguration persists
        // ──────────────────────────────────────────────

        [Fact]
        public void SaveConfiguration_ShouldPersistViaModDataService()
        {
            // Arrange
            var config = new VisualizationConfiguration
            {
                Opacity = 0.8f,
                AccessibilityDegradation = "notify"
            };
            _service.UpdateConfiguration(config);

            // Act
            _service.SaveConfiguration("test_save");

            // Assert
            _mockModDataService.Verify(
                x => x.SaveData<VisualizationConfiguration>(
                    It.Is<VisualizationConfiguration>(c => c.Opacity == 0.8f && c.AccessibilityDegradation == "notify"),
                    It.Is<string>(key => key.Contains("visualization_config"))),
                Times.Once);
        }

        [Fact]
        public void SaveConfiguration_ShouldUseCorrectKey()
        {
            // Act
            _service.SaveConfiguration("test_save");

            // Assert
            _mockModDataService.Verify(
                x => x.SaveData<VisualizationConfiguration>(
                    It.IsAny<VisualizationConfiguration>(),
                    It.Is<string>(key => key.StartsWith("visualization_config_"))),
                Times.Once);
        }

        // ──────────────────────────────────────────────
        // ResetToDefaults
        // ──────────────────────────────────────────────

        [Fact]
        public void ResetToDefaults_ShouldRestoreModConstantsValues()
        {
            // Arrange - first modify the config
            var customConfig = new VisualizationConfiguration
            {
                OverlaysEnabled = false,
                TooltipsEnabled = false,
                HoeFeedbackEnabled = false,
                Opacity = 0.99f,
                ShowPatterns = false,
                AccessibilityDegradation = "never",
                PoorColor = new ColorDTO(10, 10, 10, 10),
                ModerateColor = new ColorDTO(20, 20, 20, 20),
                HealthyColor = new ColorDTO(30, 30, 30, 30),
                UnknownColor = new ColorDTO(40, 40, 40, 40)
            };
            _service.UpdateConfiguration(customConfig);

            // Act
            _service.ResetToDefaults();
            var config = _service.GetConfiguration();

            // Assert
            Assert.True(config.OverlaysEnabled);
            Assert.True(config.TooltipsEnabled);
            Assert.True(config.HoeFeedbackEnabled);
            Assert.Equal(0.5f, config.Opacity);
            Assert.True(config.ShowPatterns);
            Assert.Equal("auto", config.AccessibilityDegradation);
            Assert.Equal(new ColorDTO(255, 0, 0, 255), config.PoorColor);
            Assert.Equal(new ColorDTO(255, 255, 0, 255), config.ModerateColor);
            Assert.Equal(new ColorDTO(0, 255, 0, 255), config.HealthyColor);
            Assert.Equal(new ColorDTO(128, 128, 128, 255), config.UnknownColor);
        }

        // ──────────────────────────────────────────────
        // UpdateConfiguration with null
        // ──────────────────────────────────────────────

        [Fact]
        public void UpdateConfiguration_WithNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.UpdateConfiguration(null));
        }

        // ──────────────────────────────────────────────
        // Thread-safe access
        // ──────────────────────────────────────────────

        [Fact]
        public void GetConfiguration_ConcurrentReads_ShouldNotThrow()
        {
            // Arrange
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            var config = _service.GetConfiguration();
                            Assert.NotNull(config);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void LoadConfiguration_ConcurrentWithGetConfiguration_ShouldNotThrow()
        {
            // Arrange
            var validConfig = new VisualizationConfiguration { Opacity = 0.75f };
            _mockModDataService
                .Setup(x => x.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
                .Returns(validConfig);

            var tasks = new List<Task>();
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        _service.LoadConfiguration("test_save");
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj) { exceptions.Add(ex); }
                    }
                }));

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var config = _service.GetConfiguration();
                        Assert.NotNull(config);
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj) { exceptions.Add(ex); }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Empty(exceptions);
        }
    }
}
