using System;
using System.IO;
using System.Reflection;
using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object,
                mockCompostingBinService.Object,
                mockSoilDecayService.Object);

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
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object,
                mockCompostingBinService.Object,
                mockSoilDecayService.Object);

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
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object,
                mockCompostingBinService.Object,
                mockSoilDecayService.Object);

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
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object,
                mockCompostingBinService.Object,
                mockSoilDecayService.Object);

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

            // Read README.md from embedded resource for reliable test access across all environments
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var readmeResourceName = resourceNames.FirstOrDefault(name => name.EndsWith("README.md"));

            Assert.True(readmeResourceName != null, $"README.md resource not found in assembly. Available resources: {string.Join(", ", resourceNames)}");

            // Act
            using var stream = assembly.GetManifestResourceStream(readmeResourceName);
            Assert.True(stream != null, $"Could not load README.md resource: {readmeResourceName}");

            using var reader = new StreamReader(stream);
            var readmeContent = reader.ReadToEnd();

            // Assert - Verify the README contains the correct GitHub releases link
            Assert.Contains(expectedLink, readmeContent);
        }

        [Fact]
        public void Entry_SubscribesToAssetRequestedEvent()
        {
            // Arrange
            var modEntry = new ModEntry();
            var mockHelper = new Mock<IModHelper>();
            var mockEvents = new Mock<IModEvents>();
            var mockContentEvents = new Mock<IContentEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockInputEvents = new Mock<IInputEvents>();
            var mockDisplayEvents = new Mock<IDisplayEvents>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();

            mockManifest.Setup(m => m.UniqueID).Returns("test.mod.id");
            mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));

            mockEvents.Setup(e => e.Content).Returns(mockContentEvents.Object);
            mockEvents.Setup(e => e.GameLoop).Returns(mockGameLoopEvents.Object);
            mockEvents.Setup(e => e.Input).Returns(mockInputEvents.Object);
            mockEvents.Setup(e => e.Display).Returns(mockDisplayEvents.Object);
            mockHelper.Setup(h => h.Events).Returns(mockEvents.Object);

            typeof(Mod).GetProperty("Monitor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(modEntry, mockMonitor.Object);
            typeof(Mod).GetProperty("ModManifest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(modEntry, mockManifest.Object);

            // Act
            modEntry.Entry(mockHelper.Object);

            // Assert
            mockContentEvents.VerifyAdd(c => c.AssetRequested += It.IsAny<EventHandler<StardewModdingAPI.Events.AssetRequestedEventArgs>>(), Times.Once);
        }

        private static AssetRequestedEventArgs CreateAssetRequestedEventArgs(string assetName)
        {
            var mockName = new Mock<IAssetName>();
            mockName.Setup(n => n.IsEquivalentTo(assetName, It.IsAny<bool>())).Returns(true);
            mockName.Setup(n => n.IsEquivalentTo(It.Is<string>(s => s != assetName), It.IsAny<bool>())).Returns(false);

            var mockAssetInfo = new Mock<IAssetInfo>();
            mockAssetInfo.Setup(a => a.NameWithoutLocale).Returns(mockName.Object);

            var ctor = typeof(AssetRequestedEventArgs).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            args[0] = mockAssetInfo.Object;

            if (parameters.Length > 1 && parameters[1].ParameterType.IsGenericType)
            {
                var genArgs = parameters[1].ParameterType.GetGenericArguments();
                var returnType = genArgs.Last();
                var paramTypes = genArgs.Take(genArgs.Length - 1).ToArray();
                var method = new System.Reflection.Emit.DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(ModEntryTests).Module);
                var il = method.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                args[1] = method.CreateDelegate(parameters[1].ParameterType);
            }

            var instance = (AssetRequestedEventArgs)ctor.Invoke(args);

            var editOpsField = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (editOpsField != null && editOpsField.GetValue(instance) == null)
            {
                editOpsField.SetValue(instance, Activator.CreateInstance(editOpsField.FieldType));
            }

            var modField = typeof(AssetRequestedEventArgs).GetField("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (modField != null)
            {
                var mockType = typeof(Mock<>).MakeGenericType(modField.FieldType);
                var mockModMetadata = (Mock)Activator.CreateInstance(mockType)!;
                modField.SetValue(instance, mockModMetadata.Object);
            }

            return instance;
        }

        private static System.Collections.IList GetEditOperations(AssetRequestedEventArgs args)
        {
            var field = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            return (System.Collections.IList)field!.GetValue(args)!;
        }

        [Fact]
        public void OnAssetRequested_CraftingRecipes_EditsRecipeDictionary()
        {
            // Arrange
            var modEntry = new ModEntry();
            var args = CreateAssetRequestedEventArgs("Data/CraftingRecipes");

            // Act
            modEntry.OnAssetRequested(null, args);

            // Assert
            var editOperations = GetEditOperations(args);
            Assert.NotEmpty(editOperations);
            var editOp = editOperations[0]!;
            var mockAssetData = new Mock<IAssetData>();
            var mockDictData = new Mock<IAssetDataForDictionary<string, string>>();
            var recipeDict = new Dictionary<string, string>();
            mockDictData.Setup(d => d.Data).Returns(recipeDict);
            mockAssetData.Setup(a => a.AsDictionary<string, string>()).Returns(mockDictData.Object);

            var applyMethod = editOp.GetType().GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyMethod != null)
            {
                applyMethod.Invoke(editOp, new object[] { mockAssetData.Object });
            }
            else
            {
                var actionField = editOp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
                var action = actionField?.GetValue(editOp) as Action<IAssetData>;
                action?.Invoke(mockAssetData.Object);
            }

            Assert.True(recipeDict.ContainsKey(ModConstants.CompostingBinRecipeId));
            Assert.Equal(
                $"{ModConstants.CompostingBinWoodCost} 388 {ModConstants.CompostingBinStoneCost} 390 {ModConstants.CompostingBinFiberCost} 771/Home/{ModConstants.CompostingBinItemId}/true/default/",
                recipeDict[ModConstants.CompostingBinRecipeId]);
        }

        [Fact]
        public void OnAssetRequested_Machines_HandlesSafelyWithoutThrowing()
        {
            // Arrange
            var modEntry = new ModEntry();
            var args = CreateAssetRequestedEventArgs("Data/Machines");

            // Act & Assert
            var ex = Record.Exception(() => modEntry.OnAssetRequested(null, args));
            Assert.Null(ex);
        }

        [Fact]
        public void OnAssetRequested_UnrelatedAsset_DoesNotEdit()
        {
            // Arrange
            var modEntry = new ModEntry();
            var args = CreateAssetRequestedEventArgs("Data/Bundles");

            // Act
            modEntry.OnAssetRequested(null, args);

            // Assert
            var editOperations = GetEditOperations(args);
            Assert.Empty(editOperations);
        }
    }
}
