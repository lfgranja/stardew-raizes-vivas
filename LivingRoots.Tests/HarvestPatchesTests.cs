using System;
using System.Linq;
using System.Reflection;
using LivingRoots.Patches;
using Xunit;

namespace LivingRoots.Tests
{
    public class HarvestPatchesTests
    {
        [Fact]
        public void HarvestPrefix_IsStatic()
        {
            var method = typeof(HarvestPatches).GetMethod("HarvestPrefix", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            Assert.True(method!.IsStatic);
        }

        [Fact]
        public void HarvestPostfix_IsStatic()
        {
            var method = typeof(HarvestPatches).GetMethod("HarvestPostfix", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            Assert.True(method!.IsStatic);
        }

        [Fact]
        public void NoPatchAll_AnnotationUsage()
        {
            // Verify no PatchAll attribute on the class
            var attrs = typeof(HarvestPatches).GetCustomAttributes(false);
            Assert.DoesNotContain(attrs, a => a.GetType().Name.Contains("HarmonyPatch"));
        }

        [Fact]
        public void Initialize_AcceptsMonitorAndService()
        {
            var method = typeof(HarvestPatches).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            Assert.True(method!.IsStatic);
            Assert.Equal(4, method.GetParameters().Length);
        }
    }
}
