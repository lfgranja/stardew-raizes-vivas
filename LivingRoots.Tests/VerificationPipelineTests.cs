using System.Collections;
using System.Reflection;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Contract test for SC-008 rollback verification. Documents and verifies that
/// the <see cref="CompostingBinService.CollectCompost"/> rollback mechanism
/// exists and behaves correctly: when the inventory refuses the compost, the
/// bin retains its Ready state (the batch is never consumed).
/// </summary>
/// <remarks>
/// The authoritative rollback test
/// <c>CollectCompost_WhenInventoryRefuses_RetainsBinReadyState</c> lives in
/// <c>CompostingBinServiceTests.cs</c>. This file provides the complementary
/// verification pipeline contract: it verifies the rollback mechanism through
/// the public API and documents the prerequisite for yield activation (FR-005).
/// </remarks>
public class VerificationPipelineTests
{
    private readonly Mock<IModDataService> _mockModDataService;
    private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
    private readonly Mock<IOrganicWasteValidator> _mockWasteValidator;
    private readonly Mock<IMonitor> _mockMonitor;
    private readonly TimeProviderStub _timeStub;
    private readonly CompostingBinFactory _factory;
    private readonly RecordingPlayerInventory _inventory;
    private readonly CompostingBinService _service;

    public VerificationPipelineTests()
    {
        GameStateFixture.Install();

        _mockModDataService = new Mock<IModDataService>();
        _mockSaveIdProvider = new Mock<ISaveIdProvider>();
        _mockWasteValidator = new Mock<IOrganicWasteValidator>();
        _mockMonitor = new Mock<IMonitor>();
        _timeStub = new TimeProviderStub();
        _factory = new CompostingBinFactory();
        _inventory = new RecordingPlayerInventory();

        _service = new CompostingBinService(
            _mockModDataService.Object,
            _mockSaveIdProvider.Object,
            _mockWasteValidator.Object,
            _mockMonitor.Object,
            _timeStub,
            _factory,
            _inventory);
    }

    // ------------------------------------------------------------------
    // SC-008: rollback mechanism exists and is documented
    // ------------------------------------------------------------------

    /// <summary>
    /// SC-008: the <see cref="CompostingBinService.CollectCompost"/> rollback
    /// mechanism exists as a public method on the service. This test verifies
    /// the method is callable and has the correct signature.
    /// </summary>
    [Fact]
    public void CollectCompost_MethodExists_WithCorrectSignature()
    {
        // Arrange
        var method = typeof(CompostingBinService).GetMethod(
            "CollectCompost",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(int), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(Vector2), parameters[1].ParameterType);
    }

    /// <summary>
    /// SC-008: the rollback test
    /// <c>CollectCompost_WhenInventoryRefuses_RetainsBinReadyState</c> exists
    /// in <c>CompostingBinServiceTests.cs</c>. This test verifies the
    /// verification pipeline prerequisite is met by confirming the test
    /// method exists via reflection.
    /// </summary>
    [Fact]
    public void RollbackTest_ExistsInCompostingBinServiceTests()
    {
        // Arrange
        var testType = typeof(CompostingBinServiceTests);
        var method = testType.GetMethod(
            "CollectCompost_WhenInventoryRefuses_RetainsBinReadyState",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert — the test exists, documenting the SC-008 prerequisite
        Assert.NotNull(method);
        Assert.True(method!.GetParameters().Length == 0);
    }

    /// <summary>
    /// SC-008: when the inventory refuses the compost, the bin retains its
    /// Ready state. The batch is never consumed on a failed collection attempt.
    /// This is the core rollback invariant.
    /// </summary>
    [Fact]
    public void CollectCompost_WhenInventoryRefuses_BinRetainsReadyState()
    {
        // Arrange
        var item = ItemFactory.CreateValidWasteItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Act
        var result = _service.CollectCompost("Farm", new Vector2(10, 10));

        // Assert — rollback: bin stays Ready, no items granted
        Assert.Equal(0, result);
        Assert.Empty(_inventory.AddedItems);
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    /// <summary>
    /// SC-008: after a rollback (inventory refusal), a subsequent collection
    /// attempt with an available inventory succeeds. The batch was retained,
    /// not destroyed, by the failed attempt.
    /// </summary>
    [Fact]
    public void CollectCompost_AfterRollback_WithAvailableInventory_Succeeds()
    {
        // Arrange
        var item = ItemFactory.CreateValidWasteItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        // First attempt: inventory refuses (rollback)
        var firstResult = _service.CollectCompost("Farm", new Vector2(10, 10));
        Assert.Equal(0, firstResult);
        Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));

        // Act: second attempt with available inventory
        _inventory.AcceptsItems = true;
        var secondResult = _service.CollectCompost("Farm", new Vector2(10, 10));

        // Assert — the retained batch was collected
        Assert.Equal(1, secondResult);
        Assert.Single(_inventory.AddedItems);
        Assert.Equal(CompostingBinState.Empty, _service.GetBinState("Farm", new Vector2(10, 10)));
    }

    /// <summary>
    /// SC-008: repeated collection attempts from a full inventory never
    /// destroy the finished compost. The bin stays Ready across multiple
    /// refusals, and no items are ever added to the inventory.
    /// </summary>
    [Fact]
    public void CollectCompost_RepeatedAttemptsFromFullInventory_NeverDestroysCompost()
    {
        // Arrange
        var item = ItemFactory.CreateValidWasteItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        // Act: multiple attempts
        for (int i = 0; i < 5; i++)
        {
            var result = _service.CollectCompost("Farm", new Vector2(10, 10));

            // Assert — batch retained after each attempt
            Assert.Equal(0, result);
            Assert.Equal(CompostingBinState.Ready, _service.GetBinState("Farm", new Vector2(10, 10)));
            Assert.Empty(_inventory.AddedItems);
        }
    }

    /// <summary>
    /// SC-008: the rollback mechanism logs a warning when the inventory
    /// refuses the compost, providing observability for the refusal event.
    /// </summary>
    [Fact]
    public void CollectCompost_WhenInventoryRefuses_LogsWarning()
    {
        // Arrange
        var item = ItemFactory.CreateValidWasteItem();
        _mockWasteValidator.Setup(x => x.IsValidOrganicWaste(item)).Returns(true);
        _inventory.AcceptsItems = false;

        _timeStub.TotalDays = 1;
        _service.AddWaste("Farm", new Vector2(10, 10), item);
        _timeStub.TotalDays = 3;
        _service.ProcessDayStart("Farm");

        // Act
        _service.CollectCompost("Farm", new Vector2(10, 10));

        // Assert
        _mockMonitor.Verify(
            x => x.Log(It.Is<string>(s => s.Contains("inventory full")), LogLevel.Warn),
            Times.Once);
    }
}
