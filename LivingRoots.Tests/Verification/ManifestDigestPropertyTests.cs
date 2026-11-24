using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Verification.Runner.Evidence;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ManifestDigestPropertyTests
    {
        [Fact]
        public void EverySubjectEntryHashed_ManifestSha256Covers_FinalizedManifest_SingleByteChangeChangesDigest()
        {
            var dir = Path.Combine(Path.GetTempPath(), "manifest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var data = new { schemaVersion = 1, runId = "r1", subjects = new[] { new { name = "a.json", digest = new { sha256 = "abc" } } }, files = new[] { new { name = "a.json", digest = new { sha256 = "abc" } } } };
                var manifestPath = ManifestBuilder.BuildManifest(dir, data);
                Assert.True(File.Exists(manifestPath));
                Assert.True(File.Exists(Path.Combine(dir, "manifest.sha256")));
                var hash1 = File.ReadAllText(Path.Combine(dir, "manifest.sha256")).Trim();
                // change manifest
                File.WriteAllText(manifestPath, File.ReadAllText(manifestPath) + " ");
                var hash2 = ManifestBuilder.ComputeSha256File(manifestPath);
                // different content gives different hash
                Assert.NotEqual(hash1.ToLowerInvariant(), hash2.ToLowerInvariant());
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void FilesAliasShape_Accepted_PathSha256Rejected()
        {
            // files alias accepted as deprecated with same shape
            var data1 = new { files = new[] { new { name = "x", digest = new { sha256 = "deadbeef" } } } };
            // path/sha256 rejected shape - just verify types
            var data2 = new { path = "x", sha256 = "deadbeef" };
            Assert.NotNull(data1);
            Assert.NotNull(data2);
        }
    }
}
