using System;
using System.Text;
using LivingRoots.Services;

class Program
{
    static void Main()
    {
        var normalizer = new UnicodeNormalizer();
        
        // Test the exact inputs from the failing tests
        string input1 = "café тест naïve";
        string result1 = normalizer.Normalize(input1);
        Console.WriteLine($"Input 1: {input1}");
        Console.WriteLine($"Output 1: {result1}");
        Console.WriteLine($"Expected 1: cafe тест naive");
        Console.WriteLine();
        
        string input2 = "file_with_úñíçødé_chars";
        string result2 = normalizer.Normalize(input2);
        Console.WriteLine($"Input 2: {input2}");
        Console.WriteLine($"Output 2: {result2}");
        Console.WriteLine($"Expected 2: file_with_unicode_chars");
        Console.WriteLine();
        
        // Debug character by character for input 1
        Console.WriteLine("Input 1 characters:");
        for (int i = 0; i < input1.Length; i++)
        {
            char c = input1[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4}");
        }
        Console.WriteLine("Output 1 characters:");
        for (int i = 0; i < result1.Length; i++)
        {
            char c = result1[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4}");
        }
        Console.WriteLine();
        
        // Debug character by character for input 2
        Console.WriteLine("Input 2 characters:");
        for (int i = 0; i < input2.Length; i++)
        {
            char c = input2[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4}");
        }
        Console.WriteLine("Output 2 characters:");
        for (int i = 0; i < result2.Length; i++)
        {
            char c = result2[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4}");
        }
    }
}