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
    public class SoilHealthServiceDosProtectionTests
    {
        private readonly Mock<IModDataService> _mockDataService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizationService;

        public SoilHealthServiceDosProtectionTests()
        {
            _mockDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();
        }

        [Fact]
        public void LoadData_WithPerLocationTileLimitExceeded_AbortsEntireLoad()
        {
            // Arrange: Create save data with more tiles than the per-location limit (500)
            var excessTiles = ModConstants.MaxTilesPerLocation + 10; // 510 tiles (exceeds limit by 10)

            var locationEntries = new Dictionary<string, float>(excessTiles);
            for (var i = 0; i < excessTiles; i++)
            {
                locationEntries.Add($"{i},0", 75.0f); // Valid tile keys that will be processed
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

            // Act: Load the data - this should trigger DoS protection and abort the entire load
            service.LoadData("test_save");

            // Assert: Verify that the warning log appeared when exceeding the per-location tile limit
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Aborting load") &&
                                   msg.Contains("'Farm'") &&
                                   msg.Contains("exceeds tile count limit") &&
                                   msg.Contains("To prevent data loss") &&
                                   msg.Contains("entire load operation is being aborted")),
                LogLevel.Alert),
                Times.Once);

            // Verify that no data from the exceeding location was added to the cache
            for (var i = 0; i < excessTiles; i++)
            {
                Assert.Equal(30f, service.GetSoilHealth("Farm", new Vector2(i, 0))); // Returns InitialSoilHealth (30f) when cache is cleared
            }
        }

        [Fact]
        public void LoadData_WithPerLocationTileLimitExceeded_AbortsEntireLoadIncludingValidLocations()
        {
            // Arrange: Create save data with one location exceeding the limit and another valid location
            var excessTiles = ModConstants.MaxTilesPerLocation + 10; // Exceeds the limit

            var locationEntriesExceeding = new Dictionary<string, float>(excessTiles);
            for (var i = 0; i < excessTiles; i++)
            {
                locationEntriesExceeding.Add($"{i},0", 75.0f);
            }

            var locationEntriesValid = new Dictionary<string, float>();
            locationEntriesValid.Add("100,100", 60.0f); // Valid entry in another location

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["ExceedingLocation"] = locationEntriesExceeding,  // This will exceed the limit
                    ["ValidLocation"] = locationEntriesValid           // This should NOT be processed
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

            // Act: Load the data - this should abort the entire load when the first location exceeds the limit
            service.LoadData("test_save");

            // Assert: Verify that the warning log appeared for the exceeding location
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Aborting load") &&
                                   msg.Contains("'ExceedingLocation'") &&
                                   msg.Contains("exceeds tile count limit") &&
                                   msg.Contains("To prevent data loss") &&
                                   msg.Contains("entire load operation is being aborted")),
                LogLevel.Alert),
                Times.Once);

            // Verify that no data from the exceeding location was added to the cache
            for (var i = 0; i < excessTiles; i++)
            {
                Assert.Equal(30f, service.GetSoilHealth("ExceedingLocation", new Vector2(i, 0))); // Returns InitialSoilHealth (30f) when cache is cleared
            }

            // The valid location should NOT be processed since the entire load is aborted
            Assert.Equal(30f, service.GetSoilHealth("ValidLocation", new Vector2(100, 100))); // Returns InitialSoilHealth (30f) when cache is cleared
        }

        [Fact]
        public void LoadData_WhenOneLocationExceedsLimit_AbortsEntireLoad()
        {
            // Arrange: Create save data with one location at the limit and one that exceeds the limit
            // When the second location exceeds the limit, the entire load should be aborted

            var atLimitTiles = ModConstants.MaxTilesPerLocation; // Exactly at the limit
            var excessTiles = ModConstants.MaxTilesPerLocation + 1; // Exceeds the limit by 1

            var locationEntriesAtLimit = new Dictionary<string, float>(atLimitTiles);
            for (var i = 0; i < atLimitTiles; i++)
            {
                locationEntriesAtLimit.Add($"{i},0", 75.0f);
            }

            var locationEntriesExceeding = new Dictionary<string, float>(excessTiles);
            for (var i = 0; i < excessTiles; i++)
            {
                locationEntriesExceeding.Add($"{i},0", 85.0f);
            }

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["AtLimitLocation"] = locationEntriesAtLimit,      // Exactly at the limit - should be processed
                    ["ExceedingLocation"] = locationEntriesExceeding   // This will exceed the limit and abort the entire load
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

            // Act: Load the data - should abort the entire load when the exceeding location is encountered
            service.LoadData("test_save");

            // Assert: Verify that the warning log appeared for the exceeding location
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Aborting load") &&
                                   msg.Contains("'ExceedingLocation'") &&
                                   msg.Contains("exceeds tile count limit") &&
                                   msg.Contains("To prevent data loss") &&
                                   msg.Contains("entire load operation is being aborted")),
                LogLevel.Alert),
                Times.Once);

            // Verify that the at-limit location was NOT processed since the entire load was aborted
            for (var i = 0; i < atLimitTiles; i++)
            {
                Assert.Equal(30f, service.GetSoilHealth("AtLimitLocation", new Vector2(i, 0))); // Returns InitialSoilHealth (30f) when cache is cleared
            }

            // Verify that no data from the exceeding location was added to the cache
            for (var i = 0; i < excessTiles; i++)
            {
                Assert.Equal(30f, service.GetSoilHealth("ExceedingLocation", new Vector2(i, 0))); // Returns InitialSoilHealth (30f) when cache is cleared
            }
        }

        [Fact]
        public void LoadData_WithExactlyAtPerLocationTileLimit_DoesNotAbort()
        {
            // Arrange: Create save data with exactly the per-location limit (should NOT trigger DoS protection)
            var atLimitTiles = ModConstants.MaxTilesPerLocation; // Exactly at the limit

            var locationEntries = new Dictionary<string, float>(atLimitTiles);
            for (var i = 0; i < atLimitTiles; i++)
            {
                locationEntries.Add($"{i},0", 75.0f); // Valid tile keys that will be processed
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

            // Act: Load the data - this should NOT trigger DoS protection
            service.LoadData("test_save");

            // Assert: Verify that no critical error log appeared for tile limit exceeded
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Tile count limit") &&
                                   msg.Contains("exceeded for location")),
                LogLevel.Alert),
                Times.Never);

            // Verify that the data was loaded successfully (up to the limit)
            for (var i = 0; i < atLimitTiles; i++)
            {
                Assert.Equal(75.0f, service.GetSoilHealth("Farm", new Vector2(i, 0)));
            }
        }

        [Fact]
        public void LoadData_WithMultipleLocationsEachWithinLimit_ProcessesAllLocations()
        {
            // Arrange: Create save data with multiple locations, each within the per-location limit
            var location1Entries = new Dictionary<string, float>();
            var location2Entries = new Dictionary<string, float>();

            // Add tiles to first location (well within the limit)
            for (var i = 0; i < 100; i++)
            {
                location1Entries.Add($"{i},0", 75.0f);
            }

            // Add tiles to second location (well within the limit)
            for (var i = 0; i < 200; i++)
            {
                location2Entries.Add($"{i},0", 85.0f);
            }

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["Farm"] = location1Entries,
                    ["Town"] = location2Entries
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

            // Act: Load the data - should process both locations since each is within the limit
            service.LoadData("test_save");

            // Assert: Verify that no critical error log appeared for tile limit exceeded
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Tile count limit") &&
                                   msg.Contains("exceeded for location")),
                LogLevel.Alert),
                Times.Never);

            // Verify that data from both locations was loaded successfully
            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(75.0f, service.GetSoilHealth("Farm", new Vector2(i, 0)));
            }

            for (var i = 0; i < 200; i++)
            {
                Assert.Equal(85.0f, service.GetSoilHealth("Town", new Vector2(i, 0)));
            }
        }
    }
}
