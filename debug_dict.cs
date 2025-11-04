using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var dict = new Dictionary<char, string>();
        dict.Add('\'', "'");
        dict.Add('"', "\"");
        
        Console.WriteLine("Dictionary created successfully");
        Console.WriteLine($"Keys in dictionary: {string.Join(", ", dict.Keys)}");
    }
}