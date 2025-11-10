using System;
using LivingRoots.Domain;

class Program
{
    static void Main(string[] args)
    {
        BoundaryTest.RunTest();
        BoundaryTest.CompareLogic();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}