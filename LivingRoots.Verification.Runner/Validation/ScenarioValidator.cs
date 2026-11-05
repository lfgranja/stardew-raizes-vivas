using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace LivingRoots.Verification.Runner.Validation
{
    public sealed class ScenarioValidator
    {
        public ScenarioValidator()
        {
        }

        public string? LastErrorPath { get; private set; }

        public int Validate(string scenarioPath)
        {
            LastErrorPath = null;
            try
            {
                if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
                {
                    return 14;
                }

                var fullPath = Path.GetFullPath(scenarioPath);
                string text;
                try
                {
                    text = File.ReadAllText(fullPath);
                }
                catch
                {
                    return 14;
                }

                JsonNode? node;
                try
                {
                    node = JsonNode.Parse(text);
                }
                catch
                {
                    return 14;
                }

                if (node is not JsonObject obj)
                {
                    return 14;
                }

                int schemaVersion = 2;
                if (obj["schemaVersion"] is JsonValue sv && sv.TryGetValue<int>(out var parsedVersion))
                {
                    schemaVersion = parsedVersion;
                }

                var schemaPath = FindSchema(fullPath, schemaVersion);
                if (schemaPath is not null)
                {
                    try
                    {
                        var schemaJson = File.ReadAllText(schemaPath);
                        var schema = JsonSchema.FromText(schemaJson);
                        using var doc = JsonDocument.Parse(node.ToJsonString());
                        var result = schema.Evaluate(doc.RootElement);
                        if (!result.IsValid)
                        {
                            LastErrorPath = result.InstanceLocation.ToString();
                            if (string.IsNullOrEmpty(LastErrorPath))
                            {
                                LastErrorPath = "/";
                            }
                            return 14;
                        }
                    }
                    catch
                    {
                        // Fall back to manual validation below.
                    }
                }

                if (!ValidateCrossFields(obj, fullPath, out var crossPath))
                {
                    LastErrorPath = crossPath;
                    return 14;
                }

                if (obj["testMdPath"] is JsonValue tmv && tmv.TryGetValue<string>(out var testMdPath) && !string.IsNullOrWhiteSpace(testMdPath))
                {
                    if (!ValidateTestMd(fullPath, testMdPath, out var mdPath))
                    {
                        LastErrorPath = mdPath;
                        return 14;
                    }
                }
                else
                {
                    LastErrorPath = "/testMdPath";
                    return 14;
                }

                return 0;
            }
            catch
            {
                return 14;
            }
        }

        private static string? FindSchema(string scenarioPath, int schemaVersion)
        {
            string schemaFile = schemaVersion == 3 ? "scenario-v3.schema.json" : "scenario.schema.json";
            var dirs = new List<string>();
            var d = Path.GetDirectoryName(scenarioPath);
            while (!string.IsNullOrEmpty(d))
            {
                dirs.Add(d!);
                d = Path.GetDirectoryName(d);
            }
            dirs.Add(Directory.GetCurrentDirectory());

            foreach (var dir in dirs)
            {
                var cand = Path.Combine(dir, "specs", "010-headless-verification-runner", "contracts", schemaFile);
                if (File.Exists(cand))
                {
                    return cand;
                }

                cand = Path.Combine(dir, "contracts", schemaFile);
                if (File.Exists(cand))
                {
                    return cand;
                }
            }

            return null;
        }

        private static bool ValidateCrossFields(JsonObject obj, string scenarioPath, out string path)
        {
            path = "/";

            try
            {
                var kind = obj["scenarioKind"]?.GetValue<string>();
                var ev = obj["expectedVerdict"]?.GetValue<string>();
                if (kind == "positive")
                {
                    if (ev is not null && ev != "pass")
                    {
                        path = "/expectedVerdict";
                        return false;
                    }
                }
                else if (kind == "negative")
                {
                    if (string.IsNullOrEmpty(ev) || ev == "pass")
                    {
                        path = "/expectedVerdict";
                        return false;
                    }
                }
                else if (kind == "boundary")
                {
                    if (string.IsNullOrEmpty(ev) || (ev != "pass" && ev != "fail"))
                    {
                        path = "/expectedVerdict";
                        return false;
                    }
                }
            }
            catch
            {
            }

            if (obj["steps"] is JsonArray steps)
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    if (steps[i] is not JsonObject step)
                    {
                        continue;
                    }

                    var action = step["action"]?.GetValue<string>();
                    var inputs = step["inputs"] as JsonObject;
                    if (action == "insertWaste")
                    {
                        if (inputs is null || !inputs.ContainsKey("itemId") || !inputs.ContainsKey("count"))
                        {
                            path = "/steps/" + i + "/inputs";
                            return false;
                        }
                    }
                    else if (action == "setSoilHealth")
                    {
                        if (inputs is null || !inputs.ContainsKey("soilHealth"))
                        {
                            path = "/steps/" + i + "/inputs";
                            return false;
                        }
                    }
                    else if (action == "runMacro")
                    {
                        if (inputs is null || !inputs.ContainsKey("macroFile"))
                        {
                            path = "/steps/" + i + "/inputs";
                            return false;
                        }
                    }
                }
            }

            if (obj["thresholds"] is JsonArray thresholds)
            {
                for (var t = 0; t < thresholds.Count; t++)
                {
                    if (thresholds[t] is not JsonObject thr)
                    {
                        continue;
                    }

                    var signal = thr["signal"]?.GetValue<string>();
                    var predicate = thr["predicate"] as JsonObject;
                    if (predicate is null)
                    {
                        path = "/thresholds/" + t + "/predicate";
                        return false;
                    }

                    var op = predicate["op"]?.GetValue<string>();
                    string? field = null;
                    if (op == "cmp")
                    {
                        field = predicate["left"]?.GetValue<string>();
                    }
                    else if (predicate.ContainsKey("field"))
                    {
                        field = predicate["field"]?.GetValue<string>();
                    }

                    if (signal is not null && field is not null)
                    {
                        var allowed = AllowedFields(signal);
                        if (!allowed.Contains(field))
                        {
                            path = "/thresholds/" + t + "/predicate";
                            return false;
                        }
                    }

                    if (op == "multiplier")
                    {
                        var baseRef = predicate["base"]?.GetValue<string>();
                        if (baseRef is not null)
                        {
                            var suffix = baseRef.StartsWith("pre.", StringComparison.Ordinal) ? baseRef.Substring(4) : baseRef;
                            if (suffix != field)
                            {
                                path = "/thresholds/" + t + "/predicate/base";
                                return false;
                            }
                        }

                        if (predicate["factor"] is JsonValue fv && fv.TryGetValue<double>(out var f))
                        {
                            if (f < 0.01 || f > 100)
                            {
                                path = "/thresholds/" + t + "/predicate/factor";
                                return false;
                            }
                        }
                    }

                    if (op == "between")
                    {
                        if (predicate["min"] is JsonValue minv && predicate["max"] is JsonValue maxv &&
                            minv.TryGetValue<double>(out var mn) && maxv.TryGetValue<double>(out var mx))
                        {
                            if (mn > mx)
                            {
                                path = "/thresholds/" + t + "/predicate/min";
                                return false;
                            }
                        }
                    }

                    if (op == "sentinel")
                    {
                        var sentinel = predicate["sentinel"]?.GetValue<string>();
                        if (!SentinelFieldOk(sentinel, field))
                        {
                            path = "/thresholds/" + t + "/predicate/sentinel";
                            return false;
                        }
                    }

                    if (signal == "smapiLog" && thr.ContainsKey("subject"))
                    {
                        path = "/thresholds/" + t + "/subject";
                        return false;
                    }
                }
            }

            try
            {
                var role = obj["playerRole"]?.GetValue<string>();
                if (role == "farmhand" && !obj.ContainsKey("hostGuard"))
                {
                    path = "/hostGuard";
                    return false;
                }
                if (role == "host" && obj.ContainsKey("hostGuard"))
                {
                    path = "/hostGuard";
                    return false;
                }
            }
            catch
            {
            }

            return true;
        }

        private static HashSet<string> AllowedFields(string signal)
        {
            if (signal == "soilHealthStateDelta")
            {
                return new HashSet<string> { "soilHealth", "healthTier", "soilHealthState" };
            }
            if (signal == "cropPhaseDelta")
            {
                return new HashSet<string>
                {
                    "phaseDays",
                    "currentPhase",
                    "dayOfCurrentPhase",
                    "fullyGrown",
                    "dead",
                    "indexOfHarvest",
                    "healthTier",
                    "soilHealthState",
                    "poorHarvestQuality",
                    "moderateHarvestQuality",
                    "healthyHarvestQuality",
                    "poorExtraCount",
                    "moderateExtraCount",
                    "healthyExtraCount",
                    "healthyExtraQuality",
                    "poorVanillaQuality",
                    "moderateVanillaQuality",
                    "healthyVanillaQuality",
                    "healthyQualityRoll",
                    "healthyExtraRoll",
                };
            }
            if (signal == "smapiLog")
            {
                return new HashSet<string> { "elapsedMilliseconds", "eventName", "locationsProcessed", "isMainPlayer" };
            }
            return new HashSet<string>();
        }

        private static bool SentinelFieldOk(string? sentinel, string? field)
        {
            if (sentinel == "SENTINEL_UNTOUCHED_99999")
            {
                return field == "phaseDays";
            }
            if (sentinel is "NAN_HEALTH" or "MISSING_KEY" or "HEALTH_LE_ZERO" or "POOR_POSITIVE_LT_34")
            {
                return field == "healthTier";
            }
            if (sentinel == "PHASE_MINUS_ONE")
            {
                return field == "currentPhase";
            }
            return false;
        }

        private static bool ValidateTestMd(string scenarioPath, string testMdPath, out string path)
        {
            path = "/testMdPath";

            string? candidate = null;
            var baseDir = Path.GetDirectoryName(Path.GetFullPath(scenarioPath))!;
            if (Path.IsPathRooted(testMdPath))
            {
                candidate = testMdPath;
            }
            else
            {
                var c1 = Path.Combine(baseDir, testMdPath);
                if (File.Exists(c1))
                {
                    candidate = c1;
                }
                else
                {
                    var c2 = Path.Combine(Directory.GetCurrentDirectory(), testMdPath);
                    if (File.Exists(c2))
                    {
                        candidate = c2;
                    }
                }
            }

            if (candidate is null || !File.Exists(candidate))
            {
                return false;
            }

            if (!testMdPath.EndsWith("test.md", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string content;
            try
            {
                content = File.ReadAllText(candidate);
            }
            catch
            {
                return false;
            }

            const string begin = "<!-- GATE3:BEGIN -->";
            const string end = "<!-- GATE3:END -->";
            var beginCount = CountOccurrences(content, begin);
            var endCount = CountOccurrences(content, end);
            var bi = content.IndexOf(begin, StringComparison.Ordinal);
            var ei = content.IndexOf(end, StringComparison.Ordinal);

            if (beginCount != 1 || endCount != 1)
            {
                return false;
            }
            if (bi < 0 || ei < 0 || bi > ei)
            {
                return false;
            }

            return true;
        }

        private static int CountOccurrences(string s, string token)
        {
            var count = 0;
            var idx = 0;
            while ((idx = s.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += token.Length;
            }
            return count;
        }
    }
}
