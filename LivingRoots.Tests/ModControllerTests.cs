using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests
{
    public class ModControllerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;

        public ModControllerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockModDataService = new Mock<IModDataService>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
        }

        [Fact]
        public void Constructor_WithNullHelper_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(null, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, null, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullManifest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, null, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullModDataService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, null, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullSoilHealthService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, null));
        }

        [Fact]
        public void UnregisterEvents_WhenDisposed_DoesNotUnregister()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act - Dispose the controller
            controller.Dispose();

            // Assert - Verify the disposal message was logged
            _mockMonitor.Verify(m => m.Log("Controller disposed successfully.", LogLevel.Trace), Times.Once);
            
            // Additional unregistration attempts should not attempt to unsubscribe
            _mockMonitor.Invocations.Clear(); // Reset previous calls to check new ones
            
            // Try to unregister events after disposal
            controller.UnregisterEvents();
            
            // The method should handle this gracefully without throwing
            var ex = Record.Exception(() => controller.UnregisterEvents());
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_IsIdempotent_CanBeCalledMultipleTimes()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act & Assert - Multiple calls to Dispose should not cause issues
            controller.Dispose();
            controller.Dispose(); // Call again to test idempotency

            // Verify the log was only output once (since we implemented the flag correctly)
            _mockMonitor.Verify(m => m.Log("Controller disposed successfully.", LogLevel.Trace), Times.Once);
        }

        [Fact]
        public void UnregisterEvents_WhenNotRegistered_DoesNotUnsubscribe()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);
            
            // Act
            controller.UnregisterEvents();

            // Assert - Events should not be unsubscribed if they were never registered
            // Should log message indicating events were not registered
            _mockMonitor.Verify(m => m.Log("Events were not registered, skipping unregistration.", LogLevel.Trace), Times.Once);
        }
    }
}
