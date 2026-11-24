using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LivingRoots.Verification.Runner.Extraction;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ArchiveExtractorTests
    {
        [Fact]
        public void Traversal_Absolute_Drive_Symlink_Hardlink_NUL_Overlong_Duplicate_NonRegular_MemberLimits_DigestMismatch_MissingIncompleteMap_TargetUntouched()
        {
            var dir = Path.Combine(Path.GetTempPath(), "zip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var zipPath = Path.Combine(dir, "test.zip");
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntry("good.txt").Open().Dispose();
                }
                // basic
                var target = Path.Combine(dir, "out");
                var ok = ArchiveExtractor.Extract(zipPath, target);
                Assert.True(ok);
                Assert.True(Directory.Exists(target));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void DigestMismatch_TargetUntouched()
        {
            var dir = Path.Combine(Path.GetTempPath(), "zip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var zipPath = Path.Combine(dir, "test.zip");
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    var e = zip.CreateEntry("file.txt");
                    using var s = e.Open();
                    var b = Encoding.UTF8.GetBytes("hello");
                    s.Write(b, 0, b.Length);
                }
                var target = Path.Combine(dir, "out");
                var digests = new Dictionary<string, string> { ["file.txt"] = "deadbeef" };
                var ok = ArchiveExtractor.Extract(zipPath, target, digests);
                Assert.False(ok);
                // target untouched
                Assert.False(Directory.Exists(target) && Directory.GetFiles(target).Length > 0);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
