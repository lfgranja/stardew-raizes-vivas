using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class CropPhaseDumpWriterTests
    {
        [Fact]
        public void Write_DeletesStaleSentinel_WritesPayloadThenDoneLast()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dump_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var jsonPath = Path.Combine(dir, "crop_phase_dump_spring_1_1.json");
                var donePath = jsonPath + ".done";
                File.WriteAllText(jsonPath, "{\"done\":false}");
                File.WriteAllText(donePath, "stale");

                var dump = new CropPhaseDump
                {
                    DayKey = "spring_1_1",
                    CapturedAtUtc = DateTime.UtcNow.ToString("o"),
                    Done = false,
                    Crops = new List<CropPhaseEntry>(),
                };

                Assert.True(CropPhaseDumpWriter.Write(dir, "spring_1_1", dump));
                Assert.True(File.Exists(jsonPath));
                Assert.True(File.Exists(donePath));
                Assert.False(File.Exists(jsonPath + ".tmp"));

                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                Assert.True(doc.RootElement.GetProperty("done").GetBoolean());

                // The sidecar is written last; its presence must mean the
                // payload is already complete.
                Assert.Equal("done", File.ReadAllText(donePath));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public async Task WriteAsync_CompletesWithTheSameContract()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dump_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var dump = new CropPhaseDump
                {
                    DayKey = "spring_2",
                    CapturedAtUtc = DateTime.UtcNow.ToString("o"),
                    Crops = new List<CropPhaseEntry>(),
                };

                Assert.True(await CropPhaseDumpWriter.WriteAsync(dir, "spring_2", dump));
                Assert.True(File.Exists(Path.Combine(dir, "crop_phase_dump_spring_2.json")));
                Assert.True(File.Exists(Path.Combine(dir, "crop_phase_dump_spring_2.json.done")));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
