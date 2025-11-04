using System;
using System.Text;
using LivingRoots.Services;

class Program
{
    static void Main()
    {
        var normalizer = new UnicodeNormalizer();
        string input = "café тест naïve";
        string result = normalizer.Normalize(input);
        
        Console.WriteLine($"Input: {input}");
        Console.WriteLine($"Result: {result}");
        Console.WriteLine($"Expected: cafe тест naive");
        
        // Let's check each character
        Console.WriteLine("\nCharacter analysis:");
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            Console.WriteLine($"  {i}: '{c}' (U+{((int)c):X4})");
        }
        
        Console.WriteLine($"\nResult characters:");
        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];
            Console.WriteLine($"  {i}: '{c}' (U+{((int)c):X4})");
        }
    }
}