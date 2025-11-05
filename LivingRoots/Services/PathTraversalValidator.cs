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
            if (path == null)
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            
            path = path.Trim();
            
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Check for absolute URIs first
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Check for encoded traversal patterns first by looking for encoded dots and slashes
            string lowerPath = path.ToLowerInvariant();
            if (lowerPath.Contains("%2e%2e") || lowerPath.Contains("%2e%2e%") || 
                lowerPath.Contains("..%2f") || lowerPath.Contains("..%5c") || 
                lowerPath.Contains("%2f..") || lowerPath.Contains("%5c.."))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns.", nameof(path));
            }

            // Check for Windows drive paths like "C:\Windows\System32" or "C:/Windows/System32" before normalization
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':' && 
                (path.Length > 2 && (path[2] == '\\' || path[2] == '/')))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));
            
            // Normalize all separators to '/'
            string normalized = path.Replace('\\', '/');

            // Check for Unix-style absolute paths (starting with '/')
            if (normalized.StartsWith("/"))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Check for URL patterns that might not be caught by Uri.IsWellFormedUriString
            if (normalized.StartsWith("http://") || normalized.StartsWith("https://") || 
                normalized.StartsWith("ftp://") || normalized.StartsWith("file://"))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Split path into segments and validate each segment
            string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int depth = 0;
            
            foreach (string segment in segments)
            {
                if (segment == "..")
                {
                    depth--;
                    // If depth goes negative, it means we're trying to go above the current directory
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
                    }
                }
                else if (segment == ".")
                {
                    // Block explicit "." segments which represent current directory navigation
                    throw new ArgumentException("Path cannot contain relative path navigation.", nameof(path));
                }
                else if (!string.IsNullOrEmpty(segment))
                {
                    // Going into a subdirectory increases depth
                    depth++;
                }
            }

            // Additional check for explicit "." patterns at start/end of path
            if (normalized.StartsWith("./") || normalized.EndsWith("/.") || normalized == ".")
            {
                throw new ArgumentException("Path cannot contain relative path navigation.", nameof(path));
            }
        }
    }
}