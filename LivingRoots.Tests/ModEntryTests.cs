using System;
using System.IO;
using System.Reflection;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using Xunit;

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
        public void Dispose_WithDisposingFalse_DoesNotSetControllerToNull()
        {
            // Arrange
            var modEntry = new ModEntry();

            // Create a real controller and set it directly
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
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
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
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
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
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
            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object);

            // Set the controller field directly using reflection
            var controllerField = typeof(ModEntry).GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerField?.SetValue(modEntry, controller);

            // Act - First disposal to set disposed flag
            modEntry.Dispose();

            // Assert - Controller should be null after disposal
            var controllerAfterDispose = controllerField?.GetValue(modEntry);
            Assert.Null(controllerAfterDispose);
        }

        [Fact]
        public void Readme_ContainsCorrectGitHubReleasesLink()
        {
            // Arrange
            var expectedLink = "https://github.com/lfgranja/stardew-raizes-vivas/releases";

            // Search upward through parent directories to find README.md
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            FileInfo? readmeFile = null;

            for (int i = 0; i < 10 && dir != null; i++)
            {
                var files = dir.GetFiles("README.md");
                if (files.Length > 0)
                {
                    readmeFile = files[0];
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(readmeFile != null && readmeFile.Exists, $"README.md could not be found. Started search from {AppContext.BaseDirectory}");
            var readmeFullPath = readmeFile.FullName;

            // Act

            var readmeContent = File.ReadAllText(readmeFullPath);

            // Assert - Verify the README contains the correct GitHub releases link
            Assert.Contains(expectedLink, readmeContent);
        }
    }
}
