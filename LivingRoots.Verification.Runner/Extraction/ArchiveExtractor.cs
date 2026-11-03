using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LivingRoots.Verification.Runner.Extraction
{
    public sealed class ArchiveExtractor
    {
        public int MaxMembers { get; }

        public long MaxBytes { get; }

        public int MaxRatio { get; }

        public ArchiveExtractor()
            : this(10000, 2147483648L, 100)
        {
        }

        public ArchiveExtractor(int maxMembers, long maxBytes, int maxRatio)
        {
            MaxMembers = maxMembers;
            MaxBytes = maxBytes;
            MaxRatio = maxRatio;
        }

        public static bool Extract(string archivePath, string targetDir)
        {
            return Extract(archivePath, targetDir, null);
        }

        public static bool Extract(string archivePath, string targetDir, IDictionary<string, string>? expectedDigests)
        {
            return Extract(archivePath, targetDir, expectedDigests, 10000, 2147483648L, 100);
        }

        public static bool Extract(string archivePath, string targetDir, IDictionary<string, string>? expectedDigests, int maxMembers, long maxBytes, int maxRatio)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || string.IsNullOrWhiteSpace(targetDir))
            {
                return false;
            }

            if (!File.Exists(archivePath))
            {
                return false;
            }

            var created = new List<string>();
            try
            {
                using var zip = ZipFile.OpenRead(archivePath);
                var entries = zip.Entries.ToList();

                if (entries.Count > maxMembers)
                {
                    return false;
                }

                var names = new HashSet<string>(StringComparer.Ordinal);
                long totalUncompressed = 0;
                long totalCompressed = 0;

                foreach (var entry in entries)
                {
                    var name = entry.FullName;
                    if (!IsSafeEntryName(name))
                    {
                        return false;
                    }

                    var normalized = name.Replace('\\', '/');
                    if (!names.Add(normalized))
                    {
                        return false;
                    }

                    var unixMode = (int)(entry.ExternalAttributes >> 16);
                    if (unixMode != 0)
                    {
                        var fileType = unixMode & 0xF000;
                        if (fileType != 0 && fileType != 0x8000)
                        {
                            return false;
                        }
                    }

                    if (name.EndsWith("/", StringComparison.Ordinal) || name.EndsWith("\\", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    totalUncompressed += entry.Length;
                    totalCompressed += entry.CompressedLength;
                    if (totalUncompressed > maxBytes)
                    {
                        return false;
                    }

                    if (entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > maxRatio)
                    {
                        return false;
                    }
                }

                if (totalCompressed > 0 && totalUncompressed / totalCompressed > maxRatio)
                {
                    return false;
                }

                if (expectedDigests is not null)
                {
                    foreach (var entry in entries)
                    {
                        var key = entry.FullName.Replace('\\', '/');
                        if (!expectedDigests.ContainsKey(key) && !expectedDigests.ContainsKey(entry.FullName))
                        {
                            return false;
                        }
                    }
                }

                Directory.CreateDirectory(targetDir);
                var fullTarget = Path.GetFullPath(targetDir);
                if (!fullTarget.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    fullTarget += Path.DirectorySeparatorChar;
                }

                foreach (var entry in entries)
                {
                    var destPath = Path.Combine(targetDir, entry.FullName);
                    var fullDest = Path.GetFullPath(destPath);
                    if (!fullDest.StartsWith(fullTarget, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var parent = Path.GetDirectoryName(fullDest);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent!);
                    }

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(fullDest, FileMode.Create, FileAccess.Write))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                    created.Add(fullDest);

                    if (expectedDigests is not null)
                    {
                        var key = entry.FullName.Replace('\\', '/');
                        string expected;
                        if (!expectedDigests.TryGetValue(key, out expected!) && !expectedDigests.TryGetValue(entry.FullName, out expected!))
                        {
                            throw new InvalidDataException("missing digest");
                        }

                        var actualHash = ComputeSha256File(fullDest);
                        if (!string.Equals(actualHash, expected, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException("digest mismatch");
                        }
                    }
                }

                return true;
            }
            catch
            {
                foreach (var f in created)
                {
                    try
                    {
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                        }
                    }
                    catch
                    {
                    }
                }
                return false;
            }
        }

        public static int ExtractWithCode(string archivePath, string targetDir)
        {
            return Extract(archivePath, targetDir) ? 0 : 14;
        }

        private static bool IsSafeEntryName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Contains('\0'))
            {
                return false;
            }

            if (Path.IsPathRooted(name))
            {
                return false;
            }

            if (name.Length >= 2 && char.IsLetter(name[0]) && name[1] == ':')
            {
                return false;
            }

            if (name.Length > 4096)
            {
                return false;
            }

            var parts = name.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part == ".." || part == ".")
                {
                    return false;
                }

                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return false;
                }

                if (part.Length > 255)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ComputeSha256File(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
