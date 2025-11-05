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
        private readonly IPathTraversalValidator _pathTraversalValidator;
        private readonly IFileNameSanitizer _fileNameSanitizer;
        private readonly IReservedNameHandler _reservedNameHandler;
        
        
        
        public ModDataService(IModHelper helper, IMonitor monitor, IPathTraversalValidator pathTraversalValidator, IFileNameSanitizer fileNameSanitizer, IReservedNameHandler reservedNameHandler)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _pathTraversalValidator = pathTraversalValidator ?? throw new ArgumentNullException(nameof(pathTraversalValidator));
            _fileNameSanitizer = fileNameSanitizer ?? throw new ArgumentNullException(nameof(fileNameSanitizer));
            _reservedNameHandler = reservedNameHandler ?? throw new ArgumentNullException(nameof(reservedNameHandler));
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
                _monitor.Log($"JSON parsing error while checking data existence for key '{key}': {ex.Message}", LogLevel.Warn);
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
        
        private string GetFilePath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            // Validate path traversal first to prevent security issues
            _pathTraversalValidator.Validate(key);

            // Sanitize the key to handle invalid characters and security concerns
            string sanitizedKey = _fileNameSanitizer.Sanitize(key);

            // Handle reserved Windows filenames
            string handledKey = _reservedNameHandler.Handle(sanitizedKey);

            // Return the final path with the .json extension
            return Path.Combine("data", $"{handledKey}.json");
        }
    }
}