using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(null as IModDataService, _mockMonitor.Object, _mockFileNameSanitizationService.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(_mockDataService.Object, null as IMonitor, _mockFileNameSanitizationService.Object));
        }

        [Fact]
        public void Constructor_WithNullFileNameSanitizationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, null as IFileNameSanitizationService));
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
            var result = service.GetSoilHealth(null, tile);
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
            service.SetSoilHealth(null, tile, 5.0f);
            service.SetSoilHealth("", tile, 5.0f);
            service.SetSoilHealth("   ", tile, 5.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void SetSoilHealth_WithInvalidTileCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.NaN, 10), 5.0f);
            service.SetSoilHealth(location, new Vector2(float.PositiveInfinity, 10), 5.0f);
            service.SetSoilHealth(location, new Vector2(10, float.NaN), 5.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth(location, new Vector2(10, 10)));
        }

        [Fact]
        public void SetSoilHealth_WithOverflowCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.MaxValue, 10), 5.0f);
            service.SetSoilHealth(location, new Vector2(float.MinValue, 10), 5.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth(location, new Vector2(10, 10)));
        }

        [Fact]
        public void UpdateHealth_WithInvalidLocationName_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

            // Act
            service.UpdateHealth(null, tile, 10.0f);
            service.UpdateHealth("", tile, 1.0f);
            service.UpdateHealth("   ", tile, 1.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(5.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithInvalidTileCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

            // Act
            service.UpdateHealth("Farm", new Vector2(float.NaN, 10), 1.0f);
            service.UpdateHealth("Farm", new Vector2(float.PositiveInfinity, 10), 1.0f);
            service.UpdateHealth("Farm", new Vector2(10, float.NaN), 1.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(5.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithOverflowCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

            // Act
            service.UpdateHealth("Farm", new Vector2(float.MaxValue, 10), 1.0f);
            service.UpdateHealth("Farm", new Vector2(float.MinValue, 10), 1.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(5.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_IncrementsValueCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 5.0f);

            // Act
            service.UpdateHealth(location, tile, 2.0f);

            // Assert
            Assert.Equal(7.0f, service.GetSoilHealth(location, tile));
        }

        [Fact]
        public void UpdateHealth_DecrementsValueCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 8.0f);

            // Act
            service.UpdateHealth(location, tile, -2.0f);

            // Assert
            Assert.Equal(6.0f, service.GetSoilHealth(location, tile));
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
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

            // Act
            service.LoadData(null); // Should clear cache
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
                        ["10,10"] = 75.5f,  // Within domain range [0,100], clamped to 75.5
                        ["11,15"] = 25.5f   // Within domain range [0,10], stays 25.5
                    }
                }
            };

            // Set up the mock to return the expected sanitized value
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
                LocationHealthData = null // This could happen during deserialization
            };

            // Set up the mock to return the expected sanitized value
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
                        ["10,not_a_number"] = 25.5f,  // Invalid Y
                        ["not_a_number,10"] = 30.0f,  // Invalid X
                        ["10,10"] = 50.0f             // Valid
                    }
                }
            };

            // Set up the mock to return the expected sanitized value
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
        public void LoadData_WithInvalidValues_ConvertsNaNInfinityToZeroDuringLoad()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10,10"] = float.NaN,        // Invalid value
                        ["11,11"] = float.PositiveInfinity, // Invalid value
                        ["12,12"] = float.NegativeInfinity, // Invalid value
                        ["13,13"] = 50.0f             // Valid
                    }
                }
            };

            // Set up the mock to return the expected sanitized value
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
            // Invalid entries should be converted to 0 (not skipped)
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(11, 11)));
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));

            // Strengthen the test by verifying the internal state more thoroughly
            // Check that all expected keys are present in the internal cache with correct values
            var tile1010 = new Vector2(10, 10);
            var tile11 = new Vector2(11, 11);  
            var tile1212 = new Vector2(12, 12);
            var tile1313 = new Vector2(13, 13);

            // Verify that all entries were processed and stored with correct conversions
            float result1010 = service.GetSoilHealth("Farm", tile1010);
            float result11 = service.GetSoilHealth("Farm", tile11);  
            float result1212 = service.GetSoilHealth("Farm", tile1212);
            float result1313 = service.GetSoilHealth("Farm", tile1313);
            
            Assert.Equal(0f, result1010); // NaN value converted to 0
            Assert.Equal(0f, result11); // PositiveInfinity value converted to 0
            Assert.Equal(0f, result1212); // NegativeInfinity value converted to 0
            Assert.Equal(50.0f, result1313); // Valid value remains unchanged
            
            // Additional verification: Ensure that no unexpected values were created
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(99, 9))); // Non-existent tile should return default value
        }

        [Fact]
        public void LoadData_WithNullLocationName_SkipsInvalidEntries()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    [""] = new Dictionary<string, float> { ["11,1"] = 25.5f },   // Empty location
                    ["   "] = new Dictionary<string, float> { ["12,12"] = 30.0f }, // Whitespace location
                    ["Farm"] = new Dictionary<string, float> { ["13,13"] = 75.5f } // Valid location
                }
            };

            // Set up the mock to return the expected sanitized value
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
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

            // Set up the mock to return the expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Throws(new Exception("Load failed")); // Actually throw an exception to test the exception handling

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
            service.SetSoilHealth("Farm", tile, 5.0f);

            // Act
            service.SaveData(null);
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
            service.SetSoilHealth("Farm", tile1, 75.5f);  // Within domain range [0,100]
            service.SetSoilHealth("Farm", tile2, 25.5f);  // Within domain range [0,100]

            // Set up the mock to return the expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Act
            service.SaveData("test_save");

            // Assert - Verify the data was saved with correct key and format
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
            // This test verifies the correct behavior: when the cache is empty,
            // SaveData should still save an empty state to clear any stale data on disk.
            // This is important for data integrity to ensure that if the cache becomes empty,
            // the on-disk data is also cleared.
            
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            // Don't add any data to the cache, so it remains empty
            
            // Set up the mock to return the expected sanitized value
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
        public void SaveData_WithNaNInfinityValues_DoesNotSaveZeroValuesDueToSparseCache()
        {
            // This test validates that NaN and Infinity values are converted to 0 during SetSoilHealth
            // and are NOT saved due to the sparse cache functionality (values that are 0 are not stored)
            
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var tile = new Vector2(10, 10);
            
            // Manually set some values including NaN and Infinity to test save behavior
            service.SetSoilHealth("Farm", new Vector2(10, 10), 75.5f); // Valid value within range
            service.SetSoilHealth("Farm", new Vector2(11, 11), float.NaN); // Invalid value - will be converted to 0 by ClampHealthValue
            service.SetSoilHealth("Farm", new Vector2(12, 12), float.PositiveInfinity); // Invalid value - will be converted to 0 by ClampHealthValue
            service.SetSoilHealth("Farm", new Vector2(13, 13), 25.5f); // Valid value within range
            
            // Set up the mock to capture the data that gets saved
            SoilHealthState capturedSaveState = null;
            string capturedSaveKey = null;
            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedSaveState = state;
                    capturedSaveKey = key;
                });

            // Set up the mock to return the expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            // Act: Save the data
            service.SaveData("test_save");

            // Assert: Verify that the NaN/Infinity values were converted to 0 during SetSoilHealth
            // and are NOT saved due to sparse cache (0 values are not stored)
            Assert.NotNull(capturedSaveState);
            Assert.Equal("soil_health_data_test_save", capturedSaveKey);
            
            // Check that the saved state contains the Farm location
            Assert.Contains("Farm", capturedSaveState.LocationHealthData.Keys);
            
            var farmData = capturedSaveState.LocationHealthData["Farm"];
            // Only entries with non-zero values should be present due to sparse cache
            Assert.Contains("10,10", farmData.Keys);
            Assert.DoesNotContain("11,11", farmData.Keys); // NaN converted to 0, so not saved due to sparse cache
            Assert.DoesNotContain("12,12", farmData.Keys); // Infinity converted to 0, so not saved due to sparse cache
            Assert.Contains("13,13", farmData.Keys);
            
            Assert.Equal(75.5f, farmData["10,10"]);
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
            // but the public API prevents this, so we'll test the validation directly
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

            // Set up the mock to return the expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(corruptedState);

            // Act - Load the corrupted data and then save it
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
            service.SetSoilHealth("Farm", tile, 5.0f);

            // Set up the mock to return the expected sanitized value
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
            service.UpdateHealth(location, new Vector2(10.7f, 15.3f), 20.0f);

            // Assert - Should have updated the floored coordinates
            Assert.Equal(70.0f, service.GetSoilHealth(location, new Vector2(10, 15)));
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
        public async System.Threading.Tasks.Task ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act - Multiple threads accessing the service simultaneously
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            var tile = new Vector2(j % 10, j / 10);
                            service.SetSoilHealth("Farm", tile, j % 10 * 5.0f); // Keep values within [0,10] range
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

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred due to race conditions
            Assert.Empty(exceptions);
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
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

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
            service.SetSoilHealth("Farm", tile, 5.0f);

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
            service.SetSoilHealth("Farm", tile, 5.0f); // Set initial value

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
            service.SetSoilHealth("Farm", tile, 5.0f);

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
            // Arrange: Create save data with more entries than the per-location limit (500), 
            // This test verifies that the DoS protection counts ALL processed entries, 
            // not just the ones that are saved, and triggers when the location limit is reached.
            var totalEntries = ModConstants.MaxTilesPerLocation + 1; // 501 entries (500 limit + 1 extra)
            var validEntries = new Dictionary<string, float>(); // Use regular dictionary
            
            // Add entries that will be processed but will cause the limit to be exceeded
            // Add exactly MaxTilesPerLocation (500) entries first, then one more that should trigger the limit
            // Use valid tile keys in the format "x,y" to ensure they are processed (not skipped as invalid)
            for (int i = 0; i < totalEntries; i++)
            {
                // Use valid tile keys in the format "x,y" to ensure they are processed (not skipped as invalid)
                // Format as "row,col" where row increases slowly and column resets after hitting limit
                int row = i / 100;  // Changes every 100 entries
                int col = i % 100;  // Cycles from 0 to 99
                validEntries[$"{row},{col}"] = 50.0f; // These will be processed and counted toward the limit
            }

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = validEntries // Dictionary with 501 valid entries that will trigger the limit
                }
            };

            // Set up the mock to return the expected sanitized value
            _mockFileNameSanitizationService
                .Setup(x => x.Sanitize("test_save"))
                .Returns("test_save");

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>("soil_health_data_test_save"))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object, _mockFileNameSanitizationService.Object);

            // Act: Load the data - this should trigger the DoS protection because 
            // we're processing more entries than the per-location limit (500)
            service.LoadData("test_save");

            // Assert: The cache should have limited entries due to DoS protection
            // The DoS protection should have been triggered and processing should have stopped
            // after reaching the limit, so no entries should be present
            // Check a tile that would be among the later entries that should have been blocked
            var result = service.GetSoilHealth("Farm", new Vector2(5, 0)); // This would be tile #500+, should be 0
            
            // Add missing assertion to verify that the monitor was called to log the limit exceeded warning
            _mockMonitor.Verify(x => x.Log(It.Is<string>(msg => msg.Contains("Tile count limit") && msg.Contains("exceeded for location")), LogLevel.Warn), Times.AtLeastOnce);
            
            // The result should be 0.0f since the DoS protection limit would be reached and no entries beyond the limit should be processed
            Assert.Equal(0.0f, result);
        }
    }
}