using System.Collections.Generic;
using LivingRoots.Domain.Models;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    public class CompostingBinPersistenceTests
    {
        /// <summary>T123 [P] [US2] RED — FR-012 (partial): null payload isolates per location.</summary>
        [Fact]
        public void T123_NullLocationPayload_IsolatesThatLocation_Only()
        {
            // RED: a null inner dictionary for one location aborts the entire load
            // instead of isolating that location and allowing remaining locations
            // to load correctly (FR-012 partial).
            var corruptData = new CompostingBinData();
            corruptData.LocationBinData["CorruptFarm"] = null!; // null payload for this location
            corruptData.LocationBinData["ValidFarm"] = new Dictionary<string, CompostingBinStateData>
            {
                ["10,10"] = new CompostingBinStateData
                {
                    State = "Ready",
                    InputItemId = "(O)24",
                    InputTimestamp = 1,
                    MaturationLevel = 1
                }
            };

            var mockData = new Mock<IModDataService>();
            mockData.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns(corruptData);

            var service = new CompostingBinService(
                mockData.Object,
                new Mock<ISaveIdProvider>().Object,
                new Mock<IOrganicWasteValidator>().Object,
                new Mock<IMonitor>().Object,
                new TimeProviderStub(),
                new CompostingBinFactory(),
                new RecordingPlayerInventory());

            // The load either throws and catches (aborting everything) or skips.
            // In either case, the valid farm should still be accessible if isolated.
            service.LoadData("test-null-payload");

            // RED: with current behavior, the null payload aborts the whole load,
            // so ValidFarm never registers and stays Empty instead of Ready.
            Assert.Equal(CompostingBinState.Ready, service.GetBinState("ValidFarm", new Vector2(10, 10)));
            Assert.Equal(CompostingBinState.Empty, service.GetBinState("CorruptFarm", new Vector2(10, 10)));
        }
    }
}
