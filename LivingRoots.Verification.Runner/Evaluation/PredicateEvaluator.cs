using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LivingRoots.Verification.Runner.Evaluation
{
    public sealed class PredicateEvaluationResult
    {
        public bool Passed { get; set; }

        public string? Reason { get; set; }
    }

    public sealed class PredicateEvaluator
    {
        public PredicateEvaluator()
        {
        }

        public static bool Evaluate(JsonElement predicate, JsonElement actual, out string reason)
        {
            return Evaluate(predicate, actual, (JsonElement?)null, out reason);
        }

        public static bool Evaluate(JsonElement predicate, JsonElement actual, JsonElement? pre, out string reason)
        {
            var actualDict = ToDictionary(actual);
            var preDict = pre.HasValue ? ToDictionary(pre.Value) : null;
            return EvaluateCore(predicate, actualDict, preDict, out reason);
        }

        public static PredicateEvaluationResult Evaluate(JsonElement predicate, JsonElement actual, JsonElement? pre = null)
        {
            var result = new PredicateEvaluationResult();
            result.Passed = Evaluate(predicate, actual, pre, out var reason);
            result.Reason = reason;
            return result;
        }

        public static bool Evaluate(JsonElement predicate, IDictionary<string, object?> actual, out string reason)
        {
            return EvaluateCore(predicate, actual, null, out reason);
        }

        public static bool Evaluate(JsonElement predicate, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            return EvaluateCore(predicate, actual, pre, out reason);
        }

        public static PredicateEvaluationResult Evaluate(JsonElement predicate, IDictionary<string, object?> actual, IDictionary<string, object?>? pre = null)
        {
            var result = new PredicateEvaluationResult();
            result.Passed = EvaluateCore(predicate, actual, pre, out var reason);
            result.Reason = reason;
            return result;
        }

        private static IDictionary<string, object?> ToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = JsonToValue(prop.Value);
                }
            }
            return dict;
        }

        private static object? JsonToValue(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Number:
                    if (e.TryGetInt64(out var l))
                    {
                        return l;
                    }
                    return e.GetDouble();
                case JsonValueKind.String:
                    return e.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return e.Clone();
            }
        }

        private static bool EvaluateCore(JsonElement predicate, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            if (predicate.ValueKind != JsonValueKind.Object)
            {
                reason = "invalid-predicate";
                return false;
            }

            var op = predicate.TryGetProperty("op", out var opProp) ? opProp.GetString() : null;
            switch (op)
            {
                case "cmp":
                    return EvalCmp(predicate, actual, out reason);
                case "between":
                    return EvalBetween(predicate, actual, out reason);
                case "multiplier":
                    return EvalMultiplier(predicate, actual, pre, out reason);
                case "sentinel":
                    return EvalSentinel(predicate, actual, pre, out reason);
                case "unchanged":
                    return EvalUnchanged(predicate, actual, pre, out reason);
                case "allOf":
                    return EvalAllOf(predicate, actual, pre, out reason);
                case "anyOf":
                    return EvalAnyOf(predicate, actual, pre, out reason);
                case "not":
                    return EvalNot(predicate, actual, pre, out reason);
                default:
                    reason = "unknown-op";
                    return false;
            }
        }

        private static bool IsNumericField(string field)
        {
            return field is "soilHealth"
                or "phaseDays"
                or "currentPhase"
                or "dayOfCurrentPhase"
                or "indexOfHarvest"
                or "elapsedMilliseconds"
                or "locationsProcessed"
                or "poorHarvestQuality"
                or "moderateHarvestQuality"
                or "healthyHarvestQuality"
                or "poorExtraCount"
                or "moderateExtraCount"
                or "healthyExtraCount"
                or "healthyExtraQuality"
                or "poorVanillaQuality"
                or "moderateVanillaQuality"
                or "healthyVanillaQuality"
                or "healthyQualityRoll"
                or "healthyExtraRoll";
        }

        private static bool IsBooleanField(string field)
        {
            return field is "fullyGrown" or "dead" or "isMainPlayer";
        }

        private static bool TryGetNumeric(IDictionary<string, object?> dict, string field, out double value, out bool unmeasured)
        {
            unmeasured = false;
            value = 0;
            if (!dict.TryGetValue(field, out var raw) || raw is null)
            {
                unmeasured = true;
                return false;
            }

            if (raw is string s)
            {
                if (s == "NaN")
                {
                    unmeasured = true;
                    return false;
                }
                if (double.TryParse(s, out var d))
                {
                    value = d;
                    if (double.IsNaN(value))
                    {
                        unmeasured = true;
                        return false;
                    }
                    return true;
                }
                unmeasured = true;
                return false;
            }

            if (raw is bool)
            {
                unmeasured = true;
                return false;
            }

            try
            {
                value = Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                unmeasured = true;
                return false;
            }

            if (double.IsNaN(value))
            {
                unmeasured = true;
                return false;
            }

            return true;
        }

        private static bool EvalCmp(JsonElement p, IDictionary<string, object?> actual, out string reason)
        {
            reason = string.Empty;
            var left = p.GetProperty("left").GetString()!;
            var op = p.GetProperty("operator").GetString()!;
            var tol = p.TryGetProperty("tolerance", out var t) ? t.GetDouble() : 0.0001;

            if (IsNumericField(left))
            {
                if (!TryGetNumeric(actual, left, out var lv, out var unm))
                {
                    if (unm)
                    {
                        reason = "unmeasured-actual";
                    }
                    else
                    {
                        reason = "missing-field";
                    }
                    return false;
                }

                var right = p.GetProperty("right").GetDouble();
                switch (op)
                {
                    case "eq":
                        return Math.Abs(lv - right) <= tol;
                    case "ne":
                        return Math.Abs(lv - right) > tol;
                    case "lt":
                        return lv < right;
                    case "lte":
                        return lv <= right;
                    case "gt":
                        return lv > right;
                    case "gte":
                        return lv >= right;
                    default:
                        reason = "unknown-operator";
                        return false;
                }
            }

            if (IsBooleanField(left))
            {
                if (!actual.TryGetValue(left, out var raw) || raw is null)
                {
                    reason = "unmeasured-actual";
                    return false;
                }
                var lb = raw is bool b ? b : Convert.ToBoolean(raw, System.Globalization.CultureInfo.InvariantCulture);
                var rb = p.GetProperty("right").GetBoolean();
                return op == "eq" ? lb == rb : lb != rb;
            }

            if (!actual.TryGetValue(left, out var raws) || raws is null)
            {
                reason = "unmeasured-actual";
                return false;
            }
            var ls = raws.ToString()!;
            var rs = p.GetProperty("right").GetString()!;
            return op == "eq" ? ls == rs : ls != rs;
        }

        private static bool EvalBetween(JsonElement p, IDictionary<string, object?> actual, out string reason)
        {
            reason = string.Empty;
            var field = p.GetProperty("field").GetString()!;
            if (!IsNumericField(field))
            {
                reason = "invalid-field";
                return false;
            }

            if (!TryGetNumeric(actual, field, out var av, out var unm))
            {
                if (unm)
                {
                    reason = "unmeasured-actual";
                }
                else
                {
                    reason = "missing-field";
                }
                return false;
            }

            var min = p.GetProperty("min").GetDouble();
            var max = p.GetProperty("max").GetDouble();
            var tol = p.GetProperty("tolerance").GetDouble();
            var exclMin = p.TryGetProperty("exclusiveMin", out var em) && em.GetBoolean();
            var exclMax = p.TryGetProperty("exclusiveMax", out var ex) && ex.GetBoolean();

            var effMin = min - tol;
            var effMax = max + tol;
            var lower = exclMin ? av > effMin : av >= effMin;
            var upper = exclMax ? av < effMax : av <= effMax;
            return lower && upper;
        }

        private static bool EvalMultiplier(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var field = p.GetProperty("field").GetString()!;
            if (!IsNumericField(field))
            {
                reason = "invalid-field";
                return false;
            }

            if (!TryGetNumeric(actual, field, out var av, out var unm))
            {
                if (unm)
                {
                    reason = "unmeasured-actual";
                }
                else
                {
                    reason = "missing-field";
                }
                return false;
            }

            var factor = p.GetProperty("factor").GetDouble();
            var rounding = p.TryGetProperty("rounding", out var r) ? r.GetString()! : "round";
            var clamp = !p.TryGetProperty("clampToFieldMinimum", out var c) || c.GetBoolean();
            var tol = p.TryGetProperty("tolerance", out var tolProp) ? tolProp.GetDouble() : 0.0001;

            double preVal = 0;
            var preUnmeasured = true;
            if (pre is not null)
            {
                if (TryGetNumeric(pre, field, out preVal, out var unmPre))
                {
                    preUnmeasured = false;
                }
                else
                {
                    preUnmeasured = unmPre;
                }
            }

            double computed;
            if (field == "phaseDays" && !preUnmeasured && preVal == 99999)
            {
                computed = 99999;
            }
            else
            {
                if (preUnmeasured)
                {
                    reason = "unmeasured-actual";
                    return false;
                }

                var product = preVal * factor;
                double rounded;
                switch (rounding)
                {
                    case "floor":
                        rounded = Math.Floor(product);
                        break;
                    case "ceil":
                        rounded = Math.Ceiling(product);
                        break;
                    case "none":
                        rounded = product;
                        break;
                    default:
                        rounded = Math.Round(product, MidpointRounding.ToEven);
                        break;
                }

                if (clamp)
                {
                    var min = field == "phaseDays" ? 1.0 : 0.0;
                    computed = Math.Max(rounded, min);
                }
                else
                {
                    computed = rounded;
                }
            }

            return Math.Abs(av - computed) <= tol;
        }

        private static bool EvalSentinel(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var field = p.GetProperty("field").GetString()!;
            var sentinel = p.GetProperty("sentinel").GetString()!;
            var mustNotChange = !p.TryGetProperty("mustNotChange", out var m) || m.GetBoolean();

            if (!SentinelHolds(sentinel, actual))
            {
                reason = "sentinel-failed";
                return false;
            }

            if (mustNotChange && pre is not null)
            {
                if (!SentinelHolds(sentinel, pre))
                {
                    reason = "sentinel-changed";
                    return false;
                }
            }

            return true;
        }

        private static bool SentinelHolds(string sentinel, IDictionary<string, object?> dict)
        {
            switch (sentinel)
            {
                case "SENTINEL_UNTOUCHED_99999":
                    if (!TryGetNumeric(dict, "phaseDays", out var v, out _))
                    {
                        return false;
                    }
                    return v == 99999;
                case "NAN_HEALTH":
                    if (!dict.TryGetValue("healthTier", out var ht) || ht?.ToString() != "unknown")
                    {
                        return false;
                    }
                    if (!dict.TryGetValue("soilHealthState", out var ss) || ss?.ToString() != "nan")
                    {
                        return false;
                    }
                    return true;
                case "MISSING_KEY":
                    if (!dict.TryGetValue("healthTier", out var ht2) || ht2?.ToString() != "unknown")
                    {
                        return false;
                    }
                    if (!dict.TryGetValue("soilHealthState", out var ss2) || ss2?.ToString() != "missing")
                    {
                        return false;
                    }
                    return true;
                case "HEALTH_LE_ZERO":
                    return dict.TryGetValue("healthTier", out var ht3) && ht3?.ToString() == "nonParticipant";
                case "POOR_POSITIVE_LT_31" or "POOR_POSITIVE_LT_34":
                    return dict.TryGetValue("healthTier", out var ht4) && ht4?.ToString() == "poor";
                case "PHASE_MINUS_ONE":
                    if (!TryGetNumeric(dict, "currentPhase", out var cp, out _))
                    {
                        return false;
                    }
                    return cp == -1;
                default:
                    return false;
            }
        }

        private static bool EvalUnchanged(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var field = p.GetProperty("field").GetString()!;
            if (!IsNumericField(field))
            {
                reason = "invalid-field";
                return false;
            }

            if (!TryGetNumeric(actual, field, out var post, out var unmPost))
            {
                if (unmPost)
                {
                    reason = "unmeasured-actual";
                }
                else
                {
                    reason = "missing-field";
                }
                return false;
            }

            bool unmPre = false;
            double preVal = 0;
            if (pre is null || !TryGetNumeric(pre, field, out preVal, out unmPre))
            {
                if (unmPre)
                {
                    reason = "unmeasured-actual";
                }
                else
                {
                    reason = "missing-pre";
                }
                return false;
            }

            var tol = p.TryGetProperty("tolerance", out var t) ? t.GetDouble() : 0.0;
            return Math.Abs(post - preVal) <= tol;
        }

        private static bool EvalAllOf(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var preds = p.GetProperty("predicates");
            string? firstReason = null;
            var allPass = true;
            foreach (var child in preds.EnumerateArray())
            {
                if (!EvaluateCore(child, actual, pre, out var r))
                {
                    allPass = false;
                    if (firstReason is null)
                    {
                        firstReason = r;
                    }
                }
            }

            if (!allPass)
            {
                reason = firstReason ?? "allOf-failed";
            }
            return allPass;
        }

        private static bool EvalAnyOf(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var preds = p.GetProperty("predicates");
            string? lastReason = null;
            foreach (var child in preds.EnumerateArray())
            {
                if (EvaluateCore(child, actual, pre, out var r))
                {
                    return true;
                }
                lastReason = r;
            }

            reason = lastReason ?? "anyOf-failed";
            return false;
        }

        private static bool EvalNot(JsonElement p, IDictionary<string, object?> actual, IDictionary<string, object?>? pre, out string reason)
        {
            reason = string.Empty;
            var child = p.GetProperty("predicate");
            var childPassed = EvaluateCore(child, actual, pre, out var r);
            if (r == "unmeasured-actual")
            {
                reason = r;
                return false;
            }
            if (childPassed)
            {
                reason = "not-failed";
                return false;
            }
            return true;
        }
    }
}
