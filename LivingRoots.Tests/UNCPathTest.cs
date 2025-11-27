using System;
using System.IO;

namespace LivingRoots.Tests
{
    public class UNCPathTest
    {
        public static void Main()
        {
            TestPathMethods();
        }
        
        public static void TestPathMethods()
        {
            string uncInput = @"\\server\share\PRN.log";
            
            string directoryPath = Path.GetDirectoryName(uncInput);
            string fileName = Path.GetFileName(uncInput);
            
            Console.WriteLine($"Input: {uncInput}");
            Console.WriteLine($"Directory: {directoryPath}");
            Console.WriteLine($"FileName: {fileName}");
            
            // This will help us understand how Path methods work with UNC paths
            string combined = Path.Combine(directoryPath, "PRN_.log");
            Console.WriteLine($"Combined: {combined}");
            
            // Test with forward slashes too
            string forwardSlashInput = "//server/share/PRN.log";
            string dir2 = Path.GetDirectoryName(forwardSlashInput);
            string file2 = Path.GetFileName(forwardSlashInput);
            Console.WriteLine($"Forward slash input: {forwardSlashInput}");
            Console.WriteLine($"Directory: {dir2}");
            Console.WriteLine($"FileName: {file2}");
        }
    }
}