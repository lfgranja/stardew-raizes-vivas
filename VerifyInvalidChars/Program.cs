using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        // Get the invalid file name characters from the system
        var systemInvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        
        Console.WriteLine("System Path.GetInvalidFileNameChars():");
        Console.WriteLine(string.Join(", ", systemInvalidFileNameChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        // Get the invalid path characters from the system (these are different from file name chars)
        var systemInvalidPathChars = new HashSet<char>(Path.GetInvalidPathChars());
        
        Console.WriteLine("System Path.GetInvalidPathChars():");
        Console.WriteLine(string.Join(", ", systemInvalidPathChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        // Define the manually added characters from ModDataService.cs
        var manualChars = new[]
        {
            Path.DirectorySeparatorChar,      // Usually '/' on Unix, '\' on Windows
            Path.AltDirectorySeparatorChar,   // Usually '\' on Unix, '/' on Windows (opposite of above)
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };
        
        Console.WriteLine("Manually added characters in ModDataService.cs:");
        Console.WriteLine(string.Join(", ", manualChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        // Check which manual chars are already in the system file name set
        var alreadyInFileNameChars = manualChars.Where(c => systemInvalidFileNameChars.Contains(c)).ToArray();
        var notInFileNameChars = manualChars.Where(c => !systemInvalidFileNameChars.Contains(c)).ToArray();
        
        Console.WriteLine("Characters that are ALREADY included in Path.GetInvalidFileNameChars():");
        Console.WriteLine(string.Join(", ", alreadyInFileNameChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        Console.WriteLine("Characters that are NOT included in Path.GetInvalidFileNameChars():");
        Console.WriteLine(string.Join(", ", notInFileNameChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        // Check which manual chars are already in the system path set (this is important too)
        var alreadyInPathChars = manualChars.Where(c => systemInvalidPathChars.Contains(c)).ToArray();
        var notInPathChars = manualChars.Where(c => !systemInvalidPathChars.Contains(c)).ToArray();
        
        Console.WriteLine("Characters that are ALREADY included in Path.GetInvalidPathChars():");
        Console.WriteLine(string.Join(", ", alreadyInPathChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        Console.WriteLine("Characters that are NOT included in Path.GetInvalidPathChars():");
        Console.WriteLine(string.Join(", ", notInPathChars.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        // Combined check - in either file name or path chars
        var alreadyInEither = manualChars.Where(c => systemInvalidFileNameChars.Contains(c) || systemInvalidPathChars.Contains(c)).ToArray();
        var notInEither = manualChars.Where(c => !systemInvalidFileNameChars.Contains(c) && !systemInvalidPathChars.Contains(c)).ToArray();
        
        Console.WriteLine("Characters that are ALREADY included in either Path.GetInvalidFileNameChars() OR Path.GetInvalidPathChars():");
        Console.WriteLine(string.Join(", ", alreadyInEither.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        Console.WriteLine("Characters that are NOT included in either Path.GetInvalidFileNameChars() OR Path.GetInvalidPathChars():");
        Console.WriteLine(string.Join(", ", notInEither.Select(c => $"'{c}' ({(int)c})")));
        Console.WriteLine();
        
        if (notInEither.Length == 0)
        {
            Console.WriteLine("✓ ALL manually added characters are already included in Path.GetInvalidFileNameChars() or Path.GetInvalidPathChars()");
            Console.WriteLine("This means the initialization in ModDataService.cs is redundant and can be simplified.");
        }
        else
        {
            Console.WriteLine("✗ Some manually added characters are NOT included in Path.GetInvalidFileNameChars() or Path.GetInvalidPathChars()");
            Console.WriteLine("These would need to be kept in the initialization.");
        }
        
        Console.WriteLine($"\nRuntime Information:");
        Console.WriteLine($"OS: {Environment.OSVersion}");
        Console.WriteLine($"Platform: {(Environment.GetEnvironmentVariable("OSTYPE") != null ? Environment.GetEnvironmentVariable("OSTYPE") : Environment.OSVersion.Platform.ToString())}");
        Console.WriteLine($"DirectorySeparatorChar: '{Path.DirectorySeparatorChar}' ({(int)Path.DirectorySeparatorChar})");
        Console.WriteLine($"AltDirectorySeparatorChar: '{Path.AltDirectorySeparatorChar}' ({(int)Path.AltDirectorySeparatorChar})");
    }
}
