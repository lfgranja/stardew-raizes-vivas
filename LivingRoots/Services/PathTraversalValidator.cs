using System;
using System.IO;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation for validating and preventing path traversal attacks.
    /// </summary>
    public class PathTraversalValidator : IPathTraversalValidator
    {
        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        public void Validate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            // 1. Reject URI schemes.
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile)
                throw new ArgumentException("Path cannot be an absolute URI.", nameof(path));

            // 2. Check for encoded traversal patterns first by looking for encoded dots and slashes
            string lowerPath = path.ToLowerInvariant();
            if (lowerPath.Contains("%2e%2e") || lowerPath.Contains("%2e%2e%") || 
                lowerPath.Contains("..%2f") || lowerPath.Contains("..%5c") || 
                lowerPath.Contains("%2f..") || lowerPath.Contains("%5c.."))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns.", nameof(path));
            }

            // 3. Normalize path separators and decode encoded characters.
            string normalizedPath = path.Replace('\\', '/');
            string decodedPath = Uri.UnescapeDataString(normalizedPath);

            // 4. Check for absolute paths (both Unix and Windows style).
            // Check for Windows drive paths like "C:\Windows\System32" or "C:/Windows/System32"
            if (decodedPath.Length >= 2 && char.IsLetter(decodedPath[0]) && decodedPath[1] == ':' && 
                (decodedPath.Length > 2 && (decodedPath[2] == '\\' || decodedPath[2] == '/')))
                throw new ArgumentException("Path cannot be an absolute path.", nameof(path));
            
            // Check for Unix-style absolute paths
            if (Path.IsPathRooted(decodedPath) || decodedPath.StartsWith("/"))
                throw new ArgumentException("Path cannot be an absolute path.", nameof(path));

            // 5. Check for traversal patterns in segments.
            string[] segments = decodedPath.Split('/');
            foreach (string segment in segments)
            {
                if (segment == ".." || segment == ".")
                    throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }

            // 6. Check for double-dot patterns at the start or end
            if (decodedPath.ToLowerInvariant().StartsWith("..") || decodedPath.ToLowerInvariant().EndsWith(".."))
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
        }
    }
}