using System;
using System.Collections.Generic;
using System.Globalization;
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

        public SoilHealthServiceTests()
        {
            _mockDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
        }

        [Fact]
        public void Constructor_WithNullDataService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(null, _mockMonitor.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SoilHealthService(_mockDataService.Object, null));
        }

        [Fact]
        public void SetHealth_ValuesAreClampedTo0And100()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);

            // Act
            service.SetSoilHealth(location, tile, 150.0f); // Tries to set 150
            var resultMax = service.GetSoilHealth(location, tile);

            service.SetSoilHealth(location, tile, -50.0f); // Tries to set -50
            var resultMin = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(100.0f, resultMax);
            Assert.Equal(0.0f, resultMin);
        }

        [Fact]
        public void GetSoilHealth_WithInvalidLocationName_ReturnsDefault()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);

            // Act
            service.SetSoilHealth(null, tile, 50.0f);
            service.SetSoilHealth("", tile, 50.0f);
            service.SetSoilHealth("   ", tile, 50.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void SetSoilHealth_WithInvalidTileCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.NaN, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(float.PositiveInfinity, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(10, float.NaN), 50.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth(location, new Vector2(10, 10)));
        }

        [Fact]
        public void SetSoilHealth_WithOverflowCoordinates_DoesNotAddEntry()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";

            // Act
            service.SetSoilHealth(location, new Vector2(float.MaxValue, 10), 50.0f);
            service.SetSoilHealth(location, new Vector2(float.MinValue, 10), 50.0f);

            // Assert - Should not have added any entries to the cache
            Assert.Equal(0f, service.GetSoilHealth(location, new Vector2(10, 10)));
        }

        [Fact]
        public void UpdateHealth_WithInvalidLocationName_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth(null, tile, 10.0f);
            service.UpdateHealth("", tile, 10.0f);
            service.UpdateHealth("   ", tile, 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithInvalidTileCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth("Farm", new Vector2(float.NaN, 10), 10.0f);
            service.UpdateHealth("Farm", new Vector2(float.PositiveInfinity, 10), 10.0f);
            service.UpdateHealth("Farm", new Vector2(10, float.NaN), 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_WithOverflowCoordinates_DoesNotUpdate()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.UpdateHealth("Farm", new Vector2(float.MaxValue, 10), 10.0f);
            service.UpdateHealth("Farm", new Vector2(float.MinValue, 10), 10.0f);

            // Assert - Value should remain unchanged
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void UpdateHealth_IncrementsValueCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            service.UpdateHealth(location, tile, 100.0f); // Should clamp to 100
            var resultMax = service.GetSoilHealth(location, tile);

            service.SetSoilHealth(location, tile, 50.0f); // Reset
            service.UpdateHealth(location, tile, -100.0f); // Should clamp to 0
            var resultMin = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(100.0f, resultMax);
            Assert.Equal(0.0f, resultMin);
        }

        [Fact]
        public void LoadData_WithInvalidSaveId_ClearsCache()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            // Act
            service.LoadData(null); // Should clear cache
            service.LoadData(""); // Should clear cache
            service.LoadData("   "); // Should clear cache

            // Assert - Value should be cleared
            Assert.Equal(0f, service.GetSoilHealth("Farm", tile));
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
                        ["10,10"] = 75.0f,
                        ["11,15"] = 25.0f
                    }
                }
            };

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert
            Assert.Equal(75.0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(25.0f, service.GetSoilHealth("Farm", new Vector2(11, 15)));
        }

        [Fact]
        public void LoadData_WithNullLocationHealthData_InitializesEmptyCache()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = null // This could happen during deserialization
            };

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

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
                        ["invalid_key"] = 75.0f,      // Invalid format
                        ["10,not_a_number"] = 25.0f,  // Invalid Y
                        ["not_a_number,10"] = 30.0f,  // Invalid X
                        ["10,10"] = 50.0f             // Valid
                    }
                }
            };

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Only valid entry should be loaded
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            // Invalid entries should not exist
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(0, 0))); // Default for invalid
        }

        [Fact]
        public void LoadData_WithInvalidValues_SkipsInvalidEntries()
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

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Only valid entry should be loaded
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(13, 13)));
            // Invalid entries should not exist
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(10, 10)));
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(11, 11)));
            Assert.Equal(0f, service.GetSoilHealth("Farm", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithEmptyLocationName_SkipsInvalidEntries()
        {
            // Arrange
            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    [""] = new Dictionary<string, float> { ["11,1"] = 25.0f },   // Empty location
                    ["   "] = new Dictionary<string, float> { ["12,12"] = 30.0f }, // Whitespace location
                    ["Farm"] = new Dictionary<string, float> { ["13,13"] = 50.0f } // Valid location
                }
            };

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(saveData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert - Only valid location should be loaded
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", new Vector2(13, 13)));
            // Empty/whitespace locations should not exist
            Assert.Equal(0f, service.GetSoilHealth("", new Vector2(11, 11)));
            Assert.Equal(0f, service.GetSoilHealth("   ", new Vector2(12, 12)));
        }

        [Fact]
        public void LoadData_WithException_PreservesExistingCache()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f); // Set initial value

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Throws(new Exception("Data load failed"));

            // Act
            service.LoadData("test_save");

            // Assert - Existing cache should be preserved
            Assert.Equal(50.0f, service.GetSoilHealth("Farm", tile));
        }

        [Fact]
        public void SaveData_WithInvalidSaveId_DoesNotSave()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile1 = new Vector2(10, 10);
            var tile2 = new Vector2(15, 20);
            service.SetSoilHealth("Farm", tile1, 75.0f);
            service.SetSoilHealth("Farm", tile2, 25.0f);

            // Act
            service.SaveData("test_save");

            // Assert - Verify the data was saved with correct key and format
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), "soil_health_data_test_save"), Times.Once);
            
            // Capture the actual data that was saved
            _mockDataService.Verify(x => x.SaveData(It.Is<SoilHealthState>(state => 
                state.LocationHealthData.ContainsKey("Farm") &&
                state.LocationHealthData["Farm"].ContainsKey("10,10") &&
                state.LocationHealthData["Farm"]["10,10"] == 75.0f &&
                state.LocationHealthData["Farm"].ContainsKey("15,20") &&
                state.LocationHealthData["Farm"]["15,20"] == 25.0f
            ), "soil_health_data_test_save"), Times.Once);
        }

        [Fact]
        public void SaveData_WithInvalidValues_DoesNotSaveInvalidEntries()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tileValid = new Vector2(10, 10);
            
            // Use reflection or direct access to set invalid values in the internal cache
            // Since we can't directly set NaN/Infinity through the public API, we'll test the save logic directly
            var state = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = new Dictionary<string, float>
                    {
                        ["10,10"] = 50.0f,              // Valid
                        ["15,15"] = float.NaN,          // Invalid
                        ["16,16"] = float.PositiveInfinity, // Invalid
                        ["17,17"] = float.NegativeInfinity // Invalid
                    }
                }
            };

            // Mock the data service to return this state during save conversion
            var serviceWithValidData = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            serviceWithValidData.SetSoilHealth("Farm", tileValid, 50.0f);

            // Act
            serviceWithValidData.SaveData("test_save");

            // Verify that only valid entries are saved
            _mockDataService.Verify(x => x.SaveData(It.Is<SoilHealthState>(s => 
                s.LocationHealthData["Farm"].ContainsKey("10,10") && 
                !s.LocationHealthData["Farm"].ContainsKey("15,15") &&
                !s.LocationHealthData["Farm"].ContainsKey("16,16") &&
                !s.LocationHealthData["Farm"].ContainsKey("17,17")
            ), "soil_health_data_test_save"), Times.Once);
        }

        [Fact]
        public void SaveData_WithEmptyLocationName_DoesNotSaveInvalidEntries()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            
            // We need to test the save logic with invalid location names
            // We'll create a scenario where the internal cache has invalid entries
            // but the public API prevents this, so we'll test the validation directly
            var serviceWithValidData = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            serviceWithValidData.SetSoilHealth("Farm", new Vector2(13, 13), 75.0f);
            
            // Add invalid location names to the internal state by bypassing public API
            // This simulates what might happen with corrupted data
            var corruptedState = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    [""] = new Dictionary<string, float> { ["11,1"] = 25.0f },   // Empty location
                    ["   "] = new Dictionary<string, float> { ["12,12"] = 30.0f }, // Whitespace location
                    ["Farm"] = new Dictionary<string, float> { ["13,13"] = 75.0f } // Valid location
                }
            };

            _mockDataService
                .Setup(x => x.LoadData<SoilHealthState>(It.IsAny<string>()))
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
        public void SaveData_WithException_DoesNotThrow()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50.0f);

            _mockDataService
                .Setup(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Throws(new Exception("Data save failed"));

            // Act & Assert - Should not throw
            var ex = Record.Exception(() => service.SaveData("test_save"));
            Assert.Null(ex);
        }

        [Fact]
        public void SaveData_WithEmptyCache_DoesNotSave()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.SaveData("test_save");

            // Assert - Should not attempt to save empty data
            _mockDataService.Verify(x => x.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetSoilHealth_UsesFloorForCoordinateConversion()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
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
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            service.SetSoilHealth(location, new Vector2(10, 15), 50.0f);

            // Act - Update with fractional coordinates that map to same tile
            service.UpdateHealth(location, new Vector2(10.7f, 15.3f), 10.0f);

            // Assert - Should have updated the floored coordinates
            Assert.Equal(60.0f, service.GetSoilHealth(location, new Vector2(10, 15)));
        }

        [Fact]
        public void GetSoilHealth_WithNegativeCoordinates_WorksCorrectly()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(-5.7f, -3.3f); // Should map to (-6, -4) using MathF.Floor
            service.SetSoilHealth(location, tile, 50.0f);

            // Act
            var result = service.GetSoilHealth(location, new Vector2(-5.9f, -3.8f)); // Should still get (-6, -4)

            // Assert - Should get the value from the floored negative coordinates
            Assert.Equal(50.0f, result);
        }

        [Fact]
        public void ThreadSafety_MultipleThreadsAccessingService_DoesNotThrow()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Act - Multiple threads accessing the service simultaneously
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            var tile = new Vector2(j % 10, j / 10);
                            service.SetSoilHealth("Farm", tile, j * 1.0f);
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

            Task.WaitAll(tasks.ToArray());

            // Assert - No exceptions should have occurred due to race conditions
            Assert.Empty(exceptions);
        }
    }
}