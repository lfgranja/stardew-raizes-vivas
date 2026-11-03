using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LivingRoots.Verification.Runner.Evidence
{
    public sealed class FileEntry
    {
        public string Name { get; set; } = string.Empty;

        public Digest Digest { get; set; } = new Digest();
    }

    public sealed class Digest
    {
        public string Sha256 { get; set; } = string.Empty;
    }

    public sealed class ManifestBuilder
    {
        public ManifestBuilder()
        {
        }

        public static string Build(string directory, object data)
        {
            return BuildManifest(directory, data);
        }

        public static string BuildManifest(string directory, object data)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("directory is required", nameof(directory));
            }

            Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            var manifestPath = Path.Combine(directory, "manifest.json");
            File.WriteAllText(manifestPath, json);

            var hash = ComputeSha256String(json);
            File.WriteAllText(Path.Combine(directory, "manifest.sha256"), hash);

            return manifestPath;
        }

        public static string BuildPerAttempt(string directory, object data)
        {
            return BuildManifest(directory, data);
        }

        public static bool TryBuild(string directory, object data, out string manifestPath)
        {
            try
            {
                manifestPath = BuildManifest(directory, data);
                return true;
            }
            catch
            {
                manifestPath = string.Empty;
                return false;
            }
        }

        public static int BuildManifest(string directory, object data, out string manifestPath)
        {
            try
            {
                manifestPath = BuildManifest(directory, data);
                return 0;
            }
            catch
            {
                manifestPath = string.Empty;
                return 19;
            }
        }

        public static string ComputeSha256File(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static string ComputeSha256String(string content)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
