using System;
using System.IO;
using Xunit;

namespace LivingRoots.Tests.Architecture
{
    /// <summary>
    /// T176 (Phase 20 Wave 1, TDD RED): source-architecture assertions that the
    /// visualization literals enumerated below live in <c>ModConstants</c>
    /// (<c>LivingRoots/Constants.cs</c>), not inline at their use sites.
    /// Every use-site assertion fails today (red) — each literal is still
    /// hardcoded at the site the task lists. The two tests at the end pin the
    /// allowlist: the ModConstants palette definitions (the palette's legitimate
    /// home) and the documented canonical framework named-color usages (the
    /// shared white texture), which stay inline by design.
    /// Known adjacent literals outside T176's enumerated scope (not asserted
    /// here): the half-tile offset <c>32f</c> and the sprite fade rate
    /// <c>0.02f</c> in CompostApplicationService.cs lines 64/76, and the
    /// <c>Color.White</c> default on the TooltipData DTO
    /// (Domain/Visualization/TooltipData.cs line 22).
    /// </summary>
    public class NoHardcodedVisualizationLiteralsTests
    {
        [Fact]
        public void ColorInterpolationService_CategoryThresholds_ResolveFromModConstants()
        {
            // Sites: ColorInterpolationService.cs lines 112, 117, 168, 171,
            // 174, 177 — the Poor/Moderate band edge (34f), the Moderate/
            // Healthy band edge (67f), and the Moderate band width (33f),
            // hardcoded in both the category resolution and the interpolation
            // ramps. RED today: all six sites are inline.
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "Visualization",
                "ColorInterpolationService.cs");

            Assert.DoesNotContain("34f", source, StringComparison.Ordinal);
            Assert.DoesNotContain("67f", source, StringComparison.Ordinal);
            Assert.DoesNotContain("33f", source, StringComparison.Ordinal);
        }

        [Fact]
        public void CompostApplicationService_TileSize_ResolveFromModConstants()
        {
            // Site: CompostApplicationService.cs line 64 — the tile-size
            // multiplier 64f in the floating-text world position. RED today:
            // inline (ModConstants.TileSize already exists as the int form).
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "CompostApplicationService.cs");

            Assert.DoesNotContain("64f", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ModController_TileSize_ResolveFromModConstants()
        {
            // Site: ModController.cs line 2109 — the tile-size multiplier
            // 64f converting a tile coordinate to world pixels for debris.
            // RED today: inline.
            var source = ReadProductionSource(
                "LivingRoots",
                "Controllers",
                "ModController.cs");

            Assert.DoesNotContain("64f", source, StringComparison.Ordinal);
        }

        [Fact]
        public void HoeFeedbackRenderer_DriftRate_ResolveFromModConstants()
        {
            // Site: HoeFeedbackRenderer.cs line 235 — the floating-text
            // upward drift rate 0.02f (pixels per millisecond). RED today:
            // inline.
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "Visualization",
                "HoeFeedbackRenderer.cs");

            Assert.DoesNotContain("0.02f", source, StringComparison.Ordinal);
        }

        [Fact]
        public void HoeFeedbackRenderer_TextAndShadowColors_ResolveFromModConstants()
        {
            // Sites: HoeFeedbackRenderer.cs lines 122-123 — the floating-text
            // color (opaque white) and its drop shadow (black at half alpha),
            // constructed inline with hardcoded RGB and alpha literals. The
            // (int)(opacity * 255f) / (int)(opacity * 128f) alpha-scale
            // arithmetic is conversion idiom, not a palette literal, and is
            // not asserted. RED today: both constructions are inline.
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "Visualization",
                "HoeFeedbackRenderer.cs");

            Assert.DoesNotContain("new Color(255, 255, 255", source, StringComparison.Ordinal);
            Assert.DoesNotContain("new Color(0, 0, 0", source, StringComparison.Ordinal);
        }

        [Fact]
        public void TooltipRenderer_TextColor_ResolveFromModConstants()
        {
            // Sites: TooltipRenderer.cs lines 118 and 153 — the tooltip text
            // color Color.White, assigned inline in both the known-state and
            // unknown-state tooltip builders. RED today: inline.
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "Visualization",
                "TooltipRenderer.cs");

            Assert.DoesNotContain("Color.White", source, StringComparison.Ordinal);
        }

        [Fact]
        public void CompostApplicationService_SpriteColor_ResolveFromModConstants()
        {
            // Site: CompostApplicationService.cs line 75 — the floating
            // "+N" sprite color Color.Green, assigned inline. RED today:
            // inline (ModConstants carries a compost palette the fix can
            // defer to, e.g. CompostReadyColor).
            var source = ReadProductionSource(
                "LivingRoots",
                "Services",
                "CompostApplicationService.cs");

            Assert.DoesNotContain("Color.Green", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ModConstants_RemainsTheAllowlistedHomeOfTheVisualizationPalette()
        {
            // Allowlist: the ModConstants palette definitions themselves
            // (Constants.cs). These are the
            // single source of truth the use-site assertions above defer to —
            // a color literal is only a violation outside this file.
            var source = ReadProductionSource("LivingRoots", "Constants.cs");

            var allowlistedDeclarations = new[]
            {
                "public static readonly Color PoorColor",
                "public static readonly Color ModerateColor",
                "public static readonly Color HealthyColor",
                "public static readonly Color UnknownColor",
                "public static readonly Color PatternWhiteTone",
                "public static readonly Color PatternBlackTone",
                "public static readonly Color TooltipPanelColor",
                // T238 reconciliation: the compost/environment palette
                // constants that once appeared here were consumed only by
                // future-roadmap briefings. The unreferenced-constants
                // guard forbids speculative declarations, so those entries
                // were removed with their constants; CompostSpriteColor is
                // the live compost palette member the +N feedback uses.
                "public static readonly Color CompostSpriteColor",
            };

            foreach (var declaration in allowlistedDeclarations)
            {
                Assert.Contains(declaration, source, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void CanonicalFrameworkNamedColors_RemainAllowlisted()
        {
            // Allowlist: the documented canonical use of a framework named
            // color is the shared white texture (Visualization AGENTS.md:
            // "White texture singleton") — a 1x1 white texture uploaded for
            // rectangle drawing, in the two places that lazily create it.
            // These are the framework color's single correct use, not palette
            // literals, and stay inline by design; none of the forbidden
            // patterns above match a SetData upload.
            var controllerSource = ReadProductionSource(
                "LivingRoots",
                "Controllers",
                "ModController.cs");
            Assert.Contains(
                "whiteTexture.SetData(new[] { Microsoft.Xna.Framework.Color.White });",
                controllerSource,
                StringComparison.Ordinal);

            var hoeFeedbackSource = ReadProductionSource(
                "LivingRoots",
                "Services",
                "Visualization",
                "HoeFeedbackRenderer.cs");
            Assert.Contains(
                "texture.SetData(new[] { Color.White });",
                hoeFeedbackSource,
                StringComparison.Ordinal);
        }

        // File-locating helper: walk up from the test output directory to the
        // solution root. Scaffold copied from ArchitecturalPlanTests.cs's
        // sln-walk (GetArchitecturalPlanFiles): T172's DomainLayeringTests was
        // not present in the tree at authoring time, so its helper shape could
        // not be coordinated; the ArchitecturalPlanTests walk is the in-repo
        // precedent for source-locating tests.
        private static bool TryGetSolutionRoot(out string solutionRoot)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Stardew-LivingRoots.sln")))
                {
                    solutionRoot = dir.FullName;
                    return true;
                }

                dir = dir.Parent;
            }

            solutionRoot = string.Empty;
            return false;
        }

        private static string ReadProductionSource(params string[] relativePathSegments)
        {
            if (!TryGetSolutionRoot(out var solutionRoot))
            {
                Assert.Fail("Could not locate Stardew-LivingRoots.sln above the test output directory.");
            }

            var fullPath = Path.Combine(solutionRoot, Path.Combine(relativePathSegments));
            Assert.True(File.Exists(fullPath), $"Production source file not found: {fullPath}");
            return File.ReadAllText(fullPath);
        }
    }
}
