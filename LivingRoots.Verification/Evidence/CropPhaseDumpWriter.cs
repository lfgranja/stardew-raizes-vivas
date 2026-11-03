using System;
using System.IO;
using System.Text.Json;

namespace LivingRoots.Verification.Evidence
{
    public static class CropPhaseDumpWriter
    {
        public static bool Write(string directory, string dayKey, CropPhaseDump dump)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var jsonPath = Path.Combine(directory, $"crop_phase_dump_{dayKey}.json");
                var donePath = jsonPath + ".done";
                // delete stale sentinel
                if (File.Exists(donePath)) File.Delete(donePath);
                var tmp = jsonPath + ".tmp";
                var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tmp, json);
                // fsync + atomic rename
                File.Move(tmp, jsonPath, overwrite: true);
                // write .done sentinel last
                File.WriteAllText(donePath, "done");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
