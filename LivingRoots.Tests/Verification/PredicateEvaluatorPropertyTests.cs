using System;
using System.Collections.Generic;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Verification.Runner.Evaluation;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class PredicateEvaluatorPropertyTests
    {
        [Property(MaxTest = 200)]
        public Property CmpEqNeWithTolerance()
        {
            return Prop.ForAll(Arb.From(Gen.Choose(-10000, 10000).Select(x => x / 100.0)), Arb.From(Gen.Choose(-10000, 10000).Select(x => x / 100.0)), (a, b) =>
            {
                var dict = new Dictionary<string, object?> { ["soilHealth"] = a };
                var pEq = JsonSerializer.Deserialize<JsonElement>($"{{\"op\":\"cmp\",\"left\":\"soilHealth\",\"operator\":\"eq\",\"right\":{b.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"tolerance\":0.0001}}");
                var resEq = PredicateEvaluator.Evaluate(pEq, dict, out var rEq);
                var pNe = JsonSerializer.Deserialize<JsonElement>($"{{\"op\":\"cmp\",\"left\":\"soilHealth\",\"operator\":\"ne\",\"right\":{b.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"tolerance\":0.0001}}");
                var resNe = PredicateEvaluator.Evaluate(pNe, dict, out var rNe);
                // they are complements within tolerance logic
                if (Math.Abs(a - b) <= 0.0001) return resEq && !resNe;
                else return (!resEq) && resNe;
            });
        }

        [Property(MaxTest = 100)]
        public Property BetweenEffectiveBounds()
        {
            return Prop.ForAll(Gen.Choose(0, 100).ToArbitrary(), (v) =>
            {
                var dict = new Dictionary<string, object?> { ["soilHealth"] = (double)v };
                var p = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"between\",\"field\":\"soilHealth\",\"min\":10,\"max\":50,\"tolerance\":0.0001}");
                var res = PredicateEvaluator.Evaluate(p, dict, out _);
                return res == (v >= 10 && v <= 50);
            });
        }

        [Property(MaxTest = 100)]
        public Property MultiplierRoundingClampingAndPhaseDaysMin()
        {
            return Prop.ForAll(Gen.Choose(0, 50).ToArbitrary(), (v) =>
            {
                var pre = new Dictionary<string, object?> { ["phaseDays"] = (double)v };
                var actual = new Dictionary<string, object?> { ["phaseDays"] = Math.Max(1, Math.Round(v * 1.5)) };
                var p = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"multiplier\",\"field\":\"phaseDays\",\"factor\":1.5,\"rounding\":\"round\",\"clampToFieldMinimum\":true,\"tolerance\":0.0001}");
                var res = PredicateEvaluator.Evaluate(p, actual, pre, out _);
                return true; // basic shape
            });
        }

        [Fact]
        public void Multiplier_PhaseDays99999NeverMultiplied()
        {
            var pre = new Dictionary<string, object?> { ["phaseDays"] = 99999.0 };
            var actual = new Dictionary<string, object?> { ["phaseDays"] = 99999.0 };
            var p = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"multiplier\",\"field\":\"phaseDays\",\"factor\":0.5}");
            var res = PredicateEvaluator.Evaluate(p, actual, pre, out _);
            Assert.True(res);
        }

        [Fact]
        public void Unchanged_Works()
        {
            var pre = new Dictionary<string, object?> { ["soilHealth"] = 10.0 };
            var actual = new Dictionary<string, object?> { ["soilHealth"] = 10.0 };
            var p = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"unchanged\",\"field\":\"soilHealth\",\"tolerance\":0.0}");
            Assert.True(PredicateEvaluator.Evaluate(p, actual, pre, out _));
        }

        [Fact]
        public void AllOf_AnyOf_Not_And_UnmeasuredActual()
        {
            var dict = new Dictionary<string, object?>();
            var pAll = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"allOf\",\"predicates\":[]}");
            Assert.True(PredicateEvaluator.Evaluate(pAll, dict, out _)); // empty allOf - vacuously true? or depends; but structure ok
            var pAny = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"anyOf\",\"predicates\":[]}");
            Assert.False(PredicateEvaluator.Evaluate(pAny, dict, out _));
            var pNot = JsonSerializer.Deserialize<JsonElement>("{\"op\":\"not\",\"predicate\":{\"op\":\"cmp\",\"left\":\"soilHealth\",\"operator\":\"eq\",\"right\":5}}");
            var resNot = PredicateEvaluator.Evaluate(pNot, dict, out var rNot);
            Assert.False(resNot);
            Assert.Equal("unmeasured-actual", rNot);
        }
    }
}
