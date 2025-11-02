using StardewModdingAPI;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace LivingRoots.Services
{
    /// <summary>
    /// Service for handling mod data persistence and SMAPI interactions
    /// Following the architecture pattern described in ARCHITECTURE.md
    /// </summary>
    public class ModDataService : IModDataService
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        
        public ModDataService(IModHelper helper, IMonitor monitor)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }
        
        /// <summary>
        /// Save mod data to persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to save (must be a reference type)</typeparam>
        /// <param name="data">Data to save</param>
        /// <param name="key">Key to identify the data</param>
        public void SaveData<T>(T data, string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            
            var path = GetFilePath(key);
            try
            {
                _helper.Data.WriteJsonFile(path, data);
                _monitor.Log($"Saved data for key '{key}'.", LogLevel.Trace);
            }
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while saving data for key '{key}' to '{path}': {ex.Message}", LogLevel.Warn);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _monitor.Log($"JSON error while saving data for key '{key}' to '{path}': {ex.Message}", LogLevel.Error);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Exception ex)
            {
                _monitor.Log($"Unexpected error while saving data for key '{key}' to '{path}': {ex.Message}", LogLevel.Error);
                throw; // Keep rethrowing for save operations as these are critical
            }
        }
        
        /// <summary>
        /// Load mod data from persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to load (must be a reference type)</typeparam>
        /// <param name="key">Key to identify the data</param>
        /// <returns>Loaded data or default value if not found</returns>
        public T? LoadData<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            var path = GetFilePath(key);
            try
            {
                return _helper.Data.ReadJsonFile<T>(path);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Log FileNotFoundException as Trace to reduce log noise
                _monitor.Log($"File not found while loading data for key '{key}': {ex.Message}", LogLevel.Trace);
                return null; // Return null instead of throwing when file doesn't exist
            }
            catch (System.IO.IOException ex)
            {
                // Log other IOExceptions as Warn to reduce log noise from non-critical issues
                _monitor.Log($"IOException while loading data for key '{key}': {ex.Message}", LogLevel.Warn);
                throw;
            }
            catch (Newtonsoft.Json.JsonException ex) // Catch broader JsonException instead of JsonReaderException
            {
                // Log JsonException instead of silently swallowing it
                _monitor.Log($"JSON parsing error while loading data for key '{key}': {ex.Message}", LogLevel.Error);
                throw;
            }
        }
        
        /// <summary>
        /// Check if data exists for a given key
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if data exists, false otherwise</returns>
        public bool DataExists(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            string relativePath = GetFilePath(key);
            
            try
            {
                var data = _helper.Data.ReadJsonFile<object>(relativePath);
                return data != null;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _monitor.Log($"File not found while checking data existence for key '{key}': {ex.Message}", LogLevel.Trace);
                return false; // Return false instead of throwing when file doesn't exist
            }
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while checking data existence for key '{key}': {ex.Message}", LogLevel.Warn);
                throw; // Re-throw IO errors to prevent potential data loss
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _monitor.Log($"JSON parsing error while checking data existence for key '{key}': {ex.Message}", LogLevel.Error);
                throw; // Re-throw JsonException as expected by tests
            }
        }
        
        /// <summary>
        /// Remove data for a given key
        /// </summary>
        /// <param name="key">Key to remove</param>
        public void RemoveData(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            // Delete the data file to properly remove the stored data
            var filePath = GetFilePath(key);
            try
            {
                // Always use SMAPI's API for consistency and cross-platform compatibility
                _helper.Data.WriteJsonFile<object>(filePath, null);
                _monitor.Log($"Removed data for key '{key}' by writing null.", LogLevel.Trace);
            }
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while removing data for key '{key}': {ex.Message}", LogLevel.Warn);
                throw;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Unexpected error while removing data for key '{key}': {ex.Message}", LogLevel.Error);
                throw;
            }
        }
        
        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            '<', '>', ':', '"', '|', '?', '*'
        };

        private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        /// <summary>
        /// Sanitizes the key by normalizing Unicode and replacing invalid characters
        /// </summary>
        /// <param name="key">The input key to sanitize</param>
        /// <returns>The sanitized key with invalid characters replaced</returns>
        private static string SanitizeKey(string key)
        {
            // First, normalize the Unicode to handle homoglyphs and normalize forms
            key = key.Normalize(NormalizationForm.FormC);

            // Process the string character by character to handle all special cases
            var sanitizedKeyBuilder = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                // Handle Unicode homoglyphs by converting common Cyrillic lookalikes to Latin
                char processedChar = c switch
                {
                    // Cyrillic characters that look like Latin ones
                    '\u0435' => 'e', // Cyrillic 'е' to Latin 'e'
                    '\u0430' => 'a', // Cyrillic 'а' to Latin 'a'
                    '\u043e' => 'o', // Cyrillic 'о' to Latin 'o'
                    '\u0440' => 'p', // Cyrillic 'р' to Latin 'p'
                    '\u0441' => 'c', // Cyrillic 'с' to Latin 'c'
                    '\u0445' => 'x', // Cyrillic 'х' to Latin 'x'
                    _ => c
                };

                if (InvalidFileNameChars.Contains(processedChar))
                    sanitizedKeyBuilder.Append('_');
                else
                    sanitizedKeyBuilder.Append(processedChar);
            }

            var sanitized = sanitizedKeyBuilder.ToString();

            // After handling homoglyphs, process diacritics by decomposing and removing combining marks
            // First normalize to FormD to decompose diacritics
            sanitized = sanitized.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            
            for (int i = 0; i < sanitized.Length; i++)
            {
                char c = sanitized[i];
                
                // Check if this is a combining diacritical mark (accent)
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    // This is a diacritic mark, add an underscore if the last character wasn't already an underscore
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append('_');
                    }
                }
                else
                {
                    // This is a base character, append it
                    sb.Append(c);
                }
            }
            
            sanitized = sb.ToString();

            // Replace any remaining directory separators that might have been missed
            sanitized = sanitized.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            
            // Handle consecutive dots: replace sequences of 2 or more dots with "_."
            sanitized = Regex.Replace(sanitized, @"\.{2,}", "_.");
            
            // Replace any remaining single dots with underscores
            sanitized = sanitized.Replace('.', '_');

            // Collapse multiple underscores that might result from the above operations
            sanitized = Regex.Replace(sanitized, "_{2,}", "_");

            // Trim whitespace, dots, and underscores at ends
            sanitized = sanitized.Trim(' ', '.', '_');

            if (string.IsNullOrEmpty(sanitized))
                throw new ArgumentException("Key sanitizes to an empty string, which is not allowed.", nameof(key));

            return sanitized;
        }

        /// <summary>
        /// Validates that the key doesn't contain path traversal patterns
        /// </summary>
        /// <param name="key">The key to validate</param>
        /// <exception cref="ArgumentException">Thrown if path traversal is detected</exception>
        private static void ValidatePathTraversal(string key)
        {
            // Check for absolute paths and reject them to prevent path traversal vulnerabilities
            if (Path.IsPathRooted(key))
                throw new ArgumentException("Key cannot be an absolute path, which is not allowed.", nameof(key));
            
            // Check for path traversal patterns like ../ or ..\ or ... (for path traversal)
            if (key.Contains("../") || key.Contains("..\\") || Regex.IsMatch(key, @"\.{2,}[/\\]"))
                throw new ArgumentException("Key cannot contain path traversal patterns, which is not allowed.", nameof(key));
        }

        /// <summary>
        /// Handles reserved Windows filenames by appending an underscore to the base name
        /// </summary>
        /// <param name="sanitizedKey">The sanitized key to check for reserved names</param>
        /// <returns>A filename with reserved names handled appropriately</returns>
        private static string HandleReservedWindowsFilenames(string sanitizedKey)
        {
            // Guard against reserved device names on Windows
            // Reference: https://learn.microsoft.com/windows/win32/fileio/naming-a-file
            string baseName = Path.GetFileNameWithoutExtension(sanitizedKey);
            string sanitizedExtension = Path.GetExtension(sanitizedKey);
            string extension = "json";

            if (ReservedWindowsFileNames.Contains(baseName))
            {
                baseName += "_";
            }

            return Path.Combine("data", $"{baseName}{sanitizedExtension}.{extension}");
        }

        private static string GetFilePath(string key)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));

            // Validate path traversal first to prevent security issues
            ValidatePathTraversal(key);

            // Sanitize the key to handle invalid characters
            string sanitizedKey = SanitizeKey(key);

            // Handle reserved Windows filenames
            return HandleReservedWindowsFilenames(sanitizedKey);
        }
    }
}