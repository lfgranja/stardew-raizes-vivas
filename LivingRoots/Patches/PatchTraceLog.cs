using StardewModdingAPI;

namespace LivingRoots.Patches
{
    /// <summary>
    /// Shared tracing helper for Harmony patch classes.
    /// </summary>
    internal static class PatchTraceLog
    {
        public static void Log(IMonitor? monitor, string message, Exception? ex)
        {
            try
            {
                if (monitor != null)
                {
                    if (ex != null)
                    {
                        monitor.Log($"{message}: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    }
                    else
                    {
                        monitor.Log(message, LogLevel.Trace);
                    }
                }
            }
            catch
            {
                // never throw from logging
            }
        }
    }
}
