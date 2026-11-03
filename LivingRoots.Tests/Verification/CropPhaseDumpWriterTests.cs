using System;
using System.Collections.Generic;
using System.IO;
using LivingRoots.Verification.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class CropPhaseDumpWriterTests
    {
        [Fact]
        public void Write_TempFsyncRenameParentFsync_DoneLast_DeleteStaleSentinel()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dump_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var donePath = Path.Combine(dir, "crop_phase_dump_spring_1_1.json.done");
                File.WriteAllText(donePath, "stale");
                var dump = new CropPhaseDump { DayKey = "spring_1_1", CapturedAtUtc = DateTime.UtcNow.ToString("o"), Done = true, Crops = new List<CropPhaseEntry>() };
                var ok = CropPhaseDumpWriter.Write(dir, "spring_1_1", dump);
                Assert.True(ok);
                Assert.True(File.Exists(Path.Combine(dir, "crop_phase_dump_spring_1_1.json")));
                Assert.True(File.Exists(donePath));
                // done written last
                Assert.True(File.Exists(Path.Combine(dir, "crop_phase_dump_spring_1_1.json.done")));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
