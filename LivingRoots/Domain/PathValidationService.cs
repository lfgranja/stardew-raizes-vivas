using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for validating and preventing path traversal attacks.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
    /// Uses simplified depth-based traversal detection for better maintainability.
    /// </summary>
    public class PathValidationService : IPathValidationService
    {
        /// <summary>
        /// Initializes a new instance of the PathValidationService class.
        /// </summary>
        public PathValidationService()
        {
        }
        
        /// <summary>
        /// Validates that a path does not contain path traversal patterns.
        /// Uses depth tracking to detect traversal attempts above the root directory.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        public void Validate(string path)
        {
            if (path == null)
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            
            // Normalize Unicode homoglyphs of path separators and reject invisible characters
            path = NormalizePathSeparators(path);
            
            path = path.Trim();
            
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Check for absolute URIs first
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Check for encoded traversal patterns by attempting URL decoding once
            string decodedPath;
            try
            {
                decodedPath = Uri.UnescapeDataString(path);
            }
            catch (Exception)
            {
                // If decoding fails, conservatively reject the path
                throw new ArgumentException("Path contains invalid encoded characters.", nameof(path));
            }
            
            // Normalize all separators to '/'
            string normalized = path.Replace('\\', '/');
            
            // Check for path traversal patterns at the beginning (these should always be blocked)
            if (normalized == ".." || 
                normalized.StartsWith("../"))      // Block "../" patterns at the start
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }

            // Check the decoded string for encoded traversal patterns
            string lowerDecodedPath = decodedPath.ToLowerInvariant();
            if (decodedPath != path && (lowerDecodedPath.Contains("..") || 
                lowerDecodedPath.Contains("../") || 
                lowerDecodedPath.Contains("..\\")))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns.", nameof(path));
            }
            
            // Also check the original raw path as an extra layer of security
            if (path.ToLowerInvariant().Contains("%2e%2e") || path.ToLowerInvariant().Contains("%2e%2e%") || 
                path.ToLowerInvariant().Contains("..%2f") || path.ToLowerInvariant().Contains("..%5c") || 
                path.ToLowerInvariant().Contains("%2f..") || path.ToLowerInvariant().Contains("%5c.."))
            {
                throw new ArgumentException("Path cannot contain encoded path traversal patterns.", nameof(path));
            }

            // Check for Windows drive paths like "C:\Windows\System32" or "C:/Windows/System32" before normalization
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':' && 
                (path.Length > 2 && (path[2] == '\\' || path[2] == '/')))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Check for Unix-style absolute paths (starting with '/')
            if (normalized.StartsWith("/"))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Check for URL patterns that might not be caught by Uri.IsWellFormedUriString
            if (normalized.StartsWith("http://") || normalized.StartsWith("https://") || 
                normalized.StartsWith("ftp://") || normalized.StartsWith("file://"))
                throw new ArgumentException("Path cannot be an absolute path or URI.", nameof(path));

            // Additional check for explicit "." patterns at start of path
            // Block "." as a standalone path, and paths starting with "./" since these represent explicit directory navigation
            if (normalized == "." || 
                normalized == "./" || 
                normalized.StartsWith("./"))   // Block any path starting with "./"
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }
            
            
            
            // Check for problematic explicit current directory followed by parent directory patterns
            if (normalized.Contains("./../"))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }
            
            
            
            

            // Use depth tracking to detect traversal attempts
            ValidatePathDepth(normalized);
        }
        
        /// <summary>
        /// Validates path using depth tracking to prevent traversal above root.
        /// This is the simplified approach that replaces multiple overlapping checks.
        /// </summary>
        /// <param name="normalizedPath">The normalized path with '/' separators</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected.</exception>
        private void ValidatePathDepth(string normalizedPath)
        {
            string[] segments = normalizedPath.Split('/');
            int depth = 0;
            int minDepth = 0; // Track minimum depth reached to detect traversal attempts
            bool previousWasDotDot = false;

            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                    continue; // Skip empty segments (can happen with consecutive slashes)
                
                if (segment == "..")
                {
                    // Check for consecutive ".." segments (complex traversal pattern)
                    if (previousWasDotDot)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(normalizedPath));
                    }
                    
                    depth--;
                    minDepth = Math.Min(minDepth, depth); // Track if we go below starting depth
                    previousWasDotDot = true;
                    
                    // If depth goes negative, we're trying to traverse above root
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(normalizedPath));
                    }
                }
                else if (segment == ".")
                {
                    // Current directory - no change in depth
                    previousWasDotDot = false;
                    continue;
                }
                else
                {
                    // Normal directory or file - increment depth
                    depth++;
                    previousWasDotDot = false;
                }
            }

            // Check if the last segment is ".." which would indicate traversal
            if (normalizedPath.EndsWith("/..") || normalizedPath.EndsWith(".."))
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(normalizedPath));
            }
            
            // Check if we had any net traversal attempt (minDepth < 0 means we went above root at some point)
            if (minDepth < 0)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(normalizedPath));
            }
            
            
        }
        
        /// <summary>
        /// Normalizes Unicode homoglyphs of path separators and rejects invisible characters
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>The normalized path</returns>
        private string NormalizePathSeparators(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            var result = new StringBuilder();
            
            foreach (char c in path)
            {
                // Reject invisible characters (control characters except tab, newline, carriage return)
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    throw new ArgumentException("Path cannot contain invisible characters.", nameof(path));
                }
                
                // Normalize Unicode homoglyphs of path separators
                if (c == '／') // Fullwidth solidus (U+FF0F)
                    result.Append('/');
                else if (c == '＼') // Fullwidth reverse solidus (U+FF3C)
                    result.Append('/');
                else
                    result.Append(c);
            }
            
            return result.ToString();
        }
    }
}