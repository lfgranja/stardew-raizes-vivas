using StardewModdingAPI;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Service for handling mod data persistence and SMAPI interactions
    /// Following architecture pattern described in ARCHITECTURE.md
    /// Now following Dependency Inversion Principle by depending on domain abstractions
    /// </summary>
    public class ModDataService : IModDataService
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IModLogic _modLogic;
        
        
        public ModDataService(IModHelper helper, IMonitor monitor, IModLogic modLogic)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _modLogic = modLogic ?? throw new ArgumentNullException(nameof(modLogic));
        }
        
        /// <summary>
        /// Save mod data to persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to save (must be a reference type)</typeparam>
        /// <param name="data">Data to save</param>
        /// <param name="key">Key to identify data</param>
        public void SaveData<T>(T data, string key) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            
            string sanitizedKey = GetValidatedAndSanitizedKey(key);
            
            var path = GetFilePath(sanitizedKey);
            try
            {
                _helper.Data.WriteJsonFile(path, data);
                // Use sanitized key for logging to avoid exposing raw input
                _monitor.Log($"Saved data for key '{sanitizedKey}'.", LogLevel.Trace);
            }
            
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while saving data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _monitor.Log($"JSON error while saving data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Exception ex)
            {
                _monitor.Log($"Unexpected error while saving data for key '{sanitizedKey}': {ex.Message}", LogLevel.Error);
                throw; // Keep rethrowing for save operations as these are critical
            }
        }
        
        /// <summary>
        /// Load mod data from persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to load (must be a reference type)</typeparam>
        /// <param name="key">Key to identify data</param>
        /// <returns>Loaded data or default value if not found</returns>
        public T? LoadData<T>(string key) where T : class
        {
            // Defensive null checks for helper and its Data property
            if (_helper == null)
            {
                _monitor.Log("ModHelper is null in LoadData method. This should not happen under normal circumstances.", LogLevel.Error);
                return null;
            }
            
            if (_helper.Data == null)
            {
                _monitor.Log("Helper.Data is null in LoadData method. This should not happen under normal circumstances.", LogLevel.Error);
                return null;
            }
            
            string sanitizedKey;
            try
            {
                sanitizedKey = GetValidatedAndSanitizedKey(key);
            }
            catch (ArgumentException ex)
            {
                _monitor.Log($"Invalid key provided to LoadData: {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when key is invalid
            }
            
            try
            {
            var path = GetFilePath(sanitizedKey);
            
            // Directly attempt to read the file without checking existence first to avoid TOCTOU race condition
            var result = _helper.Data.ReadJsonFile<T>(path);
            
            if (result == null)
            {
                // File does not exist or contains no valid data
                _monitor.Log($"File not found or contains no valid data while loading for key '{sanitizedKey}': {path}", LogLevel.Warn);
                return null;
            }
            
            return result;
            }
            catch (ArgumentException ex)
            {
                _monitor.Log($"Invalid key provided to LoadData: {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when key is invalid
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Log FileNotFoundException as Trace to reduce log noise
                _monitor.Log($"File not found while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Trace);
                return null; // Return null instead of throwing when file doesn't exist
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                // Log DirectoryNotFoundException as Warn to reduce log noise from non-critical issues
                _monitor.Log($"Directory not found while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when directory doesn't exist
            }
            catch (System.UnauthorizedAccessException ex)
            {
                // Log UnauthorizedAccessException as Warn to reduce log noise from non-critical issues
                _monitor.Log($"Access denied while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when access is denied
            }
            catch (System.IO.IOException ex)
            {
                // Log other IOExceptions as Warn to reduce log noise from non-critical issues
                _monitor.Log($"IOException while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when IO error occurs
            }
            catch (Newtonsoft.Json.JsonException ex) // Catch broader JsonException instead of JsonReaderException
            {
                // Log JsonException as Warn for consistency with other non-critical errors
                _monitor.Log($"JSON parsing error while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return null; // Return null instead of throwing when JSON is invalid (consistent with DataExists behavior)
            }
        }
        
        /// <summary>
        /// Check if data exists for a given key
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if data exists, false otherwise</returns>
        public bool DataExists(string key)
        {
            // Defensive null checks for helper and its Data property
            if (_helper == null)
            {
                _monitor.Log("ModHelper is null in DataExists method. This should not happen under normal circumstances.", LogLevel.Error);
                return false;
            }
            
            if (_helper.Data == null)
            {
                _monitor.Log("Helper.Data is null in DataExists method. This should not happen under normal circumstances.", LogLevel.Error);
                return false;
            }

            string sanitizedKey;
            try
            {
                sanitizedKey = GetValidatedAndSanitizedKey(key);
            }
            catch (ArgumentException ex)
            {
                _monitor.Log($"Invalid key provided to DataExists: {ex.Message}", LogLevel.Warn);
                return false;
            }

            try
            {
                string path = GetFilePath(sanitizedKey);
                
                // Use SMAPI's ReadJsonFile API to check for data existence to maintain consistency
                // with LoadData and avoid TOCTOU race conditions
                var result = _helper.Data.ReadJsonFile<object>(path);
                
                // If result is not null, the file exists and contains valid data
                // If result is null, the file doesn't exist or contains no valid data
                return result != null;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // File does not exist - log as trace to reduce noise
                _monitor.Log($"File not found while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Trace);
                return false;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                // Directory does not exist - log as warn
                _monitor.Log($"Directory not found while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                // Access denied to file or directory - treat as non-existent and log as warn
                _monitor.Log($"Access denied while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false;
            }
            catch (System.IO.IOException ex)
            {
                // Other IO exceptions - treat as non-existent and log as warn
                _monitor.Log($"IOException while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false;
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                // JSON parsing error - file exists but contains invalid JSON
                // For DataExists purpose, we consider this as "data exists" since the file is there
                _monitor.Log($"JSON parsing error while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return true;
            }
            catch (Exception ex)
            {
                // Any other unexpected exception - treat as non-existent and log as error
                _monitor.Log($"Unexpected error while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Remove data for a given key
        /// </summary>
        /// <param name="key">Key to remove</param>
        public void RemoveData(string key)
        {
            string sanitizedKey = GetValidatedAndSanitizedKey(key);
            
            // Delete data file to properly remove stored data
            var filePath = GetFilePath(sanitizedKey);
            try
            {
                // Always use SMAPI's API for consistency and cross-platform compatibility
                _helper.Data.WriteJsonFile<object>(filePath, null);
                _monitor.Log($"Removed data for key '{sanitizedKey}' by writing null.", LogLevel.Trace);
            }
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while removing data for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                throw;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Unexpected error while removing data for key '{sanitizedKey}': {ex.Message}", LogLevel.Error);
                throw;
            }
        }
        
        /// <summary>
        /// Validates and sanitizes key in a single method to reduce code duplication
        /// </summary>
        /// <param name="key">The key to validate and sanitize</param>
        /// <returns>The sanitized key</returns>
        private string GetValidatedAndSanitizedKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            // Validate raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = SanitizePathSegments(key);
            if (string.IsNullOrWhiteSpace(sanitizedKey))
                throw new ArgumentException($"Failed to sanitize key '{key}'. Sanitized key cannot be null or whitespace.", nameof(key));
            
            return sanitizedKey;
        }
        
        /// <summary>
        /// Validates and sanitizes key for RemoveData method, which has different error handling
        /// </summary>
        /// <param name="key">The key to validate and sanitize</param>
        /// <returns>The sanitized key, or null if validation fails and method should return early</returns>
        private string? GetValidatedAndSanitizedKeyForRemove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            // Validate raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = SanitizePathSegments(key);
            if (string.IsNullOrWhiteSpace(sanitizedKey))
            {
                _monitor.Log($"Failed to sanitize key '{key}'. Cannot remove data with null or whitespace sanitized key.", LogLevel.Warn);
                return null; // Return null to signal early return instead of exception
            }
            
            return sanitizedKey;
        }
        
        private string GetFilePath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            // The key should already be sanitized at this point, so we just return path
            // The validation already happened when SanitizeFileName was called in public methods

            // Sanitized key should not be null by this point due to check in public methods
            if (key == null)
                throw new ArgumentException("Sanitized key cannot be null", nameof(key));

            // Return final path with .json extension
            return Path.Combine("data", $"{key}.json");
        }
        
        /// <summary>
        /// Sanitizes each segment of a path separately to preserve directory structure
        /// </summary>
        /// <param name="path">The path to sanitize</param>
        /// <returns>The sanitized path with each segment properly sanitized</returns>
        private string SanitizePathSegments(string path)
        {
            if (path == null)
                throw new ArgumentException("Path cannot be null.", nameof(path));

            if (path.Length == 0)
                throw new ArgumentException("Path cannot be empty.", nameof(path));
            
            // Split path by both forward and backward slashes
            string[] segments = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Sanitize each segment individually
            var validSegments = new List<string>();
            for (int i = 0; i < segments.Length; i++)
            {
                // Handle special path segments that need special handling
                if (segments[i] == ".")
                {
                    // Skip . segments to prevent unnecessary directory references
                    continue;
                }
                else if (segments[i] == "..")
                {
                    // Throw an exception for .. segments to prevent path traversal
                    throw new ArgumentException($"Failed to sanitize path segment '{segments[i]}'. Path traversal segments are not allowed.", nameof(path));
                }
                
                // Use existing SanitizeFileName method for other segments
                string? sanitizedSegment = _modLogic.SanitizeFileName(segments[i]);
                
                // If any segment becomes null or whitespace after sanitization, collect it for final validation
                // Don't throw exception here - let final validation handle it
                if (!string.IsNullOrWhiteSpace(sanitizedSegment))
                {
                    validSegments.Add(sanitizedSegment);
                }
            }
            
            // Check if we have any valid segments after processing
            if (validSegments.Count == 0)
                throw new ArgumentException("Sanitized path cannot be empty.", nameof(path));
            
            // Join sanitized segments back together using system's directory separator
            // Use IEnumerable<string> directly instead of converting to array
            return string.Join(Path.DirectorySeparatorChar.ToString(), validSegments);
        }
    }
}