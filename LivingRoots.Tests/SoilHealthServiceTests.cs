using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<IMonitor> _mockMonitor;

        public SoilHealthServiceTests()
        {
            _mockModDataService = new Mock<IModDataService>(MockBehavior.Strict);
            _mockMonitor = new Mock<IMonitor>(MockBehavior.Strict);
        }

        private SoilHealthService CreateService(IModDataService? modDataService = null, IMonitor? monitor = null)
        {
            var mockModDataService = modDataService ?? _mockModDataService.Object;
            var mockMonitor = monitor ?? _mockMonitor.Object;
            
            return new SoilHealthService(mockModDataService, mockMonitor);
        }

        [Fact]
        public void SoilHealthService_Constructor_InitializesDependencies()
        {
            // Arrange
            var mockModDataService = new Mock<IModDataService>().Object;
            var mockMonitor = new Mock<IMonitor>().Object;

            // Act
            var service = new SoilHealthService(mockModDataService, mockMonitor);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SoilHealthService_Constructor_ThrowsArgumentNullException_WhenModDataServiceIsNull()
        {
            // Arrange
            IMonitor mockMonitor = new Mock<IMonitor>().Object;

            // Act & Assert
            var ex = Record.Exception(() => new SoilHealthService(null!, mockMonitor));
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void SoilHealthService_Constructor_ThrowsArgumentNullException_WhenMonitorIsNull()
        {
            // Arrange
            IModDataService mockModDataService = new Mock<IModDataService>().Object;

            // Act & Assert
            var ex = Record.Exception(() => new SoilHealthService(mockModDataService, null!));
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void LoadData_ClearsCache_WhenSaveIdIsNullOrWhitespace()
        {
            // Arrange
            var service = CreateService();
            
            // Set some initial data in the cache to verify it gets cleared
            service.SetSoilHealth("Farm", new Vector2(0, 0), 50f);

            // Setup monitor to expect the specific log message
            _mockMonitor.Setup(m => m.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn)).Verifiable();

            // Act
            service.LoadData(""); // Empty save ID

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(0, 0));
            Assert.Equal(0f, health); // Should be default value after clear
            _mockMonitor.Verify(m => m.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn), Times.Once);
        }

        [Fact]
        public void LoadData_ClearsCache_WhenSaveIdIsInvalid()
        {
            // Arrange
            var service = CreateService();
            
            // Set some initial data in the cache to verify it gets cleared
            service.SetSoilHealth("Farm", new Vector2(0, 0), 50f);

            // Setup monitor to expect the specific log message
            _mockMonitor.Setup(m => m.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn)).Verifiable();

            // Act
            service.LoadData("   "); // Whitespace-only save ID

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(0, 0));
            Assert.Equal(0f, health); // Should be default value after clear
            _mockMonitor.Verify(m => m.Log("LoadData aborted: invalid saveId. Runtime cache cleared.", LogLevel.Warn), Times.Once);
        }

        [Fact]
        public void LoadData_LoadsData_FromModDataService()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData["Farm"] = new Dictionary<string, float> { { "10,15", 75f }, { "11,15", 80f} };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var health1 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health2 = service.GetSoilHealth("Farm", new Vector2(11, 15));
            Assert.Equal(75f, health1);
            Assert.Equal(80f, health2);
            
            _mockModDataService.Verify(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void LoadData_HandlesNullLocationName_InSaveData()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData[""] = new Dictionary<string, float> { { "10,15", 75f } }; // Empty location name
            expectedData.LocationHealthData["Farm"] = new Dictionary<string, float> { { "11,15", 80f } };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var healthEmpty = service.GetSoilHealth("", new Vector2(10, 15)); // Should not find data for empty location
            var healthFarm = service.GetSoilHealth("Farm", new Vector2(11, 15)); // Should find data for Farm
            
            Assert.Equal(0f, healthEmpty);
            Assert.Equal(80f, healthFarm);
            
            _mockMonitor.Verify(m => m.Log("Skipped soil health data with null or empty location name.", LogLevel.Warn), Times.Once);
        }

        [Fact]
        public void LoadData_HandlesNullTileData_InSaveData()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData["Farm"] = null; // Null tile data
            expectedData.LocationHealthData["Greenhouse"] = new Dictionary<string, float> { { "5,5", 100f } };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var healthFarm = service.GetSoilHealth("Farm", new Vector2(0, 0)); // Should not find data for Farm with null tiles
            var healthGreenhouse = service.GetSoilHealth("Greenhouse", new Vector2(5, 5)); // Should find data for Greenhouse
            
            Assert.Equal(0f, healthFarm);
            Assert.Equal(100f, healthGreenhouse);
        }

        [Fact]
        public void LoadData_HandlesMalformedTileKeys_InSaveData()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData["Farm"] = new Dictionary<string, float> 
            { 
                { "10,15", 75f },       // Valid
                { "malformed_key", 80f }, // Invalid format
                { "12", 85f },           // Missing Y coord
                { "13,14,15", 90f }      // Too many parts
            };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var healthValid = service.GetSoilHealth("Farm", new Vector2(10, 15)); // Should find valid tile
            var healthInvalid = service.GetSoilHealth("Farm", new Vector2(12, 0)); // Should not find invalid tile
            
            Assert.Equal(75f, healthValid);
            Assert.Equal(0f, healthInvalid);
            
            // Should warn only once per location about malformed keys
            _mockMonitor.Verify(m => m.Log(It.Is<string>(s => s.Contains("malformed soil health tile key")), LogLevel.Warn), Times.AtLeastOnce);
        }

        [Fact]
        public void LoadData_HandlesInvalidTileValues_InSaveData()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData["Farm"] = new Dictionary<string, float> 
            { 
                { "10,15", 75f },        // Valid
                { "11,15", float.NaN },  // Invalid NaN
                { "12,15", float.PositiveInfinity }, // Invalid Infinity
                { "13,15", float.NegativeInfinity }  // Invalid -Infinity
            };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var healthValid = service.GetSoilHealth("Farm", new Vector2(10, 15)); // Should find valid tile
            var healthNaN = service.GetSoilHealth("Farm", new Vector2(11, 15)); // Should not find NaN tile
            var healthInf = service.GetSoilHealth("Farm", new Vector2(12, 15)); // Should not find Infinity tile
            var healthNegInf = service.GetSoilHealth("Farm", new Vector2(13, 15)); // Should not find -Infinity tile
            
            Assert.Equal(75f, healthValid);
            Assert.Equal(0f, healthNaN);
            Assert.Equal(0f, healthInf);
            Assert.Equal(0f, healthNegInf);
            
            // Should warn only once per location about invalid values
            _mockMonitor.Verify(m => m.Log(It.Is<string>(s => s.Contains("Skipped invalid soil health value")), LogLevel.Warn), Times.AtLeastOnce);
        }

        [Fact]
        public void LoadData_ClampsValues_ToRange0To100()
        {
            // Arrange
            var expectedData = new SoilHealthState();
            expectedData.LocationHealthData["Farm"] = new Dictionary<string, float> 
            { 
                { "10,15", -10f },  // Below range
                { "11,15", 150f },  // Above range
                { "12,15", 50f }    // Within range
            };
            
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);
            
            _mockMonitor.Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("test_save");

            // Assert
            var healthBelowRange = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var healthAboveRange = service.GetSoilHealth("Farm", new Vector2(11, 15));
            var healthInRange = service.GetSoilHealth("Farm", new Vector2(12, 15));
            
            Assert.Equal(0f, healthBelowRange);  // Clamped to 0
            Assert.Equal(100f, healthAboveRange); // Clamped to 100
            Assert.Equal(50f, healthInRange);    // Unchanged
        }

        [Fact]
        public void LoadData_HandlesNullSaveData_ReturnsDefaultValues()
        {
            // Arrange
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns((SoilHealthState?)null); // Explicitly cast to nullable type to satisfy compiler
            
            _mockMonitor.Setup(m => m.Log("No existing Soil Health data found. Starting fresh.", LogLevel.Info)).Verifiable();
            
            var service = CreateService();

            // Act
            service.LoadData("nonexistent_save");

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(0, 0));
            Assert.Equal(0f, health); // Should return default value when no data exists
            
            _mockMonitor.Verify(m => m.Log("No existing Soil Health data found. Starting fresh.", LogLevel.Info), Times.Once);
        }

        [Fact]
        public void SaveData_CallsModDataServiceWithCorrectKey()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            service.SetSoilHealth("Farm", new Vector2(11, 15), 80f);
            
            string capturedKey = "";
            SoilHealthState? capturedState = null;
            
            _mockModDataService
                .Setup(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedState = state;
                    capturedKey = key;
                })
                .Verifiable();
            
            _mockMonitor.Setup(m => m.Log("Soil Health data saved successfully.", LogLevel.Trace)).Verifiable();

            // Act
            service.SaveData("test_save_123");

            // Assert
            Assert.Equal("soil_health_data_test_save_123", capturedKey);
            Assert.NotNull(capturedState);
            Assert.Contains("Farm", capturedState.LocationHealthData.Keys);
            Assert.Equal(75f, capturedState.LocationHealthData["Farm"]["10,15"]);
            Assert.Equal(80f, capturedState.LocationHealthData["Farm"]["11,15"]);
            
            _mockModDataService.Verify(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SaveData_OnlySavesLocationsWithValidTiles()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            // Don't add any tiles to "Greenhouse" location
            
            string capturedKey = "";
            SoilHealthState? capturedState = null;
            
            _mockModDataService
                .Setup(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Callback<SoilHealthState, string>((state, key) => 
                {
                    capturedState = state;
                    capturedKey = key;
                })
                .Verifiable();
            
            _mockMonitor.Setup(m => m.Log("Soil Health data saved successfully.", LogLevel.Trace)).Verifiable();

            // Act
            service.SaveData("test_save_empty_location");

            // Assert
            Assert.Equal("soil_health_data_test_save_empty_location", capturedKey);
            Assert.NotNull(capturedState);
            Assert.Contains("Farm", capturedState.LocationHealthData.Keys);
            Assert.DoesNotContain("Greenhouse", capturedState.LocationHealthData.Keys); // Empty location should not be saved
            Assert.Single(capturedState.LocationHealthData); // Only Farm should be saved
        }

        [Fact]
        public void SaveData_HandlesNullSaveId()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("SaveData aborted: invalid saveId.", LogLevel.Warn)).Verifiable();

            // Act
            service.SaveData(null); // Pass null save ID

            // Assert
            // Should not call SaveData on the mock data service
            _mockModDataService.Verify(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
            monitor.Verify(m => m.Log("SaveData aborted: invalid saveId.", LogLevel.Warn), Times.Once);
        }

        [Fact]
        public void SaveData_HandlesWhitespaceSaveId()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("SaveData aborted: invalid saveId.", LogLevel.Warn)).Verifiable();

            // Act
            service.SaveData("   "); // Pass whitespace-only save ID

            // Assert
            // Should not call SaveData on the mock data service
            _mockModDataService.Verify(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
            monitor.Verify(m => m.Log("SaveData aborted: invalid saveId.", LogLevel.Warn), Times.Once);
        }

        [Fact]
        public void GetSoilHealth_ReturnsDefault_WhenLocationIsInvalid()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);

            // Act
            var health = service.GetSoilHealth("", new Vector2(10, 15)); // Empty location name
            var healthNull = service.GetSoilHealth(null!, new Vector2(10, 15)); // Null location name

            // Assert
            Assert.Equal(0f, health);
            Assert.Equal(0f, healthNull);
        }

        [Fact]
        public void GetSoilHealth_ReturnsDefault_WhenTileCoordinatesAreInvalid()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);

            // Act
            var healthNaN = service.GetSoilHealth("Farm", new Vector2(float.NaN, 15)); // NaN X
            var healthInfinity = service.GetSoilHealth("Farm", new Vector2(10, float.PositiveInfinity)); // Infinity Y
            var healthBothInvalid = service.GetSoilHealth("Farm", new Vector2(float.NaN, float.NaN)); // Both NaN

            // Assert
            Assert.Equal(0f, healthNaN);
            Assert.Equal(0f, healthInfinity);
            Assert.Equal(0f, healthBothInvalid);
        }

        [Fact]
        public void GetSoilHealth_ReturnsCorrectValue_WhenDataExists()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            service.SetSoilHealth("Farm", new Vector2(10, 16), 85.5f);

            // Act
            var health1 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health2 = service.GetSoilHealth("Farm", new Vector2(10, 16));

            // Assert
            Assert.Equal(75f, health1);
            Assert.Equal(85.5f, health2);
        }

        [Fact]
        public void GetSoilHealth_ReturnsDefault_WhenNoDataExistsForTile()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);

            // Act
            var health = service.GetSoilHealth("Farm", new Vector2(11, 15)); // Different tile

            // Assert
            Assert.Equal(0f, health); // Default value when tile doesn't exist
        }

        [Fact]
        public void SetSoilHealth_StoresCorrectValue()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(10, 15));
            Assert.Equal(75f, health);
        }

        [Fact]
        public void SetSoilHealth_ClampsValue_ToRange0To100()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.SetSoilHealth("Farm", new Vector2(10, 15), -10f);  // Below range
            var healthBelow = service.GetSoilHealth("Farm", new Vector2(10, 15));
            
            service.SetSoilHealth("Farm", new Vector2(10, 16), 150f); // Above range
            var healthAbove = service.GetSoilHealth("Farm", new Vector2(10, 16));

            // Assert
            Assert.Equal(0f, healthBelow);  // Clamped to 0
            Assert.Equal(100f, healthAbove); // Clamped to 100
        }

        [Fact]
        public void SetSoilHealth_SkipsWhenLocationIsInvalid()
        {
            // Arrange
            var service = CreateService();
            
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn)).Verifiable();

            // Act
            service.SetSoilHealth("", new Vector2(10, 15), 75f); // Empty location
            service.SetSoilHealth(null!, new Vector2(11, 15), 80f); // Null location

            // Assert
            var health1 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health2 = service.GetSoilHealth("Farm", new Vector2(11, 15));
            Assert.Equal(0f, health1);
            Assert.Equal(0f, health2);
        }

        [Fact]
        public void SetSoilHealth_SkipsWhenTileCoordinatesAreInvalid()
        {
            // Arrange
            var service = CreateService();
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn)).Verifiable();

            // Act
            service.SetSoilHealth("Farm", new Vector2(float.NaN, 15), 75f); // NaN X
            service.SetSoilHealth("Farm", new Vector2(10, float.PositiveInfinity), 80f); // Infinity Y
            service.SetSoilHealth("Farm", new Vector2(float.NaN, float.NaN), 85f); // Both NaN

            // Assert
            var health1 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health2 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health3 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            Assert.Equal(0f, health1);
            Assert.Equal(0f, health2);
            Assert.Equal(0f, health3);
            
            monitor.Verify(m => m.Log("SetSoilHealth skipped: invalid tile coordinates.", LogLevel.Warn), Times.Exactly(3));
        }

        [Fact]
        public void UpdateHealth_IncrementsExistingValue()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 50f);

            // Act
            service.UpdateHealth("Farm", new Vector2(10, 15), 25f); // Add 25 to existing 50

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(10, 15));
            Assert.Equal(75f, health);
        }

        [Fact]
        public void UpdateHealth_StartsFromZero_WhenNoExistingValue()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.UpdateHealth("Farm", new Vector2(10, 15), 30f); // Add 30 to default 0

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(10, 15));
            Assert.Equal(30f, health);
        }

        [Fact]
        public void UpdateHealth_ClampsResult_ToRange0To100()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 10f); // Start with 10

            // Act
            service.UpdateHealth("Farm", new Vector2(10, 15), -15f); // Would be -5, clamped to 0
            var healthBelow = service.GetSoilHealth("Farm", new Vector2(10, 15));
            
            service.SetSoilHealth("Farm", new Vector2(10, 16), 90f); // Start with 90
            service.UpdateHealth("Farm", new Vector2(10, 16), 20f); // Would be 110, clamped to 100
            var healthAbove = service.GetSoilHealth("Farm", new Vector2(10, 16));

            // Assert
            Assert.Equal(0f, healthBelow);  // Clamped to 0
            Assert.Equal(100f, healthAbove); // Clamped to 100
        }

        [Fact]
        public void UpdateHealth_SkipsWhenLocationIsInvalid()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 50f);
            
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn)).Verifiable();

            // Act
            service.UpdateHealth("", new Vector2(10, 15), 25f); // Empty location
            service.UpdateHealth(null!, new Vector2(11, 15), 30f); // Null location

            // Assert
            var health1 = service.GetSoilHealth("Farm", new Vector2(10, 15));
            var health2 = service.GetSoilHealth("Farm", new Vector2(11, 15));
            Assert.Equal(50f, health1); // Should remain unchanged
            Assert.Equal(0f, health2); // Should remain default
        }

        [Fact]
        public void UpdateHealth_SkipsWhenTileCoordinatesAreInvalid()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 50f);
            
            var monitor = _mockMonitor;
            monitor.Setup(m => m.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn)).Verifiable();

            // Act
            service.UpdateHealth("Farm", new Vector2(float.NaN, 15), 25f); // NaN X
            service.UpdateHealth("Farm", new Vector2(10, float.PositiveInfinity), 30f); // Infinity Y
            service.UpdateHealth("Farm", new Vector2(float.NaN, float.NaN), 35f); // Both NaN

            // Assert
            var health = service.GetSoilHealth("Farm", new Vector2(10, 15));
            Assert.Equal(50f, health); // Should remain unchanged
            
            monitor.Verify(m => m.Log("UpdateHealth skipped: invalid tile coordinates.", LogLevel.Warn), Times.Exactly(3));
        }

        [Fact]
        public void ThreadSafety_MultipleThreadsAccessingService_DoNotCorruptData()
        {
            // Arrange
            var service = CreateService();
            var tasks = new List<System.Threading.Tasks.Task>();
            
            // Set initial values for some tiles
            service.SetSoilHealth("Farm", new Vector2(0, 0), 50f);
            service.SetSoilHealth("Farm", new Vector2(1, 1), 60f);

            // Act - Run multiple operations from different threads
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        // Different threads operate on different tiles to avoid conflicts
                        var tileX = taskId * 10 + j % 5;
                        var tileY = taskId * 10 + j / 5;
                        
                        service.SetSoilHealth("Farm", new Vector2(tileX, tileY), taskId * 10 + j);
                        var result = service.GetSoilHealth("Farm", new Vector2(tileX, tileY));
                        
                        // Update some existing tiles
                        if (tileX < 5 && tileY < 5)
                        {
                            service.UpdateHealth("Farm", new Vector2(tileX, tileY), 5f);
                        }
                    }
                }));
            }

            // Wait for all tasks to complete
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            // Assert - No exceptions should occur and data should be consistent
            var health00 = service.GetSoilHealth("Farm", new Vector2(0, 0));
            var health11 = service.GetSoilHealth("Farm", new Vector2(1, 1));
            
            // Values should be in valid range
            Assert.InRange(health00, 0f, 100f);
            Assert.InRange(health11, 0f, 100f);
        }

        [Fact]
        public void LoadData_HandlesExceptionGracefully()
        {
            // Arrange
            _mockModDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test exception during load"));
            
            _mockMonitor.Setup(m => m.Log("Error occurred while loading soil health data.", LogLevel.Error)).Verifiable();
            
            var service = CreateService();

            // Act
            var ex = Record.Exception(() => service.LoadData("test_save"));

            // Assert
            Assert.Null(ex); // Exception should be caught and logged, not propagated
            _mockMonitor.Verify(m => m.Log("Error occurred while loading soil health data.", LogLevel.Error), Times.Once);
        }

        [Fact]
        public void SaveData_HandlesExceptionGracefully()
        {
            // Arrange
            var service = CreateService();
            service.SetSoilHealth("Farm", new Vector2(10, 15), 75f);
            
            _mockModDataService
                .Setup(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test exception during save"));
            
            _mockMonitor.Setup(m => m.Log("Error occurred while persisting soil health data.", LogLevel.Error)).Verifiable();

            // Act
            var ex = Record.Exception(() => service.SaveData("test_save"));

            // Assert
            Assert.Null(ex); // Exception should be caught and logged, not propagated
            _mockMonitor.Verify(m => m.Log("Error occurred while persisting soil health data.", LogLevel.Error), Times.Once);
        }

        [Fact]
        public void SaveData_SkipsSavingWhenNoValidData()
        {
            // Arrange
            var service = CreateService();
            // Don't set any soil health values, so the cache is empty
            
            _mockMonitor.Setup(m => m.Log("No valid soil health data to save; skipping persistence.", LogLevel.Trace)).Verifiable();

            // Act
            service.SaveData("test_save_no_data");

            // Assert
            // SaveData should not be called on the mock data service since there's no valid data
            _mockModDataService.Verify(ds => ds.SaveData(It.IsAny<SoilHealthState>(), It.IsAny<string>()), Times.Never);
            // The log message should be called since we have the condition to check if LocationHealthData is empty
            _mockMonitor.Verify(m => m.Log("No valid soil health data to save; skipping persistence.", LogLevel.Trace), Times.Once);
        }
    }
}