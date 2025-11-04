using System;
using System.Text;
using LivingRoots.Services;

class Program
{
    static void Main()
    {
        var normalizer = new UnicodeNormalizer();
        
        // Test the exact input from the failing test
        string input = "café тест naïve";
        string result = normalizer.Normalize(input);
        
        Console.WriteLine($"Input: {input}");
        Console.WriteLine($"Result: {result}");
        Console.WriteLine($"Expected: cafe тест naive");
        
        // Check each character
        Console.WriteLine("\nCharacter by character:");
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            Console.WriteLine($"Input[{i}]: '{c}' (U+{((int)c):X4})");
        }
        
        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];
            Console.WriteLine($"Result[{i}]: '{c}' (U+{((int)c):X4})");
        }
    }
}