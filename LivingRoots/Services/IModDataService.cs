using StardewModdingAPI;

namespace LivingRoots.Services
{
    /// <summary>
    /// Interface for handling mod data persistence and SMAPI interactions
    /// Following the Dependency Inversion Principle (DIP) from SOLID
    /// </summary>
    public interface IModDataService
    {
        /// <summary>
        /// Save mod data to persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to save (must be a reference type)</typeparam>
        /// <param name="data">Data to save</param>
        /// <param name="key">Key to identify the data</param>
        void SaveData<T>(T data, string key) where T : class;
        
        /// <summary>
        /// Load mod data from persistent storage
        /// </summary>
        /// <typeparam name="T">Type of data to load (must be a reference type)</typeparam>
        /// <param name="key">Key to identify the data</param>
        /// <returns>Loaded data or default value if not found</returns>
        T? LoadData<T>(string key) where T : class;
        
        /// <summary>
        /// Check if data exists for a given key
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if data exists, false otherwise</returns>
        bool DataExists(string key);
        
        /// <summary>
        /// Remove data for a given key
        /// </summary>
        /// <param name="key">Key to remove</param>
        void RemoveData(string key);
    }
}