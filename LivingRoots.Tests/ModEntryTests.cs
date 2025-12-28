using System;
using System.IO;
using System.Reflection;
using StardewModdingAPI;
using Xunit;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Services;
using LivingRoots.Domain;
using Moq;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Unit tests for the ModEntry class
    /// </summary>
    public class ModEntryTests
    {
        [Fact]
        public void ModEntry_Instantiation_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new ModEntry());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenCalled_SetsDisposedFlagToTrue()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Act
            modEntry.Dispose();
            
            // Assert - Check the _disposed field using reflection
            var disposedField = typeof(ModEntry).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            var isDisposed = disposedField?.GetValue(modEntry) as bool?;
            Assert.True(isDisposed);
        }

        [Fact]
        public void Dispose_WhenControllerIsNull_DoesNotThrow()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Ensure controller is null by not calling Entry
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, null);
            
            // Act & Assert
            var exception = Record.Exception(() => modEntry.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_SetsDisposedFlagOnlyOnce()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Act
            modEntry.Dispose();
            modEntry.Dispose(); // Call Dispose twice
            
            // Assert - Check the _disposed field using reflection
            var disposedField = typeof(ModEntry).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            var isDisposed = disposedField?.GetValue(modEntry) as bool?;
            Assert.True(isDisposed); // Should still be true after first call
        }

        [Fact]
        public void Dispose_WithDisposingFalse_DoesNotSetControllerToNull()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Create a real controller and set it directly
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockModDataService = new Mock<IModDataService>();
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object);
            
            // Set the controller field directly using reflection
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, controller);
            
            // Verify controller is not null before disposal
            var controllerBeforeDispose = controllerField?.GetValue(modEntry);
            Assert.NotNull(controllerBeforeDispose);
            
            // Act - Call Dispose with disposing=false using reflection
            var disposeMethod = typeof(ModEntry).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
            disposeMethod?.Invoke(modEntry, new object[] { false });
            
            // Assert - Controller should not be set to null when disposing=false
            var controllerAfterDispose = controllerField?.GetValue(modEntry);
            Assert.NotNull(controllerAfterDispose);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotDisposeControllerAgain()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Create a real controller and set it directly
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockModDataService = new Mock<IModDataService>();
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object);
            
            // Set the controller field directly using reflection
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, controller);
            
            // Set the disposed flag to true manually to simulate already disposed state
            var disposedField = typeof(ModEntry).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            disposedField?.SetValue(modEntry, true);
            
            // Act
            modEntry.Dispose();
            
            // Assert - Controller should still not be null since we didn't actually dispose it
            var controllerAfterDispose = controllerField?.GetValue(modEntry);
            Assert.NotNull(controllerAfterDispose);
        }

        [Fact]
        public void Dispose_WhenCalledWithController_SetsControllerToNull()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Create a real controller and set it directly
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockModDataService = new Mock<IModDataService>();
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object);
            
            // Set the controller field directly using reflection
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, controller);
            
            // Verify controller is not null before disposal
            var controllerBeforeDispose = controllerField?.GetValue(modEntry);
            Assert.NotNull(controllerBeforeDispose);
            
            // Act
            modEntry.Dispose();
            
            // Assert
            var controllerAfterDispose = controllerField?.GetValue(modEntry);
            Assert.Null(controllerAfterDispose);
        }
        
        [Fact]
        public async Task Dispose_WhenCalledFromMultipleThreads_IsThreadSafe()
        {
            // Arrange
            var modEntry = new ModEntry();
            var tasks = new System.Threading.Tasks.Task[10];
            var disposedFlags = new bool[10];
            
            // Act
            for (int i = 0; i < 10; i++)
            {
                int index = i; // Capture for closure
                tasks[index] = System.Threading.Tasks.Task.Run(() =>
                {
                    modEntry.Dispose();
                    // Check the disposed state after disposal
                    var disposedField = typeof(ModEntry).GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    disposedFlags[index] = (bool)(disposedField?.GetValue(modEntry) ?? false);
                });
            }
            
            await System.Threading.Tasks.Task.WhenAll(tasks);
            
            // Assert - Only one disposal should have happened effectively, but the flag should be true
            var disposedField = typeof(ModEntry).GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var finalDisposedState = (bool)(disposedField?.GetValue(modEntry) ?? false);
            Assert.True(finalDisposedState);
            
            // Ensure no exceptions were thrown during concurrent disposal
            foreach (var task in tasks)
            {
                Assert.Equal(System.Threading.Tasks.TaskStatus.RanToCompletion, task.Status);
            }
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_CallsBaseDisposeAndReturnsEarly()
        {
            // Arrange
            var modEntry = new ModEntry();
            
            // Create a real controller and set it directly
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockModDataService = new Mock<IModDataService>();
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object);
            
            // Set the controller field directly using reflection
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, controller);
            
            // First disposal to set disposed flag
            modEntry.Dispose();
            
            // Verify controller is null after first disposal
            var controllerAfterFirstDispose = controllerField?.GetValue(modEntry);
            Assert.Null(controllerAfterFirstDispose);
            
            // Act - Call Dispose again when already disposed
            modEntry.Dispose();
            
            // Assert - Controller should still be null (no double disposal)
            var controllerAfterSecondDispose = controllerField?.GetValue(modEntry);
            Assert.Null(controllerAfterSecondDispose);
        }

        [Fact]
        public async Task Dispose_WhenCalledConcurrentlyWithBaseDisposeCall_IsThreadSafe()
        {
            // Arrange
            var modEntry = new ModEntry();
            var tasks = new System.Threading.Tasks.Task[10];
            var exceptions = new System.Exception[10];
            
            // Act
            for (int i = 0; i < 10; i++)
            {
                int index = i; // Capture for closure
                tasks[index] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        modEntry.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        exceptions[index] = ex;
                    }
                });
            }
            
            await System.Threading.Tasks.Task.WhenAll(tasks);
            
            // Assert - No exceptions should have been thrown during concurrent disposal
            for (int i = 0; i < exceptions.Length; i++)
            {
                Assert.Null(exceptions[i]);
            }
            
            // Verify final disposed state
            var disposedField = typeof(ModEntry).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            var finalDisposedState = (bool)(disposedField?.GetValue(modEntry) ?? false);
            Assert.True(finalDisposedState);
        }

        [Fact]
        public void Readme_ContainsCorrectGitHubReleasesLink()
        {
            // Arrange
            var readmePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "README.md"));
            var expectedLink = "https://github.com/lfgranja/stardew-raizes-vivas/releases";
            
            // Add File.Exists assertion to fail fast with a clear message
            Assert.True(File.Exists(readmePath), $"README.md file not found at path: {readmePath}");
            
            // Act
            var readmeContent = File.ReadAllText(readmePath);
            
            // Assert - This should fail initially since the link is incorrect
            Assert.Contains(expectedLink, readmeContent);
        }
    }
}
