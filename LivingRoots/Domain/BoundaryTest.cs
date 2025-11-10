using System;

namespace LivingRoots.Domain
{
    public class BoundaryTest
    {
        public static void RunTest()
        {
            var service = new UnicodeNormalizationService();
            
            // Test boundary cases for Cyrillic characters
            Console.WriteLine("Testing Cyrillic boundary handling:");
            
            // Test 1: Cyrillic character at the beginning followed by Cyrillic
            string test1 = "еtest"; // Cyrillic 'е' at beginning followed by Latin
            string? result1 = service.Normalize(test1);
            Console.WriteLine($"Test 1 - Input: '{test1}', Output: '{result1}'");
            
            // Test 2: Cyrillic character at the end preceded by Cyrillic  
            string test2 = "стest"; // Assuming 'с' is Cyrillic, at beginning followed by Latin
            string? result2 = service.Normalize(test2);
            Console.WriteLine($"Test 2 - Input: '{test2}', Output: '{result2}'");
            
            // Test 3: Single Cyrillic character (both beginning and end)
            string test3 = "а"; // Single Cyrillic character
            string? result3 = service.Normalize(test3);
            Console.WriteLine($"Test 3 - Input: '{test3}', Output: '{result3}'");
            
            // Test 4: Cyrillic at beginning followed by Cyrillic (should preserve)
            string test4 = "ест"; // Cyrillic 'е' followed by Cyrillic 'с' and 'т'
            string? result4 = service.Normalize(test4);
            Console.WriteLine($"Test 4 - Input: '{test4}', Output: '{result4}'");
            
            // Test 5: Latin 'e' at beginning followed by Latin (should convert if it's a confusable in a different context)
            string test5 = "eLatin"; // Latin 'e' at beginning
            string? result5 = service.Normalize(test5);
            Console.WriteLine($"Test 5 - Input: '{test5}', Output: '{result5}'");
        }
        
        // This method simulates the old logic for comparison
        private static bool OldShouldConvertConfusable(char c, string text, int index)
        {
            bool prevIsCyrillic = index > 0 && IsCyrillicLetter(text[index - 1]);
            bool nextIsCyrillic = index < text.Length - 1 && IsCyrillicLetter(text[index + 1]);
            
            // If the character is surrounded by Cyrillic letters on BOTH sides, preserve it as part of legitimate Cyrillic text
            if (prevIsCyrillic && nextIsCyrillic)
            {
                return false; // Don't convert - it's part of legitimate Cyrillic text
            }
            
            // If only one neighbor is Cyrillic or if there are no Cyrillic neighbors on both sides,
            // convert the confusable character to prevent spoofing at script boundaries
            // This strengthens security by requiring BOTH sides to be Cyrillic to preserve the character
            return true;
        }
        
        // This method simulates the new logic
        private static bool NewShouldConvertConfusable(char c, string text, int index)
        {
            // Handle boundary cases: when at the beginning or end of the string
            bool prevIsCyrillic = index > 0 && IsCyrillicLetter(text[index - 1]);
            bool nextIsCyrillic = index < text.Length - 1 && IsCyrillicLetter(text[index + 1]);
            
            // If at the beginning of the string (no previous character), consider only the next neighbor
            if (index == 0)
            {
                // If the next character is Cyrillic, preserve this character (return false)
                if (nextIsCyrillic)
                    return false;
            }
            // If at the end of the string (no next character), consider only the previous neighbor
            else if (index == text.Length - 1)
            {
                // If the previous character is Cyrillic, preserve this character (return false)
                if (prevIsCyrillic)
                    return false;
            }
            // If not at boundaries, check both neighbors as before
            else
            {
                // If the character is surrounded by Cyrillic letters on BOTH sides, preserve it as part of legitimate Cyrillic text
                if (prevIsCyrillic && nextIsCyrillic)
                {
                    return false; // Don't convert - it's part of legitimate Cyrillic text
                }
            }
            
            // Convert the confusable character to prevent spoofing
            // This includes cases where:
            // - Character is at beginning/end and neighbor is not Cyrillic
            // - Character is in middle and not surrounded by Cyrillic on both sides
            return true;
        }
        
        private static bool IsCyrillicLetter(char c)
        {
            return (c >= '\u0400' && c <= '\u04FF') || // Cyrillic block
                   (c >= '\u0500' && c <= '\u052F') || // Cyrillic Supplement block
                   (c >= '\u2DE0' && c <= '\u2DFF') || // Cyrillic Extended-A block  
                   (c >= '\uA640' && c <= '\uA69F');   // Cyrillic Extended-B block
        }
        
        public static void CompareLogic()
        {
            Console.WriteLine("\nComparing old vs new logic:");
            
            // Example string: "ест" (Cyrillic e, s, t)
            string text = "ест";
            char c = text[0]; // First character (Cyrillic 'е')
            int index = 0; // At the beginning
            
            bool oldResult = OldShouldConvertConfusable(c, text, index);
            bool newResult = NewShouldConvertConfusable(c, text, index);
            
            Console.WriteLine($"Character '{c}' at index {index} in '{text}':");
            Console.WriteLine($"Old logic result: {oldResult} (would {(oldResult ? "convert" : "preserve")})");
            Console.WriteLine($"New logic result: {newResult} (would {(newResult ? "convert" : "preserve")})");
            
            // The old logic would check prevIsCyrillic=false (no previous char) and nextIsCyrillic=true (next is Cyrillic)
            // Since both are not true, it would return true (convert)
            // The new logic sees it's at index 0 (beginning), checks if next is Cyrillic (true), so returns false (preserve)
        }
    }
}