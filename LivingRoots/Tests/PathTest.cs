using System;
using System.IO;

class PathTest
{
    static void Main()
    {
        string uncPath = @"\\server\share\PRN.log";
        Console.WriteLine($"Original: {uncPath}");
        Console.WriteLine($"Path.GetDirectoryName: '{Path.GetDirectoryName(uncPath)}'");
        Console.WriteLine($"Path.GetFileName: '{Path.GetFileName(uncPath)}'");
        
        string normalPath = @"C:\path\to\CON.txt";
        Console.WriteLine($"Normal Path: {normalPath}");
        Console.WriteLine($"Path.GetDirectoryName: '{Path.GetDirectoryName(normalPath)}'");
        Console.WriteLine($"Path.GetFileName: '{Path.GetFileName(normalPath)}'");
    }
}