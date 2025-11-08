using System;
using LivingRoots.Domain;

class SimpleTest
{
    static void Main()
    {
        var service = new PathValidationService();
        
        // Test the problematic path
        Console.WriteLine("Testing 'folder/subfolder/../../file.txt':");
        try 
        { 
            service.Validate("folder/subfolder/../../file.txt"); 
            Console.WriteLine("No exception thrown - PATH IS ALLOWED (unexpected?)"); 
        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Exception thrown: {ex.Message} - PATH IS BLOCKED (expected)"); 
        }
        
        // Test other paths for comparison
        Console.WriteLine("\nTesting '../file.txt':");
        try 
        { 
            service.Validate("../file.txt"); 
            Console.WriteLine("No exception thrown - PATH IS ALLOWED (unexpected)"); 
        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Exception thrown: {ex.Message} - PATH IS BLOCKED (expected)"); 
        }
        
        Console.WriteLine("\nTesting 'folder/../file.txt':");
        try 
        { 
            service.Validate("folder/../file.txt"); 
            Console.WriteLine("No exception thrown - PATH IS ALLOWED (unexpected?)"); 
        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Exception thrown: {ex.Message} - PATH IS BLOCKED (expected?)"); 
        }
    }
}
