using System;
using System.Text;

class Program
{
    static void Main()
    {
        // Test the problematic characters
        string mixedUnicode = "café тест naïve";
        string fileNameUnicode = "file_with_úñíçødé_chars";
        
        Console.WriteLine("Mixed Unicode Input: " + mixedUnicode);
        for (int i = 0; i < mixedUnicode.Length; i++)
        {
            char c = mixedUnicode[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4} - Category: {char.GetUnicodeCategory(c)}");
        }
        
        Console.WriteLine("\nFile Name Unicode Input: " + fileNameUnicode);
        for (int i = 0; i < fileNameUnicode.Length; i++)
        {
            char c = fileNameUnicode[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4} - Category: {char.GetUnicodeCategory(c)}");
        }
        
        // Test normalization
        Console.WriteLine("\nAfter FormD normalization:");
        string mixedNormalized = mixedUnicode.Normalize(System.Text.NormalizationForm.FormD);
        Console.WriteLine("Mixed normalized: " + mixedNormalized);
        for (int i = 0; i < mixedNormalized.Length; i++)
        {
            char c = mixedNormalized[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4} - Category: {char.GetUnicodeCategory(c)}");
        }
        
        string fileNameNormalized = fileNameUnicode.Normalize(System.Text.NormalizationForm.FormD);
        Console.WriteLine("\nFile name normalized: " + fileNameNormalized);
        for (int i = 0; i < fileNameNormalized.Length; i++)
        {
            char c = fileNameNormalized[i];
            Console.WriteLine($"  [{i}] '{c}' U+{(int)c:X4} - Category: {char.GetUnicodeCategory(c)}");
        }
    }
}