using System;
using LivingRoots.Domain;

namespace TempTest
{
    class Program
    {
        static void Main()
        {
            var service = new PathValidationService();
            
            Console.WriteLine("Testing ../file.txt:");
            try 
            { 
                service.Validate("../file.txt"); 
                Console.WriteLine("No exception thrown for ../file.txt"); 
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"Exception for ../file.txt: {ex.Message}"); 
            }
            
            Console.WriteLine("\nTesting folder/./../file.txt:");
            try 
            { 
                service.Validate("folder/./../file.txt"); 
                Console.WriteLine("No exception thrown for folder/./../file.txt"); 
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"Exception for folder/./../file.txt: {ex.Message}"); 
            }
            
            Console.WriteLine("\nTesting folder/./file.txt:");
            try 
            { 
                service.Validate("folder/./file.txt"); 
                Console.WriteLine("No exception thrown for folder/./file.txt"); 
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"Exception for folder/./file.txt: {ex.Message}"); 
            }
        }
    }
}
