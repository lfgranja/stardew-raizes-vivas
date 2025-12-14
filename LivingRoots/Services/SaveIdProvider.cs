using System;
using System.Globalization;
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
                var game1Type = GetGame1Type();
                if (game1Type != null)
                {
                    // Try to access the uniqueIDForThisGame field using more comprehensive binding flags
                    var uniqueIDField = game1Type.GetField("uniqueIDForThisGame", 
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (uniqueIDField != null)
                    {
                        var value = uniqueIDField.GetValue(null);
                        if (value != null)
                        {
                            // Perform explicit type checking and use invariant culture for conversion
                            string saveId = ConvertValueToString(value);
                            // Validate that the save ID is not null, empty, or whitespace before returning
                            if (IsValidSaveId(saveId))
                                return saveId;
                        }
                    }
                    else
                    {
                        // Log minimal information about reflection failure at Trace level for debugging
                        effectiveMonitor?.Log("GetSaveId: Field 'uniqueIDForThisGame' not found in Game1 type", LogLevel.Trace);
                    }
                    
                    // Alternative: try to get save folder name if it exists
                    var saveFolderNameField = game1Type.GetField("SaveFolderName", 
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (saveFolderNameField != null)
                    {
                        var value = saveFolderNameField.GetValue(null);
                        if (value != null)
                        {
                            // Perform explicit type checking and use invariant culture for conversion
                            string saveId = ConvertValueToString(value);
                            // Validate that the save ID is not null, empty, or whitespace before returning
                            if (IsValidSaveId(saveId))
                                return saveId;
                        }
                    }
                    else
                    {
                        // Log minimal information about reflection failure at Trace level for debugging
                        effectiveMonitor?.Log("GetSaveId: Field 'SaveFolderName' not found in Game1 type", LogLevel.Trace);
                    }
                } // Added missing closing brace
                else
                {
                    // Log minimal information about type lookup failure at Trace level for debugging
                    effectiveMonitor?.Log("GetSaveId: Type 'StardewValley.Game1' not found", LogLevel.Trace);
                }
                
                // If we're in a test environment or SMAPI context isn't available yet,
                // return null which will be handled by the calling code
                return null;
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                // Log minimal reflection errors for debugging at Trace level
                // Using a static message to avoid exposing sensitive system information
                effectiveMonitor?.Log("GetSaveId: ReflectionTypeLoadException occurred during type loading", LogLevel.Trace);
                // Optionally log the count of loader exceptions for debugging purposes without exposing details
                effectiveMonitor?.Log($"GetSaveId: Number of loader exceptions: {ex.LoaderExceptions?.Length ?? 0}", LogLevel.Trace);
                return null;
            }
            catch (Exception ex)
            {
                // Log minimal information about any other reflection errors at Trace level for debugging
                effectiveMonitor?.Log($"GetSaveId: Exception occurred during reflection: {ex.GetType().Name}", LogLevel.Trace);
                return null;
            }
        }
        
        /// <summary>
        /// Converts a value to string with explicit type checking and invariant culture formatting
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>String representation of the value</returns>
        private static string ConvertValueToString(object value)
        {
            // Explicit type checking and conversion using invariant culture
            return value switch
            {
                string stringValue => stringValue,
                int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                long longValue => longValue.ToString(CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
                decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
                bool boolValue => boolValue.ToString(CultureInfo.InvariantCulture),
                DateTime dateTimeValue => dateTimeValue.ToString(CultureInfo.InvariantCulture),
                Guid guidValue => guidValue.ToString(),
                IFormattable formattableValue => formattableValue.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }
        
        /// <summary>
        /// Validates that the save ID is not null, empty, or whitespace
        /// </summary>
        /// <param name="saveId">The save ID to validate</param>
        /// <returns>True if the save ID is valid, false otherwise</returns>
        private static bool IsValidSaveId(string? saveId)
        {
            return !string.IsNullOrWhiteSpace(saveId);
        }
        
        /// <summary>
        /// Helper method to get the Game1 type with enhanced lookup mechanism
        /// </summary>
        private static Type? GetGame1Type()
        {
            try
            {
                // First try the original method
                var game1Type = Type.GetType("StardewValley.Game1, Stardew Valley");
                if (game1Type != null)
                    return game1Type;

                // If the original method fails, try loading from the current domain
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly?.FullName?.StartsWith("Stardew Valley") == true)
                    {
                        game1Type = assembly.GetType("StardewValley.Game1");
                        if (game1Type != null)
                            return game1Type;
                    }
                }

                // If not found in loaded assemblies, try to load the assembly by name
                try
                {
                    var stardewAssembly = Assembly.Load("Stardew Valley");
                    game1Type = stardewAssembly.GetType("StardewValley.Game1");
                    if (game1Type != null)
                        return game1Type;
                }
                catch (Exception)
                {
                    // If we can't load the assembly, continue to next approach
                }

                // As a last resort, look for the type in all loaded assemblies
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.FullName == "StardewValley.Game1")
                                return type;
                        }
                    }
                    catch (Exception)
                    {
                        // Skip assemblies that can't be loaded or accessed
                        continue;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}