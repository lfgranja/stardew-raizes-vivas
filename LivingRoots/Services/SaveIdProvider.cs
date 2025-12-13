using System;
using System.Reflection;
using StardewModdingAPI;
using LivingRoots.Domain;

namespace LivingRoots.Services
{
    /// <summary>
    /// Provides the save ID for data persistence operations by accessing Stardew Valley's Game1 class.
    /// This implementation uses reflection to access the game's save folder name.
    /// </summary>
    public class SaveIdProvider : ISaveIdProvider
    {
        private readonly IMonitor? _monitor;

        public SaveIdProvider(IMonitor? monitor = null)
        {
            _monitor = monitor;
        }

        public string? GetSaveId(IMonitor? monitor = null)
        {
            // Use the passed monitor if provided, otherwise use the one from constructor
            var effectiveMonitor = monitor ?? _monitor;

            try
            {
                // Try to get the save folder name from SMAPI context
                // In SMAPI, this is available through the game state
                var game1Type = Type.GetType("StardewValley.Game1, Stardew Valley");
                if (game1Type != null)
                {
                    var uniqueIDField = game1Type.GetField("uniqueIDForThisGame", 
                        BindingFlags.Static | BindingFlags.Public);
                    if (uniqueIDField != null)
                    {
                        var value = uniqueIDField.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }
                    else
                    {
                        // Log detailed information about reflection failure at Trace level for debugging
                        var fieldNames = GetFieldNames(game1Type);
                        effectiveMonitor?.Log($"GetSaveId: Field 'uniqueIDForThisGame' not found in Game1 type. Available fields: {string.Join(", ", fieldNames)}", LogLevel.Trace);
                    }
                    
                    // Alternative: try to get save folder name if it exists
                    var saveFolderNameField = game1Type.GetField("SaveFolderName", 
                        BindingFlags.Static | BindingFlags.Public);
                    if (saveFolderNameField != null)
                    {
                        var value = saveFolderNameField.GetValue(null);
                        if (value != null)
                            return value.ToString();
                    }
                    else
                    {
                        // Log detailed information about reflection failure at Trace level for debugging
                        var fieldNames = GetFieldNames(game1Type);
                        effectiveMonitor?.Log($"GetSaveId: Field 'SaveFolderName' not found in Game1 type. Available fields: {string.Join(", ", fieldNames)}", LogLevel.Trace);
                    }
                }
                else
                {
                    // Log detailed information about reflection failure at Trace level for debugging
                    var assemblyNames = GetAssemblyNames();
                    effectiveMonitor?.Log($"GetSaveId: Type 'StardewValley.Game1, Stardew Valley' not found. Available assemblies: {string.Join(", ", assemblyNames)}", LogLevel.Trace);
                }
                
                // If we're in a test environment or SMAPI context isn't available yet,
                // return null which will be handled by the calling code
                return null;
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                // Log detailed reflection errors for debugging at Trace level
                // Check for null LoaderExceptions to avoid null reference exception
                string loaderExceptionMessages = "No loader exceptions";
                if (ex.LoaderExceptions != null)
                {
                    var loaderExceptionMessagesArray = new string[ex.LoaderExceptions.Length];
                    for (int i = 0; i < ex.LoaderExceptions.Length; i++)
                    {
                        // Also check each individual exception for null to avoid null reference
                        loaderExceptionMessagesArray[i] = ex.LoaderExceptions[i]?.Message ?? 
                                                        ex.LoaderExceptions[i]?.ToString() ?? 
                                                        "Unknown loader exception";
                    }
                    loaderExceptionMessages = string.Join("; ", loaderExceptionMessagesArray);
                }
                
                effectiveMonitor?.Log($"GetSaveId: ReflectionTypeLoadException occurred: {ex.Message}. Loader exceptions: {loaderExceptionMessages}", LogLevel.Trace);
                return null;
            }
            catch (Exception ex)
            {
                // Log detailed information about any other reflection errors at Trace level for debugging
                effectiveMonitor?.Log($"GetSaveId: Exception occurred during reflection: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                return null;
            }
        }
        
        /// <summary>
        /// Helper method to get field names from a type for debugging purposes
        /// </summary>
        private static string[] GetFieldNames(Type type)
        {
            try
            {
                var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
                var fieldNames = new string[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    fieldNames[i] = fields[i].Name;
                }
                return fieldNames;
            }
            catch
            {
                return new string[] { "Error retrieving field names" };
            }
        }
        
        /// <summary>
        /// Helper method to get assembly names for debugging purposes
        /// </summary>
        private static string[] GetAssemblyNames()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assemblyNames = new string[assemblies.Length];
                for (int i = 0; i < assemblies.Length; i++)
                {
                    assemblyNames[i] = assemblies[i].FullName;
                }
                return assemblyNames;
            }
            catch
            {
                return new string[] { "Error retrieving assembly names" };
            }
        }
    }
}