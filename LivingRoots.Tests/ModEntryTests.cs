using System;
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
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object);
            
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
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object);
            
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
            
            var controller = new ModController(
                mockHelper.Object, 
                mockMonitor.Object, 
                mockManifest.Object, 
                mockModDataService.Object);
            
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
    }
}