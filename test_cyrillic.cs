using System;
using System.Text;

class Program
{
    static void Main()
    {
        string input = "тест";
        Console.WriteLine($"Input: {input}");
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            Console.WriteLine($"  {i}: '{c}' (U+{((int)c):X4})");
        }
        
        // Test the specific Cyrillic characters
        char[] cyrillicChars = {'а', 'е', 'с', 'т', 'А', 'Е', 'С', 'Т'};
        Console.WriteLine("\nCyrillic character codes:");
        foreach (char c in cyrillicChars)
        {
            Console.WriteLine($"  '{c}' (U+{((int)c):X4})");
        }
    }
}