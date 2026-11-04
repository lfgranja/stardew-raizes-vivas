using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LivingRoots.Verification.Evidence
{
    /// <summary>
    /// Writes a crop-phase dump with the C-DMP-01/C-DMP-02 ordering:
    /// delete stale sentinel, temp write, fsync file, atomic rename,
    /// fsync parent directory, then write the `.done` sentinel last.
    /// The async entry point keeps the fsync work off the game thread.
    /// </summary>
    public static class CropPhaseDumpWriter
    {
        private const int OpenReadOnly = 0;

        [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string pathname, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int fsync(int fd);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);

        public static Task<bool> WriteAsync(string directory, string dayKey, CropPhaseDump dump)
        {
            return Task.Run(() => Write(directory, dayKey, dump));
        }

        public static bool Write(string directory, string dayKey, CropPhaseDump dump)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var jsonPath = Path.Combine(directory, $"crop_phase_dump_{dayKey}.json");
                var donePath = jsonPath + ".done";

                // 1. Delete a stale sentinel before touching the payload.
                if (File.Exists(donePath))
                {
                    File.Delete(donePath);
                }

                dump.Done = true;
                var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
                var tmp = jsonPath + ".tmp";

                // 2-3. Temp write, then fsync the file.
                using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true))
                    {
                        writer.Write(json);
                        writer.Flush();
                    }

                    stream.Flush(flushToDisk: true);
                }

                // 4. Atomic rename over the final path.
                File.Move(tmp, jsonPath, overwrite: true);

                // 5. fsync the parent directory so the rename is durable.
                FsyncDirectory(directory);

                // 6. Write the sentinel last and fsync it.
                using (var stream = new FileStream(donePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = Encoding.UTF8.GetBytes("done");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(flushToDisk: true);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FsyncDirectory(string directory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            int fd = open(directory, OpenReadOnly);
            if (fd < 0)
            {
                return;
            }

            try
            {
                _ = fsync(fd);
            }
            finally
            {
                _ = close(fd);
            }
        }
    }
}
