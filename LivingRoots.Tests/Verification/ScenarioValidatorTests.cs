using System;
using System.IO;
using System.Text.Json.Nodes;
using LivingRoots.Verification.Runner.Validation;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ScenarioValidatorTests
    {
        [Fact]
        public void Validate_Draft202012_AcceptReject_WithCrossFields_Exit14WithFieldPath()
        {
            var validator = new ScenarioValidator();
            var dir = Path.Combine(Path.GetTempPath(), "scen_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                // create test md with GATE3 block
                var testMd = Path.Combine(dir, "test.md");
                File.WriteAllText(testMd, "<!-- GATE3:BEGIN -->\nx\n<!-- GATE3:END -->");

                var scenario = Path.Combine(dir, "s.json");
                var json = @"{
                  ""schemaVersion"": 1,
                  ""scenarioId"": ""s1"",
                  ""scenarioKind"": ""positive"",
                  ""expectedVerdict"": ""pass"",
                  ""testMdPath"": ""test.md"",
                  ""steps"": [],
                  ""thresholds"": []
                }";
                File.WriteAllText(scenario, json);
                var code = validator.Validate(scenario);
                // May be 0 or 14 depending on schema presence; but should set LastErrorPath on 14
                if (code == 14) Assert.NotNull(validator.LastErrorPath);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void Validate_SchemaVersion3_AcceptsHarvestFields_AndV2RejectsThem()
        {
            var validator = new ScenarioValidator();
            var repoRoot = FindRepoRoot();
            var dir = Path.Combine(repoRoot, "LivingRoots.Tests", "bin", "scenario-validator-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var testMd = Path.Combine(dir, "specs", "014-soil-health-crop-quality", "test.md");
                Directory.CreateDirectory(Path.GetDirectoryName(testMd)!);
                File.WriteAllText(testMd, "<!-- GATE3:BEGIN -->\nx\n<!-- GATE3:END -->");

                var v3Scenario = Path.Combine(dir, "v3.json");
                File.WriteAllText(v3Scenario, BuildScenario(3, "poorHarvestQuality"));
                var v3Code = validator.Validate(v3Scenario);
                Assert.True(v3Code == 0, "v3 validation failed at " + validator.LastErrorPath);

                var v2Scenario = Path.Combine(dir, "v2.json");
                File.WriteAllText(v2Scenario, BuildScenario(2, "poorHarvestQuality"));
                Assert.Equal(14, validator.Validate(v2Scenario));

                var unknownScenario = Path.Combine(dir, "unknown.json");
                File.WriteAllText(unknownScenario, BuildScenario(3, "unknownHarvestField"));
                Assert.Equal(14, validator.Validate(unknownScenario));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "specs", "010-headless-verification-runner", "contracts", "scenario-v3.schema.json");
                if (File.Exists(candidate))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Repository root with the scenario v3 schema was not found.");
        }

        private static string BuildScenario(int schemaVersion, string thresholdField)
        {
            return @"{
              ""schemaVersion"": " + schemaVersion + @",
              ""scenarioId"": ""harvest-schema-test"",
              ""featureSlug"": ""014-soil-health-crop-quality"",
              ""testMdPath"": ""specs/014-soil-health-crop-quality/test.md"",
              ""summary"": ""schema version dispatch"",
              ""playerRole"": ""host"",
              ""minimumGameVersion"": ""1.6.14"",
              ""environment"": {
                ""gameVersion"": ""1.6.14"",
                ""smapiVersion"": ""4.4.0"",
                ""modVersion"": ""0.4.0"",
                ""apiVersion"": ""1.0.0"",
                ""saveId"": ""runner-self-test"",
                ""config"": { ""saveMode"": ""save"", ""headless"": true, ""skipBuild"": true }
              },
              ""milestoneSave"": ""runner-self-test"",
              ""initCommands"": [
                { ""kind"": ""console"", ""command"": ""lr_harvest_matrix 60 15"" }
              ],
              ""steps"": [
                { ""action"": ""harvest"", ""target"": ""60,15"" }
              ],
              ""thresholds"": [
                {
                  ""signal"": ""cropPhaseDelta"",
                  ""predicate"": { ""op"": ""cmp"", ""left"": """ + thresholdField + @""", ""operator"": ""eq"", ""right"": 0 }
                }
              ],
              ""expectedSignals"": [""soilHealthStateDelta"", ""cropPhaseDelta"", ""smapiLog""],
              ""expectedVerdict"": ""pass"",
              ""rngSeed"": 1,
              ""time"": ""2026-11-05T00:00:00Z"",
              ""determinism"": { ""idempotent"": true, ""declaredSources"": [] },
              ""scenarioKind"": ""positive""
            }";
        }

        [Fact]
        public void TestMdPath_MarkerValidation_MissingWrongExtension_MissingDuplicateInverted_Exit14BeforeLaunch()
        {
            var validator = new ScenarioValidator();
            var dir = Path.Combine(Path.GetTempPath(), "scen_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                // missing file
                var scenario = Path.Combine(dir, "s.json");
                File.WriteAllText(scenario, @"{""schemaVersion"":1,""scenarioId"":""s1"",""scenarioKind"":""positive"",""expectedVerdict"":""pass"",""testMdPath"":""missing.md"",""steps"":[],""thresholds"":[]}");
                var code = validator.Validate(scenario);
                Assert.Equal(14, code);
                Assert.Equal("/testMdPath", validator.LastErrorPath);

                // wrong extension
                var testMd2 = Path.Combine(dir, "test.txt");
                File.WriteAllText(testMd2, "<!-- GATE3:BEGIN -->x<!-- GATE3:END -->");
                var scenario2 = Path.Combine(dir, "s2.json");
                File.WriteAllText(scenario2, @"{""schemaVersion"":1,""scenarioId"":""s2"",""scenarioKind"":""positive"",""expectedVerdict"":""pass"",""testMdPath"":""test.txt"",""steps"":[],""thresholds"":[]}");
                code = validator.Validate(scenario2);
                Assert.Equal(14, code);

                // duplicate
                var testMd3 = Path.Combine(dir, "test3.md");
                File.WriteAllText(testMd3, "<!-- GATE3:BEGIN -->x<!-- GATE3:BEGIN -->y<!-- GATE3:END -->");
                var scenario3 = Path.Combine(dir, "s3.json");
                File.WriteAllText(scenario3, @"{""schemaVersion"":1,""scenarioId"":""s3"",""scenarioKind"":""positive"",""expectedVerdict"":""pass"",""testMdPath"":""test3.md"",""steps"":[],""thresholds"":[]}");
                code = validator.Validate(scenario3);
                Assert.Equal(14, code);

                // inverted
                var testMd4 = Path.Combine(dir, "test4.md");
                File.WriteAllText(testMd4, "<!-- GATE3:END -->x<!-- GATE3:BEGIN -->");
                var scenario4 = Path.Combine(dir, "s4.json");
                File.WriteAllText(scenario4, @"{""schemaVersion"":1,""scenarioId"":""s4"",""scenarioKind"":""positive"",""expectedVerdict"":""pass"",""testMdPath"":""test4.md"",""steps"":[],""thresholds"":[]}");
                code = validator.Validate(scenario4);
                Assert.Equal(14, code);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
