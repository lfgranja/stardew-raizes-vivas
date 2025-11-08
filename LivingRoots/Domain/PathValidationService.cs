using System;
using System.Globalization;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Implementation for validating and preventing path traversal attacks.
    /// This implementation follows the Dependency Inversion Principle by depending on abstractions.
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

            // Additional check for explicit "." patterns at start of path
            // Block "." as a standalone path, and paths starting with "./" since these represent explicit directory navigation
            if (normalized == "." || 
                normalized == "./" || 
                normalized.StartsWith("./"))   // Block any path starting with "./"
            {
                throw new ArgumentException("Path cannot contain relative path navigation.", nameof(path));
            }
            
            // Check for path traversal patterns at the beginning
            if (normalized == ".." || 
                normalized.StartsWith("../"))      // Block "../" patterns
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }

            // Split path into segments and validate each segment
            string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Additional check: count actual directory levels vs parent directory references
            // This catches cases like "folder/./../file.txt" where we enter one directory and then exit it
            int actualDirLevels = 0; // Count of actual directory names (not "." or "..")
            int parentDirRefs = 0;   // Count of ".." segments
            
            foreach (string segment in segments)
            {
                if (segment == "..")
                {
                    parentDirRefs++;
                }
                else if (segment != ".")
                {
                    actualDirLevels++;
                }
            }
            
            // Process segments to detect path traversal attempts
            int depth = 0;
            foreach (string segment in segments)
            {
                if (segment == "..")
                {
                    depth--;
                    // If depth goes negative, it means we're trying to go above the current directory
                    // This happens when we encounter more ".." segments than directory levels we've gone into
                    if (depth < 0)
                    {
                        throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
                    }
                }
                else if (segment == ".")
                {
                    // Allow "." segments as they represent current directory and are generally safe
                    // This is important for paths like "folder/.config/file.txt" where ".config" is a hidden directory
                    // The check above already blocks paths that start or end with "." or "./"
                    continue; // Continue processing instead of throwing
                }
                else if (!string.IsNullOrEmpty(segment))
                {
                    // Going into a subdirectory increases depth
                    depth++;
                }
            }
            
            // Additional check: If final depth is 0 or negative and we had ".." segments,
            // it means we've potentially traversed out of the intended directory context
            if (parentDirRefs > 0 && depth <= 0)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }
            
            // If we have more parent directory references than actual directory levels we've entered,
            // it indicates an attempt to traverse above the current directory context
            // This includes disguised traversal attempts like "folder/./../file.txt" where we enter one directory and then exit it
            if (parentDirRefs > actualDirLevels)
            {
                throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
            }
            
            // Special case: if we have parent directory references equal to or greater than 
            // the number of directory levels we entered before the final component,
            // and we've entered at least one directory, it's also a traversal attempt
            // e.g. "folder/../file.txt" (1 ".." >= 1 dir "folder")  
            // e.g. "folder/subfolder/../../file.txt" (2 ".." >= 2 dirs "folder","subfolder")
            if (parentDirRefs > 0)
            {
                // Count directories we went into (excluding final component if it's likely a file)
                int dirsBeforeFinal = 0;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i] != ".." && segments[i] != ".")
                    {
                        dirsBeforeFinal++;
                    }
                }
                
                // If parent directory references are >= directories before final component, it's a traversal
                if (parentDirRefs >= dirsBeforeFinal)
                {
                    throw new ArgumentException("Path cannot contain path traversal patterns.", nameof(path));
                }
            }
        }
        
        /// <summary>
        /// Normalizes Unicode homoglyphs of path separators and rejects invisible characters.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        private static string NormalizePathSeparators(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            // Normalize Unicode homoglyphs of path separators
            // Replace common Unicode path separators with standard forward slash
            path = path.Replace('\u2215', '/'); // Division slash
            path = path.Replace('\u2044', '/'); // Fraction slash
            
            // Reject invisible characters that could be used for obfuscation
            var result = new System.Text.StringBuilder();
            foreach (char c in path)
            {
                // Check if character is invisible (control characters except whitespace)
                if (char.IsControl(c) && c != '\t' && c != '\r' && c != '\n')
                {
                    // Reject invisible characters
                    throw new ArgumentException("Path cannot contain invisible characters.", nameof(path));
                }
                result.Append(c);
            }
            
            return result.ToString();
        }
    }
}