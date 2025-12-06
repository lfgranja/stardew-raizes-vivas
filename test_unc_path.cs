using System;
using System.IO;

class Program
{
    static void Main()
    {
        string uncPath = @"\\server\share\PRN.log";
        
        Console.WriteLine($"Original path: {uncPath}");
        Console.WriteLine($"Path.GetDirectoryName: {Path.GetDirectoryName(uncPath)}");
        Console.WriteLine($"Path.GetFileName: {Path.GetFileName(uncPath)}");
        Console.WriteLine($"Path.Combine result: {Path.Combine(Path.GetDirectoryName(uncPath), "PRN_.log")}");
    }
}