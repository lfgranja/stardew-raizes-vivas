using System;
using System.IO;
using System.Text.Json;
using LivingRoots.Verification.Runner.Evaluation;
using LivingRoots.Verification.Runner.Evidence;
using LivingRoots.Verification.Runner.Extraction;
using LivingRoots.Verification.Runner.Validation;

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
                    {
                        if (args.Length < 2)
                        {
                            Console.Error.WriteLine("missing scenario path");
                            return 14;
                        }
                        var v = new ScenarioValidator();
                        int code = v.Validate(args[1]);
                        Console.WriteLine(JsonSerializer.Serialize(new { valid = code == 0, code }));
                        return code;
                    }
                case "evaluate":
                    {
                        // evaluate <scenario-or-predicate> <actual-json> [pre-json]
                        if (args.Length < 3)
                        {
                            Console.Error.WriteLine("usage: evaluate <predicate.json> <actual.json>");
                            return 14;
                        }
                        try
                        {
                            var predText = File.ReadAllText(args[1]);
                            var actualText = File.ReadAllText(args[2]);
                            using var predDoc = JsonDocument.Parse(predText);
                            using var actualDoc = JsonDocument.Parse(actualText);
                            var reason = string.Empty;
                            bool ok = PredicateEvaluator.Evaluate(predDoc.RootElement, actualDoc.RootElement, out reason);
                            Console.WriteLine(JsonSerializer.Serialize(new { passed = ok, reason }));
                            return ok ? 0 : 19;
                        }
                        catch
                        {
                            Console.Error.WriteLine("evaluation failed");
                            return 19;
                        }
                    }
                case "extract":
                    {
                        if (args.Length < 3)
                        {
                            Console.Error.WriteLine("usage: extract <archive> <target>");
                            return 14;
                        }
                        int code = ArchiveExtractor.ExtractWithCode(args[1], args[2]);
                        Console.WriteLine(JsonSerializer.Serialize(new { extracted = code == 0, code }));
                        return code;
                    }
                case "build-manifest":
                    {
                        if (args.Length < 3)
                        {
                            Console.Error.WriteLine("usage: build-manifest <directory> <input.json>");
                            return 14;
                        }
                        string dir = args[1];
                        string inputPath = args[2];
                        object data;
                        try
                        {
                            var text = File.ReadAllText(inputPath);
                            data = JsonSerializer.Deserialize<object>(text) ?? new object();
                        }
                        catch
                        {
                            Console.Error.WriteLine("invalid input");
                            return 14;
                        }
                        try
                        {
                            string path = ManifestBuilder.BuildManifest(dir, data);
                            // write detached sha already done by BuildManifest
                            Console.WriteLine(JsonSerializer.Serialize(new { manifest = path }));
                            return 0;
                        }
                        catch
                        {
                            Console.Error.WriteLine("manifest build failed");
                            return 19;
                        }
                    }
                default:
                    Console.Error.WriteLine($"Unknown subcommand: {verb}");
                    return 14;
            }
        }
    }
}
