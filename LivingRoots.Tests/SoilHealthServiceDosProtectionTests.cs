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
        public void LoadData_WithPerLocationTileLimitExceeded_AbortsAndClearsCache()
        {
            // Arrange: Create save data with more tiles than the per-location limit (500)
            var excessTiles = ModConstants.MaxTilesPerLocation + 10; // 510 tiles (exceeds limit by 10)

            var locationEntries = new Dictionary<string, float>(excessTiles);
            for (int i = 0; i < excessTiles; i++)
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

            // Pre-populate cache with some data to verify it gets cleared
            service.SetSoilHealth("ExistingLocation", new Vector2(1, 1), 80.0f);

            // Act: Load the data - this should trigger DoS protection and clear the cache
            service.LoadData("test_save");

            // Assert: Verify that the critical error log appeared when exceeding the per-location tile limit
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Tile count limit") &&
                                   msg.Contains("exceeded for location") &&
                                   msg.Contains("Cache cleared to prevent inconsistent state.")),
                LogLevel.Alert),
                Times.Once);

            // Verify that the cache was cleared - the existing data should be gone
            Assert.Equal(0.0f, service.GetSoilHealth("ExistingLocation", new Vector2(1, 1)));

            // Verify that no data from the loaded save was added to the cache
            for (int i = 0; i < excessTiles; i++)
            {
                Assert.Equal(0.0f, service.GetSoilHealth("Farm", new Vector2(i, 0)));
            }
        }

        [Fact]
        public void LoadData_WithPerLocationTileLimitExceeded_DoesNotProcessOtherLocations()
        {
            // Arrange: Create save data with one location exceeding the limit and another valid location
            var excessTiles = ModConstants.MaxTilesPerLocation + 10; // Exceeds the limit

            var locationEntriesExceeding = new Dictionary<string, float>(excessTiles);
            for (int i = 0; i < excessTiles; i++)
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
                    ["ValidLocation"] = locationEntriesValid           // This should not be processed
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

            // Pre-populate cache with some data to verify it gets cleared
            service.SetSoilHealth("ExistingLocation", new Vector2(1, 1), 80.0f);

            // Act: Load the data - this should abort when the first location exceeds the limit
            service.LoadData("test_save");

            // Assert: Verify that the critical error log appeared
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Tile count limit") &&
                                   msg.Contains("exceeded for location") &&
                                   msg.Contains("Cache cleared to prevent inconsistent state.")), LogLevel.Alert),
                Times.Once);

            // Verify that the cache was cleared - the existing data should be gone
            Assert.Equal(0.0f, service.GetSoilHealth("ExistingLocation", new Vector2(1, 1)));

            // Verify that no data from either location was added to the cache
            for (int i = 0; i < excessTiles; i++)
            {
                Assert.Equal(0.0f, service.GetSoilHealth("ExceedingLocation", new Vector2(i, 0)));
            }

            // The valid location should also not be processed since the operation was aborted
            Assert.Equal(0.0f, service.GetSoilHealth("ValidLocation", new Vector2(100, 100)));
        }

        [Fact]
        public void LoadData_WhenOneLocationExceedsLimit_AbortsProcessing()
        {
            // Arrange: Create save data with one location at the limit and one that exceeds the limit
            // When the second location exceeds the limit, processing should abort and cache should be cleared

            var atLimitTiles = ModConstants.MaxTilesPerLocation; // Exactly at the limit
            var excessTiles = ModConstants.MaxTilesPerLocation + 1; // Exceeds the limit by 1

            var locationEntriesAtLimit = new Dictionary<string, float>(atLimitTiles);
            for (int i = 0; i < atLimitTiles; i++)
            {
                locationEntriesAtLimit.Add($"{i},0", 75.0f);
            }

            var locationEntriesExceeding = new Dictionary<string, float>(excessTiles);
            for (int i = 0; i < excessTiles; i++)
            {
                locationEntriesExceeding.Add($"{i},0", 85.0f);
            }

            var saveData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    ["AtLimitLocation"] = locationEntriesAtLimit,      // Exactly at the limit - should be processed
                    ["ExceedingLocation"] = locationEntriesExceeding   // This will exceed the limit and cause abort
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

            // Pre-populate cache with some data to verify it gets cleared
            service.SetSoilHealth("ExistingLocation", new Vector2(1, 1), 80.0f);

            // Act: Load the data - should abort when the second location exceeds the limit
            service.LoadData("test_save");

            // Assert: Verify that the critical error log appeared
            _mockMonitor.Verify(x => x.Log(
                It.Is<string>(msg => msg.Contains("Tile count limit") &&
                                   msg.Contains("exceeded for location") &&
                                   msg.Contains("Cache cleared to prevent inconsistent state.")),
                LogLevel.Alert),
                Times.Once);

            // Verify that the cache was cleared - the existing data should be gone
            Assert.Equal(0.0f, service.GetSoilHealth("ExistingLocation", new Vector2(1, 1)));

            // Verify that no data from either location was added to the cache
            for (int i = 0; i < atLimitTiles; i++)
            {
                Assert.Equal(0.0f, service.GetSoilHealth("AtLimitLocation", new Vector2(i, 0)));
            }

            for (int i = 0; i < excessTiles; i++)
            {
                Assert.Equal(0.0f, service.GetSoilHealth("ExceedingLocation", new Vector2(i, 0)));
            }
        }

        [Fact]
        public void LoadData_WithExactlyAtPerLocationTileLimit_DoesNotAbort()
        {
            // Arrange: Create save data with exactly the per-location limit (should NOT trigger DoS protection)
            var atLimitTiles = ModConstants.MaxTilesPerLocation; // Exactly at the limit

            var locationEntries = new Dictionary<string, float>(atLimitTiles);
            for (int i = 0; i < atLimitTiles; i++)
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
            for (int i = 0; i < atLimitTiles; i++)
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
            for (int i = 0; i < 100; i++)
            {
                location1Entries.Add($"{i},0", 75.0f);
            }

            // Add tiles to second location (well within the limit)
            for (int i = 0; i < 200; i++)
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
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(75.0f, service.GetSoilHealth("Farm", new Vector2(i, 0)));
            }

            for (int i = 0; i < 200; i++)
            {
                Assert.Equal(85.0f, service.GetSoilHealth("Town", new Vector2(i, 0)));
            }
        }
    }
}
