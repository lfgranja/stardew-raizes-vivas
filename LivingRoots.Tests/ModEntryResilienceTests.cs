using System;
using System.Collections.Generic;
using System.Reflection;
using LivingRoots;
using Moq;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T228 (FR-014, F8 partial): a failure inside the <see cref="ModEntry.Entry"/>
    /// wiring path must not prevent the mod from loading. The failure is
    /// reported in sanitized form (<c>GetType().FullName</c> + <c>HResult</c>)
    /// and never carries raw exception detail (<c>ex.Message</c> /
    /// <c>ex.StackTrace</c>). Fixed failure injection — property-based N/A.
    /// RED: <c>ModEntry.cs</c> contains zero try/catch, so the injected throw
    /// propagates out of <c>Entry</c> and this test fails.
    /// </summary>
    public class ModEntryResilienceTests
    {
        [Fact]
        public void T228_Entry_WhenWiringThrows_DoesNotPreventModLoadAndLogsSanitizedOnly()
        {
            // Arrange — a SMAPI-loader-shaped host (Monitor and ModManifest
            // populated by reflection, as the loader does before Entry), per
            // the MultiplayerConcurrencyTests production-wiring pattern
            // (ProductionWiring_ControllerNeverCarriesNullHostAtomicLockService).
            const string rawSentinel = "T228_SENTINEL_RAW_DETAIL_7f3a9c1e";
            var wiringFailure = new InvalidOperationException(rawSentinel);

            var mockMonitor = new Mock<IMonitor>();
            var loggedMessages = new List<string>();
            mockMonitor
                .Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((message, level) =>
                {
                    loggedMessages.Add(message);
                });

            var mockManifest = new Mock<IManifest>();
            mockManifest.Setup(m => m.UniqueID).Returns("test.mod.id");
            mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));

            var modEntry = new ModEntry();
            typeof(Mod).GetProperty("Monitor")!.SetValue(modEntry, mockMonitor.Object);
            typeof(Mod).GetProperty("ModManifest")!.SetValue(modEntry, mockManifest.Object);

            // Fixed failure injection on the wiring path: the very first
            // helper access in Entry (helper.Events.Content.AssetRequested +=
            // ..., ModEntry.cs:50) throws, per the secure-logging plan pattern
            // (mockHelper.Setup(x => x.Events).Throws(...)). Hermetic — Moq
            // interfaces only, no game loop runs.
            var mockHelper = new Mock<IModHelper>();
            mockHelper.Setup(h => h.Events).Throws(wiringFailure);

            // Act
            var exception = Record.Exception(() =>
            {
                modEntry.Entry(mockHelper.Object);
            });

            // Assert — the mod still loads: Entry swallows the wiring failure
            // instead of propagating it (RED today: Entry has no try/catch, so
            // the InvalidOperationException escapes and this fails).
            Assert.Null(exception);

            // Assert — the failure is reported in sanitized form only:
            // exception type FullName + HResult, never the raw message or a
            // stack trace, per the FR-015 codebase convention
            // (CompostingBinService / SoilHealthService catch sites).
            mockMonitor.Verify(
                m => m.Log(
                    It.Is<string>(s => s.Contains(typeof(InvalidOperationException).FullName!) && s.Contains("HResult")),
                    It.IsAny<LogLevel>()),
                Times.AtLeastOnce);
            Assert.All(loggedMessages, message =>
            {
                Assert.DoesNotContain(rawSentinel, message);
            });
            string stackTrace = wiringFailure.StackTrace ?? string.Empty;
            if (stackTrace.Length > 0)
            {
                Assert.All(loggedMessages, message =>
                {
                    Assert.DoesNotContain(stackTrace, message);
                });
            }
        }
    }
}
