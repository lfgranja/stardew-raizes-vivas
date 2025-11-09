using StardewModdingAPI;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Service for handling mod data persistence and SMAPI interactions
    /// Following the architecture pattern described in ARCHITECTURE.md
    /// Now following the Dependency Inversion Principle by depending on domain abstractions
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
        /// <param name="key">Key to identify the data</param>
        public void SaveData<T>(T data, string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            
            // Validate the raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize the key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = _modLogic.SanitizeFileName(key)!;
            var path = GetFilePath(sanitizedKey);
            try
            {
                _helper.Data.WriteJsonFile(path, data);
                // Use sanitized key for logging to avoid exposing raw input
                _monitor.Log($"Saved data for key '{sanitizedKey}'.", LogLevel.Trace);
            }
            
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while saving data to '{path}': {ex.Message}", LogLevel.Warn);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _monitor.Log($"JSON error while saving data to '{path}': {ex.Message}", LogLevel.Error);
                throw; // Keep rethrowing for save operations as these are critical
            }
            catch (Exception ex)
            {
                _monitor.Log($"Unexpected error while saving data to '{path}': {ex.Message}", LogLevel.Error);
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
            
            // Validate the raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize the key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = _modLogic.SanitizeFileName(key)!;
            var path = GetFilePath(sanitizedKey);
            try
            {
                var result = _helper.Data.ReadJsonFile<T>(path);
                if (result == null)
                {
                    // Log when ReadJsonFile returns null (which can happen when file exists but is empty/corrupted)
                    _monitor.Log($"Data is null while loading for key '{sanitizedKey}'.", LogLevel.Trace);
                    return null;
                }
                return result;
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
                // Log JsonException instead of silently swallowing it
                _monitor.Log($"JSON parsing error while loading data for key '{sanitizedKey}': {ex.Message}", LogLevel.Error);
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
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            // Validate the raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize the key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = _modLogic.SanitizeFileName(key)!;
            string relativePath = GetFilePath(sanitizedKey);
            
            try
            {
                var data = _helper.Data.ReadJsonFile<object>(relativePath);
                return data != null;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _monitor.Log($"File not found while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Trace);
                return false; // Return false instead of throwing when file doesn't exist
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                _monitor.Log($"Directory not found while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false; // Return false instead of throwing when directory doesn't exist
            }
            catch (System.UnauthorizedAccessException ex)
            {
                _monitor.Log($"Access denied while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false; // Return null instead of throwing when access is denied
            }
            catch (System.IO.IOException ex)
            {
                _monitor.Log($"IOException while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false; // Return false instead of throwing when IO error occurs
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _monitor.Log($"JSON parsing error while checking data existence for key '{sanitizedKey}': {ex.Message}", LogLevel.Warn);
                return false; // Return false instead of throwing when JSON is invalid
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
            
            // Validate the raw key first to catch path traversal attempts
            _modLogic.ValidatePath(key);
            
            // Sanitize the key once before try-catch block to prevent exceptions during error handling
            string sanitizedKey = _modLogic.SanitizeFileName(key)!;
            // Delete the data file to properly remove the stored data
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
        
        private string GetFilePath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            // The key should already be sanitized at this point, so we just return the path
            // The validation already happened when SanitizeFileName was called in the public methods

            // Sanitized key should not be null by this point due to check in public methods
            if (key == null)
                throw new InvalidOperationException("Sanitized key cannot be null");

            // Return the final path with the .json extension
            return Path.Combine("data", $"{key}.json");
        }
    }
}