using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        // Get the invalid file name characters from the system
        char[] systemInvalidChars = Path.GetInvalidFileNameChars();
        
        Console.WriteLine("System invalid file name characters:");
        foreach (char c in systemInvalidChars.OrderBy(x => x))
        {
            Console.WriteLine($"  '{c}' (U+{((int)c).ToString("X4")})");
        }
        
        // Define the characters that are being manually added in ModDataService.cs
        var manualAdditions = new HashSet<char>
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar,
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };
        
        Console.WriteLine("\nCharacters manually added in ModDataService.cs:");
        foreach (char c in manualAdditions.OrderBy(x => x))
        {
            Console.WriteLine($"  '{c}' (U+{((int)c).ToString("X4")})");
        }
        
        // Check which manual additions are already covered by system
        var alreadyCovered = manualAdditions.Where(c => systemInvalidChars.Contains(c)).ToList();
        var notCovered = manualAdditions.Except(systemInvalidChars).ToList();
        
        Console.WriteLine("\nCharacters that are ALREADY covered by Path.GetInvalidFileNameChars():");
        foreach (char c in alreadyCovered.OrderBy(x => x))
        {
            Console.WriteLine($"  '{c}' (U+{((int)c).ToString("X4")})");
        }
        
        Console.WriteLine("\nCharacters that are NOT covered by Path.GetInvalidFileNameChars() (these should be kept):");
        foreach (char c in notCovered.OrderBy(x => x))
        {
            Console.WriteLine($"  '{c}' (U+{((int)c).ToString("X4")})");
        }
        
        Console.WriteLine($"\nSummary:");
        Console.WriteLine($"  - Total system invalid chars: {systemInvalidChars.Length}");
        Console.WriteLine($"  - Manual additions: {manualAdditions.Count}");
        Console.WriteLine($"  - Already covered by system: {alreadyCovered.Count}");
        Console.WriteLine($"  - Additional chars needed: {notCovered.Count}");
        
        // Test creating a file with each character to see if it throws an exception
        Console.WriteLine("\nTesting character validation by attempting file creation:");
        string tempDir = Path.GetTempPath();
        string baseFileName = "test";
        string fileExtension = ".txt";
        
        foreach (char c in manualAdditions)
        {
            string invalidFileName = baseFileName + c + fileExtension;
            string fullPath = Path.Combine(tempDir, invalidFileName);
            
            try
            {
                using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                {
                    // If we reach here, the character was allowed in the filename
                    Console.WriteLine($"  WARNING: Character '{c}' was allowed in filename '{invalidFileName}' - may not be invalid on this system");
                }
                
                // Clean up the test file
                File.Delete(fullPath);
            }
            catch (ArgumentException)
            {
                // This is expected for invalid characters
                Console.WriteLine($"  Character '{c}' correctly rejected as invalid filename character");
            }
            catch (UnauthorizedAccessException)
            {
                // May happen if file already exists or we don't have permission
                Console.WriteLine($"  Character '{c}' test failed due to access permission");
            }
            catch (IOException)
            {
                // May happen if file already exists
                Console.WriteLine($"  Character '{c}' test failed due to IO exception");
            }
        }
    }
}
