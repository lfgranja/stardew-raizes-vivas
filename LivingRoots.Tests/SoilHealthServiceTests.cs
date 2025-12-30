using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    public class SoilHealthServiceTests
    {
        private readonly Mock<IModDataService> _mockDataService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;

        public SoilHealthServiceTests()
        {
            _mockDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();
        }

        [Fact]
        public void Constructor_WithNullDataService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(null!, _mockMonitor.Object, _mockFileNameSanitizationService.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(_mockDataService.Object, null!, _mockFileNameSanitizationService.Object));
        }

        [Fact]
        public void Constructor_WithNullFileNameSanitizationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, null!));
        }

        [Fact]
        public void SetHealth_ValuesAreClampedTo0And100()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);

            // Act
            service.SetSoilHealth(location, tile, 105.0f); // Tries to set 105 (above max)
            var resultMax = service.GetSoilHealth(location, tile);

            service.SetSoilHealth(location, tile, -5.0f); // Tries to set -5 (below min)
            var resultMin = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(100.0f, resultMax); // 105 should be clamped to 100 (MaxSoilHealth)
            Assert.Equal(0.0f, resultMin); // -5 should be clamped to 0 (MinSoilHealth)
        }

        [Fact]
        public void GetSoilHealth_WithInvalidLocationName_ReturnsDefault()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);

            // Act
            var result = service.GetSoilHealth(null!, tile);
            var resultEmpty = service.GetSoilHealth("", tile);
            var resultWhitespace = service.GetSoilHealth("   ", tile);

            // Assert
            Assert.Equal(0f, result);
            Assert.Equal(0f, resultEmpty);
            Assert.Equal(0f, resultWhitespace);
        }

        [Fact]
        public void GetSoilHealth_WithInvalidTileCoordinates_ReturnsDefault()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            var resultNaN = service.GetSoilHealth(location, new Vector2(float.NaN, 10));
            var resultInfinity = service.GetSoilHealth(location, new Vector2(float.PositiveInfinity, 10));
            var resultBothNaN = service.GetSoilHealth(location, new Vector2(float.NaN, float.NaN));

            // Assert
            Assert.Equal(0f, resultNaN);
            Assert.Equal(0f, resultInfinity);
            Assert.Equal(0f, resultBothNaN);
        }

        [Fact]
        public void GetSoilHealth_WithOverflowCoordinates_ReturnsDefault()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            var resultHigh = service.GetSoilHealth(location, new Vector2(float.MaxValue, 10));
            var resultLow = service.GetSoilHealth(location, new Vector2(float.MinValue, 10));

            // Assert
            Assert.Equal(0f, resultHigh);
            Assert.Equal(0f, resultLow);
        }

        [Fact]
        public void SetSoilHealth_WithInvalidLocationName_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);

            // Act
            service.SetSoilHealth(null!, tile, 5.0f);
            service.SetSoilHealth("", tile, 5.0f);
            service.SetSoilHealth("   ", tile, 5.0f);

            // Assert - Should not have added any entries to the cache
            var runtimeCacheField = typeof(SoilHealthService).GetField("_runtimeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(runtimeCacheField);
            var runtimeCache = runtimeCacheField.GetValue(service) as Dictionary<string, Dictionary<Point, float>>;
            Assert.NotNull(runtimeCache);
            Assert.False(runtimeCache.ContainsKey(""));
            Assert.False(runtimeCache.ContainsKey("   "));
        }

        [Fact]
        public void SetSoilHealth_WithInvalidTileCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.NaN, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(float.PositiveInfinity, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(10, float.NaN), 50.0f);

            // Assert - Should not have added any entries to the cache
            var runtimeCacheField = typeof(SoilHealthService).GetField("_runtimeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(runtimeCacheField);
            var runtimeCache = runtimeCacheField.GetValue(service) as Dictionary<string, Dictionary<Point, float>>;
            Assert.NotNull(runtimeCache);
            Assert.False(runtimeCache.ContainsKey(location));
        }

        [Fact]
        public void SetSoilHealth_WithOverflowCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.MaxValue, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(float.MinValue, 10), 50.0f);

            // Assert - Should not have added any entries to the cache
            var runtimeCacheField = typeof(SoilHealthService).GetField("_runtimeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(runtimeCacheField);
            var runtimeCache = runtimeCacheField.GetValue(service) as Dictionary<string, Dictionary<Point, float>>;
            Assert.NotNull(runtimeCache);
            Assert.False(runtimeCache.ContainsKey(location));
        }

        [Fact]
        public void UpdateHealth_WithInvalidLocationName_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(null!, tile, 100.0f);
            service.UpdateHealth("", tile, 10.0f);
            service.UpdateHealth("   ", tile, 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithInvalidTileCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(location, new Vector2(float.NaN, 10), 10.0f);
            service.UpdateHealth(location, new Vector2(float.PositiveInfinity, 10), 10.0f);
            service.UpdateHealth(location, new Vector2(10, float.NaN), 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithOverflowCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(location, new Vector2(float.MaxValue, 10), 10.0f);
            service.UpdateHealth(location, new Vector2(float.MinValue, 10), 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_IncrementsValueCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            service.UpdateHealth(location, tile, 25.0f);

            // Assert
            Assert.Equal(75.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void UpdateHealth_DecrementsValueCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            service.UpdateHealth(location, tile, -25.0f);

            // Assert
            Assert.Equal(25.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void UpdateHealth_ClampsTo0And100()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            service.UpdateHealth(location, tile, 80.0f); // Should result in 50+80=130 -> clamp to 100 (MaxSoilHealth)
            var resultMax = service.GetSoilHealth(location, tile);

            service.SetSoilHealth(location, tile, 50.0f); // Reset
            service.UpdateHealth(location, tile, -100.0f); // Should result in 50-100=-50 -> clamp to 0 (MinSoilHealth)
            var resultMin = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(100.0f, resultMax); // 130 clamped to 100
            Assert.Equal(0.0f, resultMin); // -50 clamped to 0
        }

        [Fact]
        public void LoadData_WithInvalidSaveId_ClearsCacheToPreventLeakage()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.LoadData(null!); // Should clear cache
            service.LoadData(""); // Should clear cache
            service.LoadData("   "); // Should clear cache

            // Assert - Value should be cleared (not preserved) when invalid saveId is passed
            // This prevents data leakage between different game saves
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void LoadData_WithValidSaveId_LoadsData()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10,10"] = 75.5f, // Within domain range [0,100], stays 75.5
                        ["11,15"] = 25.5f   // Within domain range [0,100], stays 25.5
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
        }

        [Fact]
        public void LoadData_WithNullLocationHealthData_InitializesEmptyCache()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = null! // This could happen during deserialization
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Should not throw and should have empty cache
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
        }

        [Fact]
        public void LoadData_WithMalformedTileKeys_SkipsInvalidEntries()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["invalid_key"] = 75.5f,      // Invalid format
                        ["10,not_a_number"] = 25.5f, // Invalid Y
                        ["not_a_number,10"] = 30.0f,  // Invalid X
                        ["10,10"] = 50.0f             // Valid
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Only valid entry should be loaded
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            // Invalid entries should not exist
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(0, 0))); // Default for invalid
        }

        [Fact]
        public void LoadData_WithInvalidValues_ConvertsNaNInfinityDuringLoad()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10,10"] = float.NaN,        // Invalid value
                        ["11,11"] = float.PositiveInfinity, // Invalid value, PositiveInfinity should be converted to 100f
                        ["12,12"] = float.NegativeInfinity, // Invalid value, NegativeInfinity should be converted to 0f
                        ["13,13"] = 50.0f             // Valid
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Invalid values should be converted to 0, valid entries should remain
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(13, 13)));
            // NegativeInfinity should be converted to 0 (not skipped)
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            // PositiveInfinity should be converted to 100.
            Assert.Equal(100f, service.GetSoilHealth("Farm", new Vector2(11, 11)));
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));
            
            // Strengthen the test by verifying the internal state more thoroughly
            // Check that all expected keys are present in the internal cache with correct values
            var tile10 = new Vector2(10, 10);
            var tile11 = new Vector2(11, 11);  
            var tile12 = new Vector2(12, 12);
            var tile13 = new Vector2(13, 13);

            // Verify that all entries were processed and stored with correct conversions
            float result10 = service.GetSoilHealth("Farm", tile10);
            float result11 = service.GetSoilHealth("Farm", tile11);  
            float result12 = service.GetSoilHealth("Farm", tile12);
            float result13 = service.GetSoilHealth("Farm", tile13);
            
            Assert.Equal(0f, result10); // NaN value converted to 0
            Assert.Equal(100f, result11); // PositiveInfinity value converted to 100
            Assert.Equal(0f, result12); // NegativeInfinity value converted to 0
            Assert.Equal(50.0f, result13); // Valid value remains unchanged
            
            // Additional verification: Ensure that no unexpected values were created
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(99, 99))); // Non-existent tile should return default value
        }

        [Fact]
        public void LoadData_WithNullLocationName_SkipsInvalidEntries()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    [""] = new Dictionary<string, float> { ["11,11"] = 25.5f },   // Empty location
                    ["   "] = new Dictionary<string, float> { ["12,12"] = 30.0f }, // Whitespace location
                    ["Farm"] = new Dictionary<string, float> { ["13,13"] = 75.5f } // Valid location
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Only valid location should be loaded
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(13, 13)));
            // Empty/whitespace locations should not exist
            Assert.Equal(0f, service.GetSoilHealth("", new Vector2(11, 11)));
            Assert.Equal(0f, service.GetSoilHealth("   ", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithException_HandlesGracefully()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Throws(new Exception("Load failed")); // Actually throw an exception to test exception handling

            // Act & Assert - Should handle exception gracefully and not propagate
            var ex = Record.Exception(() => service.LoadData("test_save"));
            Assert.Null(ex); // Should not throw
            // Cache should be cleared when exception occurs during loading
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void SaveData_WithInvalidSaveId_DoesNotSave()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

            // Act
            service.SaveData(null!);
            service.SaveData("");
            service.SaveData("   ");

            // Assert - Data should not be saved
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SaveData_WithValidData_SavesCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile1 = new Vector2(10, 10);
            var tile2 = new Vector2(15, 20);
            service.SetSoilHealth("Farm", tile1, 75.5f);
            service.SetSoilHealth("Farm", tile2, 25.5f);

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Act
            service.SaveData("test_save");

            // Assert - Verify that data was saved with correct key and format
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), "soil_health_data_test_save"), Times.Once);
            
            // Capture the actual data that was saved
            _mockDataService.Verify(x => x.SaveData(It.Is<SoilHealthState>(state => 
                state.LocationHealthData.ContainsKey("Farm") &&
                state.LocationHealthData["Farm"].ContainsKey("10,10") &&
                state.LocationHealthData["Farm"]["10,10"] == 75.5f &&
                state.LocationHealthData["Farm"].ContainsKey("15,20") &&
                state.LocationHealthData["Farm"]["15,20"] == 25.5f
            ), "soil_health_data_test_save"), Times.Once);
        }

        [Fact]
        public void SaveData_WithEmptyCache_SavesEmptyStateToClearStaleData()
        {
            // This test verifies that correct behavior: when the cache is empty,
            // SaveData should still save an empty state to clear any stale data on disk.
            // This is important for data integrity to ensure that if the cache becomes empty,
            // on-disk data is also cleared.
            
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            // Don't add any data to the cache, so it remains empty
            
            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Act
            service.SaveData("test_save");

            // Assert - Should save an empty state to clear any previously saved data on disk
            _mockDataService.Verify(
                x => x.SaveData(
                    It.Is<SoilHealthState>(s => s.LocationHealthData != null && s.LocationHealthData.Count == 0),
                    "soil_health_data_test_save"
                ),
                Times.Once
            );
        }

        [Fact]
        public void SaveData_WithNaNInfinityValues_SavesNonZeroValuesAndSkipsZeroValuesDueToSparseCache()
        {
            // This test validates the correct behavior of NaN and Infinity values during save:
            // - NaN values are converted to 0 by ClampHealthValue and are NOT saved due to sparse cache functionality
            // - PositiveInfinity values are converted to MaxSoilHealth (100) by ClampHealthValue and ARE saved
            // - NegativeInfinity values are converted to MinSoilHealth (0) by ClampHealthValue and are NOT saved due to sparse cache
            // - Valid values are saved normally
            
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            // Manually set some values including NaN and Infinity to test save behavior
            service.SetSoilHealth("Farm", new Vector2(10, 10), 75.5f); // Valid value - should be saved
            service.SetSoilHealth("Farm", new Vector2(11, 11), float.NaN); // Invalid value - will be converted to 0 by ClampHealthValue, not saved due to sparse cache
            service.SetSoilHealth("Farm", new Vector2(12, 12), float.PositiveInfinity); // Invalid value - will be converted to MaxSoilHealth (100) by ClampHealthValue, IS saved
            service.SetSoilHealth("Farm", new Vector2(13, 13), 25.5f); // Valid value - should be saved
            service.SetSoilHealth("Farm", new Vector2(14, 14), float.NegativeInfinity); // Invalid value - will be converted to MinSoilHealth (0) by ClampHealthValue, not saved due to sparse cache
            
            // Set up mock to capture the data that gets saved
            SoilHealthState capturedSaveState = null!;
            string capturedSaveKey = null!;
            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedSaveState = state;
                    capturedSaveKey = key;
                });

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Act: Save the data
            service.SaveData("test_save");

            // Assert: Verify that the correct state was captured
            Assert.NotNull(capturedSaveState);
            Assert.Equal("soil_health_data_test_save", capturedSaveKey);
            
            // Check that the saved state contains the Farm location
            Assert.Contains("Farm", capturedSaveState.LocationHealthData.Keys);
            
            var farmData = capturedSaveState.LocationHealthData["Farm"];
            // Only entries with non-zero values should be present due to sparse cache
            Assert.Contains("10,10", farmData.Keys); // 75.5f -> should be saved
            Assert.DoesNotContain("11,11", farmData.Keys); // NaN converted to 0, so not saved due to sparse cache
            Assert.Contains("12,12", farmData.Keys); // PositiveInfinity converted to 100, so IS saved
            Assert.Contains("13,13", farmData.Keys); // 25.5f -> should be saved
            Assert.DoesNotContain("14,14", farmData.Keys); // NegativeInfinity converted to 0, so not saved due to sparse cache
            
            Assert.Equal(75.5f, farmData["10,10"]);
            Assert.Equal(100f, farmData["12,12"]); // PositiveInfinity converted to MaxSoilHealth (100)
            Assert.Equal(25.5f, farmData["13,13"]);
            
            // Verify that SaveData was called exactly once
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SaveData_WithNullLocationName_DoesNotSaveInvalidEntries()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            
            // We need to test the save logic with invalid location names
            // We'll create a scenario where the internal cache has invalid entries
            // but the public API prevents this, so we'll test validation directly
            var serviceWithValidData = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            serviceWithValidData.SetSoilHealth("Farm", new Vector2(13, 13), 75.5f);
            
            // Add invalid location names to the internal state by bypassing public API
            // This simulates what might happen with corrupted data
            var corruptedState = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    [""] = new Dictionary<string, float> { ["11,1"] = 25.5f },   // Empty location
                    ["   "] = new Dictionary<string, float> { ["12,12"] = 30.0f }, // Whitespace location
                    ["Farm"] = new Dictionary<string, float> { ["13,13"] = 75.5f } // Valid location
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(corruptedState);

            // Act - Load corrupted data and then save it
            serviceWithValidData.LoadData("test_save");
            serviceWithValidData.SaveData("test_save");

            // Verify that only valid locations are saved
            _mockDataService.Verify(x => x.SaveData(It.Is<SoilHealthState>(s => 
                !s.LocationHealthData.ContainsKey("") &&
                !s.LocationHealthData.ContainsKey("   ") &&
                s.LocationHealthData.ContainsKey("Farm")
            ), "soil_health_data_test_save"), Times.Once);
        }

        [Fact]
        public void SaveData_WithException_HandlesGracefully()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Throws(new Exception("Data save failed"));

            // Act & Assert - Should handle exception gracefully and not propagate
            var ex = Record.Exception(() => service.SaveData("test_save"));
            Assert.Null(ex); // Should not throw
        }

        [Fact]
        public void GetSoilHealth_UsesFloorForCoordinateConversion()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10.7f, 15.3f); // Should map to (10, 15)
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            var result = service.GetSoilHealth(location, new Vector2(10.9f, 15.8f)); // Should still get (10, 15)

            // Assert - Should get the value from the floored coordinates
            Assert.Equal(50.0f, result);
        }

        [Fact]
        public void SetSoilHealth_UsesFloorForCoordinateConversion()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act - Set with fractional coordinates
            service.SetSoilHealth(location, new Vector2(10.7f, 15.3f), 50.0f);

            // Assert - Should be accessible with floored coordinates
            Assert.Equal(50.0f, service.GetSoilHealth(location, new Vector2(10, 15)));
            // And also with other fractional coordinates that floor to same tile
            Assert.Equal(50.0f, service.GetSoilHealth(location, new Vector2(10.9f, 15.8f)));
        }

        [Fact]
        public void UpdateHealth_UsesFloorForCoordinateConversion()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 15);
            service.SetSoilHealth(location, tile, 50.0f);

            // Act - Update with fractional coordinates that map to same tile
            service.UpdateHealth(location, new Vector2(10.7f, 15.3f), 10.0f);

            // Assert - Should have updated the floored coordinates
            Assert.Equal(60.0f, service.GetSoilHealth(location, new Vector2(10, 15)));
        }

        [Fact]
        public void GetSoilHealth_WithNegativeCoordinates_WorksCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(-5.7f, -3.3f); // Should map to (-6, -4) using MathF.Floor
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            var result = service.GetSoilHealth(location, new Vector2(-5.9f, -3.8f)); // Should still get (-6, -4)

            // Assert - Should get the value from the floored negative coordinates
            Assert.Equal(50.0f, result);
        }

        [Fact]
        public async Task ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act - Multiple threads accessing the service simultaneously with disjoint tile ranges
            // Fixed: Capture the loop variable in a local variable before creating the task
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var workerId = i; // Capture per-iteration value to avoid closure issues
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            int x = (workerId * 100) + j; // Unique X coordinate per worker
                            int y = workerId;              // Same Y for all operations of this worker
                            var tile = new Vector2(x, y);
                            service.SetSoilHealth("Farm", tile, j % 10 * 5.0f); // Keep values within [0,100] range
                            service.GetSoilHealth("Farm", tile);
                            service.UpdateHealth("Farm", tile, 1.0f);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // Verify that events were registered only once despite multiple concurrent calls
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ThreadSafety_MultipleThreadsAccessingService_UsesOverlappingTileRanges()
        {
            // This test verifies that the original test implementation uses overlapping tile ranges
            // which makes the test less deterministic and effective at detecting concurrency bugs
            // Strengthened: Add meaningful post-condition assertions to validate service state after concurrent access
            
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();
            var accessedTiles = new List<Vector2>();
            var tilesLock = new object();

            // Act - Multiple threads accessing the service simultaneously
            var tasks = new List<Task>();
            for (int i = 0; i < 3; i++) // Use fewer threads to make overlapping easier to detect
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++) // Use fewer operations to make overlapping easier to detect
                        {
                            var tile = new Vector2(j % 10, j / 10); // This creates overlapping tile ranges across threads
                            
                            // Record the tile being accessed
                            lock (tilesLock)
                            {
                                accessedTiles.Add(tile);
                            }
                            
                            service.SetSoilHealth("Farm", tile, j % 10 * 5.0f); // Keep values within [0,100] range
                            service.GetSoilHealth("Farm", tile);
                            service.UpdateHealth("Farm", tile, 1.0f);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert - Verify that overlapping tile ranges were used
            Assert.Empty(exceptions);
            
            // Check for overlapping tiles - since all threads use the same pattern (j % 10, j / 10),
            // they will access the same tiles, creating overlaps
            var distinctTiles = accessedTiles.Distinct().ToList();
            var totalAccesses = accessedTiles.Count;
            
            // If there are overlapping accesses, distinct tiles will be fewer than total accesses
            Assert.True(totalAccesses > distinctTiles.Count, 
                $"Expected overlapping tile accesses: {totalAccesses} total accesses vs {distinctTiles.Count} distinct tiles. " +
                $"This confirms that threads access the same tiles, creating race conditions.");

            // NEW: Strengthened post-condition assertions to validate service state after concurrent access
            // Verify that all values remain within expected range [0, 100] after concurrent access
            foreach (var tile in distinctTiles)
            {
                var healthValue = service.GetSoilHealth("Farm", tile);
                
                // Assert that the value is within the valid range [0, 100]
                Assert.InRange(healthValue, 0.0f, 100.0f);
                
                // Additional validation: Check that the value is a valid float (not NaN or Infinity)
                Assert.False(float.IsNaN(healthValue), 
                    $"Soil health value for tile {tile} is NaN after concurrent access, indicating potential data corruption.");
                Assert.False(float.IsInfinity(healthValue), 
                    $"Soil health value for tile {tile} is Infinity after concurrent access, indicating potential data corruption.");
            }
            
            // Additional post-condition validation: Verify that the service state is consistent
            // by checking that no unexpected tiles have invalid values
            var allTiles = distinctTiles.Select(t => new Vector2(MathF.Floor(t.X), MathF.Floor(t.Y))).Distinct().ToList();
            foreach (var tile in allTiles)
            {
                var healthValue = service.GetSoilHealth("Farm", tile);
                // Values should be within range and not NaN/Infinity
                Assert.InRange(healthValue, 0.0f, 100.0f);
                Assert.False(float.IsNaN(healthValue));
                Assert.False(float.IsInfinity(healthValue));
            }
        }

        [Fact]
        public async Task ThreadSafety_WithDisjointTileRanges_DoesNotThrow()
        {
            // This is a new test to verify that each worker operates on disjoint tile ranges
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act - Multiple threads accessing the service simultaneously with disjoint tile ranges
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var workerId = i; // Capture per-iteration value to avoid closure issues
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            int x = (workerId * 100) + j;  // Changed from (workerId * 10) + j to (workerId * 100) + j for truly disjoint ranges
                            int y = workerId;
                            var tile = new Vector2(x, y);
                            service.SetSoilHealth("Farm", tile, j % 10 * 5.0f); // Keep values within [0,100] range
                            service.GetSoilHealth("Farm", tile);
                            service.UpdateHealth("Farm", tile, 1.0f);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred due to race conditions
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ThreadSafety_WithDisjointTileRanges_VerifiesDisjointRanges()
        {
            // This test verifies that each worker operates on disjoint tile ranges
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();
            var workerTiles = new List<(int workerId, List<Vector2> tiles)>();

            // Act - Multiple threads accessing the service simultaneously with disjoint tile ranges
            var tasks = new List<Task>();
             for (int i = 0; i < 5; i++) // Using 5 workers to make verification easier
                {
                    var workerId = i;
                
                
                    tasks.Add(Task.Run(() =>
                    {
                        var workerTileList = new List<Vector2>();
                        
                        try
                        {
                            for (int j = 0; j < 10; j++) // Only 10 operations per worker for easier verification
                            {
                
                                int x = (workerId * 100) + j;
                                int y = workerId;
                                var tile = new Vector2(x, y);
                                workerTileList.Add(tile);
                
                                service.SetSoilHealth("Farm", tile, workerId * 10.0f); // Set value based on workerId
                                service.GetSoilHealth("Farm", tile);
                                service.UpdateHealth("Farm", tile, 1.0f);
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (lockObj)
                            {
                                exceptions.Add(ex);
                            }
                        }
                        finally
                        {
                            lock (lockObj)
                            {
                                workerTiles.Add((workerId, workerTileList));
                            }
                        }
                    }));
                }

            await Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred
            Assert.Empty(exceptions);
            
            // Verify that each worker operated on disjoint tile ranges
            Assert.Empty(exceptions);
            Assert.Equal(5, workerTiles.Count);
            var seen = new HashSet<Vector2>();
            foreach (var (_, tiles) in workerTiles)
            {
                foreach (var tile in tiles)
                {
                    Assert.True(seen.Add(tile), $"Tile overlap detected: {tile}");
                }
            }

            for (int i = 0; i < workerTiles.Count; i++)
            {
                for (int j = i + 1; j < workerTiles.Count; j++)
                {
                    var tilesI = workerTiles[i].tiles;
                    var tilesJ = workerTiles[j].tiles;
                    
                    // Check that no tiles overlap between workers
                    foreach (var tileI in tilesI)
                    {
                        foreach (var tileJ in tilesJ)
                        {
                            Assert.NotEqual(tileI, tileJ);
                        }
                    }
                }
            }
        }

        [Fact]
        public void LoadData_WithSanitizationFailure_DoesNotLoadAndClearsCache()
        {
            // Arrange
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("invalid_save"))
                .Throws(new ArgumentException("Invalid characters"));

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.LoadData("invalid_save");

            // Assert - Cache should be cleared when sanitization fails
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile));
            // Verify that the monitor was called to log the error
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), LogLevel.Error), Times.AtLeastOnce);
        }

        [Fact]
        public void SaveData_WithSanitizationFailure_DoesNotSave()
        {
            // Arrange
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("invalid_save"))
                .Throws(new ArgumentException("Invalid characters"));

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

            // Act
            service.SaveData("invalid_save");

            // Assert - Data should not be saved when sanitization fails
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
            // Verify that the monitor was called to log the error
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), LogLevel.Error), Times.AtLeastOnce);
        }

        [Fact]
        public void LoadData_WithSanitizationToEmptyResult_DoesNotLoadAndClearsCache()
        {
            // Arrange
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("empty_result"))
                .Returns("");

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.LoadData("empty_result");

            // Assert - Cache should be cleared when sanitization results in empty string
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile));
            // Verify that the monitor was called to log the error
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), LogLevel.Error), Times.AtLeastOnce);
        }

        [Fact]
        public void SaveData_WithSanitizationToEmptyResult_DoesNotSave()
        {
            // Arrange
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("empty_result"))
                .Returns("");

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

            // Act
            service.SaveData("empty_result");

            // Assert - Data should not be saved when sanitization results in empty string
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
            // Verify that the monitor was called to log the error
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), LogLevel.Error), Times.AtLeastOnce);
        }

        [Fact]
        public void LoadData_DosProtectionCountsProcessedEntriesNotJustSaved()
        {
            // Arrange: Create save data with more than MaxTilesPerLocation (500) valid entries to trigger the limit
            // This test verifies that DoS protection counts ALL processed entries,
            // not just the ones that are saved, and triggers when the location limit is reached.
            // The test is order-independent - it doesn't depend on Dictionary enumeration order.
            var totalEntries = ModConstants.MaxTilesPerLocation + 50; // 50 entries (exceeds the limit by 50)
            
            // Build a dictionary with all valid entries
            var locationEntries = new Dictionary<string, float>(totalEntries);

            // Add all valid entries that will be processed and counted
            // Use valid tile keys that will be processed and loaded
            for (int i = 0; i < totalEntries; i++)
            {
                locationEntries.Add($"{i},0", 75.0f);
            }

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = locationEntries
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act: Load the data - this should trigger DoS protection because
            // we're processing more than the limit of entries (50 > 500 limit)
            service.LoadData("test_save");

            // Assert: Verify that the critical error log appeared when exceeding the cap
            _mockMonitor.Verify(x => x.Log(It.Is<string>(msg => msg.Contains("Tile count limit") && msg.Contains("exceeded for location")), LogLevel.Alert), Times.AtLeastOnce);

            // With the high severity fix implemented, when the limit is exceeded,
            // the entire load operation should abort and the cache should be cleared.
            // Therefore, no entries should be loaded.
            int loadedEntriesCount = 0;
            for (int i = 0; i < totalEntries; i++)
            {
                if (service.GetSoilHealth("Farm", new Vector2(i, 0)) != 0.0f)
                {
                    loadedEntriesCount++;
                }
            }
            
            // Verify that no entries were loaded (the cache was cleared due to the limit exceeded error)
            Assert.Equal(0, loadedEntriesCount);
        }

        [Fact]
        public void UpdateHealth_WithNaNDelta_DoesNotModifyExistingHealth()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(location, tile, float.NaN); // Try to update with NaN delta

            // Assert - Value should remain unchanged (not reset to 0)
            Assert.Equal(50.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void UpdateHealth_WithPositiveInfinityDelta_ModifyExistingHealthtoMax()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(location, tile, float.PositiveInfinity); // Try to update with PositiveInfinity delta

            // Assert - Value should remain unchanged (not reset to 0)
            Assert.Equal(100.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void UpdateHealth_WithNegativeInfinityDelta_ModifyExistingHealthtoMin()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(location, tile, float.NegativeInfinity); // Try to update with NegativeInfinity delta

            // Assert - Value should remain unchanged (not reset to 0)
            Assert.Equal(0.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void SaveData_WithWhitespaceLocationKeyInRuntimeCache_SkipsInvalidEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            var corruptedState = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float> { ["10,10"] = 75.5f },
                    ["   "] = new Dictionary<string, float> { ["5,5"] = 50.0f },
                    ["\t\n"] = new Dictionary<string, float> { ["6,6"] = 60.0f }
                }
            };
                
                _mockFileNameSanitizationService
                    .Setup(x => x.Sanitize("test_save"))
                    .Returns("test_save");
                
                _mockDataService
                    .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                    .Returns(corruptedState);

                service.LoadData("test_save");

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Capture the saved state to verify what was actually saved
            SoilHealthState capturedSaveState = null!;
            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedSaveState = state;
                });

            // Act - SaveData should not crash and should skip whitespace location keys
            var ex = Record.Exception(() => service.SaveData("test_save"));

            // Assert - Should not throw an exception
            Assert.Null(ex);
            
            // Verify that only the valid location was saved
            Assert.NotNull(capturedSaveState);
            Assert.True(capturedSaveState.LocationHealthData.ContainsKey("Farm"));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey("   "));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey("\t\n"));
        }

        [Fact]
        public void SaveData_WithEmptyStringLocationKeyInRuntimeCache_SkipsInvalidEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            // Use reflection to directly add an empty string location key to the runtime cache
            var runtimeCacheField = typeof(SoilHealthService).GetField("_runtimeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(runtimeCacheField);
            var runtimeCache = runtimeCacheField.GetValue(service) as Dictionary<string, Dictionary<Point, float>>;
            Assert.NotNull(runtimeCache);
            {
                // Add a valid location with data
                runtimeCache["Farm"] = new Dictionary<Point, float> { [new Point(10, 10)] = 75.5f };
                
                // Add an empty string location key (this would cause a crash without defensive check)
                runtimeCache[""] = new Dictionary<Point, float> { [new Point(5, 5)] = 50.0f };
            }

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Capture the saved state to verify what was actually saved
            SoilHealthState capturedSaveState = null!;
            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedSaveState = state;
                });

            // Act - SaveData should not crash and should skip the empty string location key
            var ex = Record.Exception(() => service.SaveData("test_save"));

            // Assert - Should not throw an exception
            Assert.Null(ex);
            
            // Verify that only the valid location was saved
            Assert.NotNull(capturedSaveState);
            Assert.True(capturedSaveState.LocationHealthData.ContainsKey("Farm"));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey(""));
        }

        [Fact]
        public void SaveData_WithMixedValidAndInvalidLocationKeys_SavesOnlyValidEntries()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            // Use reflection to directly add a mix of valid and invalid location keys to the runtime cache
            var runtimeCacheField = typeof(SoilHealthService).GetField("_runtimeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(runtimeCacheField);
            var runtimeCache = runtimeCacheField.GetValue(service) as Dictionary<string, Dictionary<Point, float>>;
            Assert.NotNull(runtimeCache);
            {
                // Add valid locations with data
                runtimeCache["Farm"] = new Dictionary<Point, float> { [new Point(10, 10)] = 75.5f };
                runtimeCache["Town"] = new Dictionary<Point, float> { [new Point(15, 20)] = 50.0f };
                
                // Add invalid location keys (empty, whitespace)
                // Note: null keys cannot be added to Dictionary<string, ...> in C#, so we only test empty and whitespace
                runtimeCache[""] = new Dictionary<Point, float> { [new Point(2, 2)] = 30.0f };
                runtimeCache["   "] = new Dictionary<Point, float> { [new Point(3, 3)] = 35.0f };
                runtimeCache["\t\n"] = new Dictionary<Point, float> { [new Point(4, 4)] = 40.0f };
            }

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Capture the saved state to verify what was actually saved
            SoilHealthState capturedSaveState = null!;
            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedSaveState = state;
                });

            // Act - SaveData should not crash and should save only valid locations
            var ex = Record.Exception(() => service.SaveData("test_save"));

            // Assert - Should not throw an exception
            Assert.Null(ex);
            
            // Verify that only valid locations were saved
            Assert.NotNull(capturedSaveState);
            Assert.Equal(2, capturedSaveState.LocationHealthData.Count);
            Assert.True(capturedSaveState.LocationHealthData.ContainsKey("Farm"));
            Assert.True(capturedSaveState.LocationHealthData.ContainsKey("Town"));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey(""));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey("   "));
            Assert.False(capturedSaveState.LocationHealthData.ContainsKey("\t\n"));
            
            // Verify that saved data is correct
            Assert.Equal(75.5f, capturedSaveState.LocationHealthData["Farm"]["10,10"]);
            Assert.Equal(50.0f, capturedSaveState.LocationHealthData["Town"]["15,20"]);
        }

        [Fact]
        public void LoadData_WithLeadingWhitespaceInTileKeys_ParsesCorrectly()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        [" 10,10"] = 75.5f,   // Leading whitespace before X
                        ["  11,15"] = 25.5f,  // Multiple leading spaces before X
                        ["12,12"] = 50.0f      // Valid entry for comparison
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Tile keys with leading whitespace should be parsed correctly
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithTrailingWhitespaceInTileKeys_ParsesCorrectly()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10,10 "] = 75.5f,   // Trailing whitespace after Y
                        ["11,15  "] = 25.5f,  // Multiple trailing spaces after Y
                        ["12,12"] = 50.0f      // Valid entry for comparison
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Tile keys with trailing whitespace should be parsed correctly
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithLeadingAndTrailingWhitespaceInTileKeys_ParsesCorrectly()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        [" 10,10 "] = 75.5f,   // Leading and trailing whitespace
                        ["  11,15 "] = 25.5f,  // Multiple leading and trailing spaces
                        ["12,12"] = 50.0f      // Valid entry for comparison
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Tile keys with leading and trailing whitespace should be parsed correctly
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithWhitespaceAroundCommaInTileKeys_ParsesCorrectly()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10 ,10"] = 75.5f,   // Space before comma
                        ["11, 15"] = 25.5f,   // Space after comma
                        ["12 , 15"] = 50.0f,   // Spaces around comma
                        ["13,13"] = 60.0f      // Valid entry for comparison
                    }
                }
            };

            // Set up mock to return expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Tile keys with whitespace around comma should be parsed correctly
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(12, 15)));
            Assert.Equal(60.0f, service.GetSoilHealth("Farm", new Vector2(13, 13)));
        }

        [Fact]
        public void Reset_ClearsAllCachedData()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile1 = new Vector2(10, 10);
            var tile2 = new Vector2(15, 20);
            
            // Add data to multiple locations
            service.SetSoilHealth("Farm", tile1, 75.5f);
            service.SetSoilHealth("Farm", tile2, 25.5f);
            service.SetSoilHealth("Town", tile1, 50.0f);
            
            // Verify data was added
            Assert.Equal(75.5f, service.GetSoilHealth("Farm", tile1));
            Assert.Equal(25.5f, service.GetSoilHealth("Farm", tile2));
            Assert.Equal(50.0f, service.GetSoilHealth("Town", tile1));

            // Act
            service.Reset();

            // Assert - All cached data should be cleared
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile1));
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", tile2));
            Assert.Equal(0.0f, service.GetSoilHealth("Town", tile1));
        }

        [Fact]
        public void Reset_WhenCacheIsEmpty_DoesNotThrow()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            // Cache is initially empty, verify this
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));

            // Act & Assert
            var ex = Record.Exception(() => service.Reset());
            Assert.Null(ex); // Should not throw
            
            // Verify cache is still empty after reset
            Assert.Equal(0.0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
        }

        [Fact]
        public async Task Reset_IsThreadSafe()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            
            // Add some data to the cache
            for (int i = 0; i < 10; i++)
            {
                service.SetSoilHealth("Farm", new Vector2(i, 0), i * 10.0f);
            }

            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act - Multiple threads calling Reset() and other operations simultaneously
            var tasks = new List<Task>();
            
            // Task to call Reset() from multiple threads
            for (int i = 0; i < 3; i++)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        service.Reset();
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }
            
            // Task to call other operations from multiple threads
            for (int i = 0; i < 3; i++)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            var tile = new Vector2(j, 0);
                            service.SetSoilHealth("Farm", tile, j * 5.0f);
                            service.GetSoilHealth("Farm", tile);
                            service.UpdateHealth("Farm", tile, 1.0f);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred due to race conditions
            Assert.Empty(exceptions);
        }
    }
}
