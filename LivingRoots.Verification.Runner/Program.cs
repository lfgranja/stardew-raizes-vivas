using System;

namespace LivingRoots.Verification.Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: LivingRoots.Verification.Runner <validate-scenario|evaluate|extract|build-manifest> ...");
                return 14;
            }

            string verb = args[0];
            switch (verb)
            {
                case "validate-scenario":
                case "evaluate":
                case "extract":
                case "build-manifest":
                    Console.Error.WriteLine($"Stub: {verb} not implemented yet.");
                    return 14;
                default:
                    Console.Error.WriteLine($"Unknown subcommand: {verb}");
                    return 14;
            }
        }
    }
}
