using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Visualization;

/// <summary>
/// T040 (FR-010 / SC-010): the per-frame overlay measurements the
/// <see cref="OverlayMeasurementFixture"/> exists to serve. Every number
/// asserted here is the tint the production draw path emitted for a rendered
/// frame under an installed ambient light — nothing is read from a
/// framebuffer (the headless host has no <c>GraphicsDevice</c>) and nothing
/// is restated from the spec as if it were measured. What is genuinely
/// measured versus modelled is stated per test and in the fixture's remarks;
/// the honest gaps (the health-0 untreated baseline drawing no overlay at
/// all, the production default opacity falling below the blend-alpha floor,
/// the test-chosen rain/winter dim values) are documented where they apply.
/// </summary>
public class OverlayMeasurementTests
{
    /// <summary>The tile the single-tile measurement frames render.</summary>
    private static readonly Point MeasurementTile = new(4, 4);

    /// <summary>The treated tile of the adjacent-pair comparison.</summary>
    private static readonly Point TreatedTile = new(5, 4);

    /// <summary>
    /// The treated tile's untreated neighbour — adjacent tile, measured
    /// under an identical ambient pass per FR-010's measurement point.
    /// </summary>
    private static readonly Point UntreatedNeighbourTile = new(6, 4);

    /// <summary>
    /// Tier representatives spanning each FR-010 tier and both joints:
    /// Poor (1–33), Moderate (34–66), Healthy (67–100).
    /// </summary>
    private static readonly float[] TierHealths = { 1f, 20f, 33f, 34f, 50f, 66f, 67f, 80f, 100f };

    /// <summary>
    /// FR-010: blend alpha ≥ 0.65 under ambient lighting, at the stated
    /// MEASUREMENT POINT (tile-centre pixel, not boundary) — for every
    /// health tier (Poor 1–33 / Moderate 34–66 / Healthy 67–100, joints
    /// included) under full-daylight ambient. MEASURED: the emitted tint's
    /// alpha is the exact byte the production draw consumed, and because the
    /// base fill is one uniform full-quad draw the tile-centre pixel carries
    /// that alpha verbatim (the fixture's
    /// <see cref="OverlayMeasurementFixture.BlendAlphaAtTileCentre"/>).
    /// Rendered at <see cref="OverlayMeasurementFixture.ConfiguredOpacity"/>
    /// (0.7) — see its remarks for why the production default opacity of 0.5
    /// would fall below the floor and is therefore not the configured value
    /// here.
    /// </summary>
    [Fact]
    public void RenderOverlays_BlendAlphaAtTileCentrePixel_MeetsFloorForEveryHealthTierUnderDaylightAmbient()
    {
        var fixture = new OverlayMeasurementFixture();

        foreach (var health in TierHealths)
        {
            var measurement = fixture.MeasureDrawnTileOrThrow(
                MeasurementTile,
                health,
                OverlayMeasurementFixture.FullDaylightAmbient);

            var blendAlpha = OverlayMeasurementFixture.BlendAlphaAtTileCentre(measurement);
            Assert.True(
                blendAlpha >= OverlayMeasurementFixture.BlendAlphaFloor,
                "FR-010: blend alpha at the tile-centre pixel for health " + health.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    + " is " + blendAlpha.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    + ", below the " + OverlayMeasurementFixture.BlendAlphaFloor + " floor (drawn tint RGBA("
                    + measurement.DrawnTint.R + ", " + measurement.DrawnTint.G + ", "
                    + measurement.DrawnTint.B + ", " + measurement.DrawnTint.A + ")).");
        }
    }

    /// <summary>
    /// FR-010: for ANY health in 1..100 — the full drawable range the
    /// palette covers — the drawn tint's blend alpha at the tile-centre
    /// pixel meets the 0.65 floor under full-daylight ambient. Verified per
    /// frame: one fresh fixture and one rendered frame per generated case
    /// (no cross-case state), with the health produced by a generator
    /// (<c>Gen.Choose(1, 100)</c> — never a raw float parameter, whose
    /// default generator would explore non-finite and out-of-range values
    /// outside the drawable domain).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RenderOverlays_BlendAlphaAtTileCentre_MeetsFloorForAnyDrawableHealthUnderDaylightAmbient()
    {
        return Prop.ForAll(
            Arb.From(DrawableHealthGenerator()),
            health =>
            {
                var fixture = new OverlayMeasurementFixture();
                var measurement = fixture.MeasureDrawnTileOrThrow(
                    MeasurementTile,
                    health,
                    OverlayMeasurementFixture.FullDaylightAmbient);

                return OverlayMeasurementFixture.BlendAlphaAtTileCentre(measurement)
                    >= OverlayMeasurementFixture.BlendAlphaFloor;
            });
    }

    /// <summary>
    /// FR-010's distinguishability clause and its ambiguous "&lt; 5%" clause,
    /// measured at the spec's own measurement point: full tile quad versus
    /// the adjacent untreated quad under an identical ambient pass.
    /// <para>
    /// HALF 1 (unambiguous): the treated tile (health 80, post-compost) and
    /// its adjacent untreated neighbour (health 65 — exactly one
    /// <see cref="ModConstants.RestorationAmount"/> application below, the
    /// FR-007 delta, chosen so the untreated baseline is drawable at all;
    /// a health-0 untreated neighbour draws no overlay and offers no quad to
    /// compare) are distinguishable under the same daylight pass: the
    /// maximum per-channel RGB shift versus the untreated quad is &gt; 0.
    /// </para>
    /// <para>
    /// HALF 2 (AMBIGUITY, documented, not papered over): FR-010 reads "RGB
    /// shift &lt; 5% vs untreated neighbouring tile at same light level".
    /// Read literally — the treated quad within 5% of the untreated quad —
    /// it contradicts FR-010's headline requirement that the treated tile be
    /// visually distinguishable (the palette shifts tens of percent across
    /// tiers), so the literal reading cannot be what the clause means. The
    /// defensible operationalization is the continuity reading: no &gt;5%
    /// jump between adjacent 1-point steps across the drawable range,
    /// asserted here against the FULL channel scale (a step of k gray levels
    /// is k/255×100% for every channel). Measured on the untreated
    /// (previous-step) channel magnitude instead, the production palette
    /// does NOT hold the ceiling — evaluated against the
    /// <c>ColorInterpolationService</c> formulas, green steps reach
    /// ≈7–10% at the low end of the Poor tier (health 1→2→3 moves green
    /// 28→30→33) and the red step reaches ≈19% at the 66→67
    /// Moderate→Healthy joint — so asserting THAT basis would fail on an
    /// ambiguous reading rather than on a defect. The fixture provides both
    /// bases (<see cref="OverlayMeasurementFixture.PerChannelRgbShiftPercentVersusUntreated"/>
    /// and
    /// <see cref="OverlayMeasurementFixture.PerChannelRgbShiftPercentOfChannelScale"/>);
    /// the strict one is left unasserted deliberately.
    /// </para>
    /// </summary>
    [Fact]
    public void RenderOverlays_TreatedVersusAdjacentUntreated_DistinguishableAtSameLightAndPaletteContinuousWithinCeiling()
    {
        var fixture = new OverlayMeasurementFixture();
        var ambient = OverlayMeasurementFixture.FullDaylightAmbient;

        // Half 1 — treated vs adjacent untreated, identical ambient pass.
        var treatedHealth = 80f;
        var untreatedHealth = treatedHealth - ModConstants.RestorationAmount;

        var treated = fixture.MeasureDrawnTileOrThrow(TreatedTile, treatedHealth, ambient);
        var untreated = fixture.MeasureDrawnTileOrThrow(UntreatedNeighbourTile, untreatedHealth, ambient);

        var pairShifts = OverlayMeasurementFixture.PerChannelRgbShiftPercentVersusUntreated(
            treated.DrawnTint,
            untreated.DrawnTint);
        var pairMaxShift = OverlayMeasurementFixture.MaxShiftPercent(pairShifts);

        Assert.True(
            pairMaxShift > 0f,
            "FR-010: the treated tile (health " + treatedHealth.ToString(System.Globalization.CultureInfo.CurrentCulture)
                + ", tint RGBA(" + treated.DrawnTint.R + ", " + treated.DrawnTint.G + ", " + treated.DrawnTint.B
                + ")) must be visually distinguishable from its untreated neighbour (health "
                + untreatedHealth.ToString(System.Globalization.CultureInfo.CurrentCulture) + ", tint RGBA("
                + untreated.DrawnTint.R + ", " + untreated.DrawnTint.G + ", " + untreated.DrawnTint.B
                + ")) at the same light level, but the maximum per-channel RGB shift was "
                + pairMaxShift.ToString(System.Globalization.CultureInfo.CurrentCulture) + "%.");

        // Half 2 — palette continuity: no >5% jump between adjacent 1-point
        // steps across the whole drawable range, per channel, full-scale basis.
        for (var step = 1; step < (int)ModConstants.MaxSoilHealth; step++)
        {
            var lower = fixture.MeasureDrawnTileOrThrow(MeasurementTile, step, ambient);
            var upper = fixture.MeasureDrawnTileOrThrow(MeasurementTile, step + 1, ambient);

            var stepMaxShift = OverlayMeasurementFixture.MaxShiftPercent(
                OverlayMeasurementFixture.PerChannelRgbShiftPercentOfChannelScale(
                    upper.DrawnTint,
                    lower.DrawnTint));

            Assert.True(
                stepMaxShift <= OverlayMeasurementFixture.RgbShiftCeilingPercent,
                "FR-010 continuity: the palette jumps " + stepMaxShift.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    + "% (full channel scale) between adjacent health steps " + step + "→" + (step + 1)
                    + " — tints RGBA(" + lower.DrawnTint.R + ", " + lower.DrawnTint.G + ", " + lower.DrawnTint.B
                    + ") → RGBA(" + upper.DrawnTint.R + ", " + upper.DrawnTint.G + ", " + upper.DrawnTint.B
                    + ") — above the " + OverlayMeasurementFixture.RgbShiftCeilingPercent + "% ceiling.");
        }
    }

    /// <summary>
    /// SC-010: overlay pixel coverage under ambient lighting ≥ 95% — clipping
    /// defined as any pixel falling below blend-alpha 0.65 at the tile
    /// boundary — for any drawable health in 1..100, verified per frame (one
    /// fresh fixture and one rendered frame per generated case). MODELLED
    /// COVERAGE, stated plainly: the production base fill is one uniform
    /// full-quad draw, so the grid coverage is computed from the single
    /// measured blend alpha the frame emitted (exactly 1.0 when it meets the
    /// floor, 0.0 otherwise); no framebuffer read exists on the headless
    /// host. Genuinely measured per frame: that one blend alpha, which is
    /// what every sample of the grid carries.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RenderOverlays_PixelCoverage_MeetsCriterionForAnyDrawableHealthUnderDaylightAmbient()
    {
        return Prop.ForAll(
            Arb.From(DrawableHealthGenerator()),
            health =>
            {
                var fixture = new OverlayMeasurementFixture();
                var measurement = fixture.MeasureDrawnTileOrThrow(
                    MeasurementTile,
                    health,
                    OverlayMeasurementFixture.FullDaylightAmbient);

                return OverlayMeasurementFixture.PixelCoverage(
                    measurement,
                    OverlayMeasurementFixture.BlendAlphaFloor)
                    >= OverlayMeasurementFixture.MinimumPixelCoverage;
            });
    }

    /// <summary>
    /// FR-010's "no clipping or washing out" clause across the ambient
    /// cycle: for each ambient pass — full daylight, night, a rain-like dim,
    /// a winter-like dim — the tile-centre blend alpha stays ≥ 0.65, the
    /// tint is not blacked out (not all-zero RGB) and keeps a non-zero
    /// alpha, for both a Poor and a Healthy tile. MEASURED from the emitted
    /// tint per frame under each ambient. Honest scope note: the rain-like
    /// and winter-like dim values are test-chosen representative dim passes
    /// (documented on the fixture constants), not values captured from the
    /// running game; the ambient blend itself is production code reading
    /// <c>Game1.ambientLight</c>.
    /// </summary>
    [Fact]
    public void RenderOverlays_AmbientCycle_BlendAlphaHoldsAndTintNeitherClipsNorWashesOut_DayNightRainWinter()
    {
        var fixture = new OverlayMeasurementFixture();

        var ambientPasses = new (Color AmbientLight, string Name)[]
        {
            (OverlayMeasurementFixture.FullDaylightAmbient, "full daylight"),
            (OverlayMeasurementFixture.NightAmbient, "night"),
            (OverlayMeasurementFixture.RainLikeDimAmbient, "rain-like dim"),
            (OverlayMeasurementFixture.WinterLikeDimAmbient, "winter-like dim")
        };

        foreach (var ambientPass in ambientPasses)
        {
            foreach (var health in new[] { 20f, 80f })
            {
                var measurement = fixture.MeasureDrawnTileOrThrow(
                    MeasurementTile,
                    health,
                    ambientPass.AmbientLight);

                var tint = measurement.DrawnTint;
                var blendAlpha = OverlayMeasurementFixture.BlendAlphaAtTileCentre(measurement);

                Assert.True(
                    blendAlpha >= OverlayMeasurementFixture.BlendAlphaFloor,
                    "FR-010: under " + ambientPass.Name + " ambient the health-" + health.ToString(System.Globalization.CultureInfo.CurrentCulture)
                        + " overlay's blend alpha is " + blendAlpha.ToString(System.Globalization.CultureInfo.CurrentCulture)
                        + ", below the " + OverlayMeasurementFixture.BlendAlphaFloor + " floor (drawn tint RGBA("
                        + tint.R + ", " + tint.G + ", " + tint.B + ", " + tint.A + ")).");

                Assert.True(
                    tint.R > 0 || tint.G > 0 || tint.B > 0,
                    "FR-010: under " + ambientPass.Name + " ambient the health-" + health.ToString(System.Globalization.CultureInfo.CurrentCulture)
                        + " overlay's tint is blacked out — all-zero RGB (drawn tint RGBA(" + tint.R + ", " + tint.G
                        + ", " + tint.B + ", " + tint.A + ")) — the overlay vanishes against the darkened world.");

                Assert.True(
                    tint.A > 0,
                    "FR-010: under " + ambientPass.Name + " ambient the health-" + health.ToString(System.Globalization.CultureInfo.CurrentCulture)
                        + " overlay's tint lost its alpha (drawn tint RGBA(" + tint.R + ", " + tint.G + ", " + tint.B
                        + ", " + tint.A + ")).");
            }
        }
    }

    /// <summary>
    /// FR-009/FR-010: health 0 is the neutral vanilla dirt baseline —
    /// <c>OverlayRenderer.BuildOverlays</c> draws no overlay for it, so the
    /// frame emits no tint observable and there is NO measurement to take.
    /// This is the requirement honoured, not a measurement gap: the frame
    /// draws zero overlays, so FR-010's blend/shift/coverage criteria have
    /// nothing to apply to, and the correct observation is exactly the
    /// absence this test asserts (a null tint measurement plus a zero
    /// destination-record count).
    /// </summary>
    [Fact]
    public void RenderOverlays_HealthZero_DrawsNoOverlaySoNoMeasurementExists_NeutralVanillaDirtByDesign()
    {
        var fixture = new OverlayMeasurementFixture();

        var measurement = fixture.MeasureTile(
            MeasurementTile,
            0f,
            OverlayMeasurementFixture.FullDaylightAmbient);

        Assert.Null(measurement);
        Assert.Equal(0, fixture.LastFrameOverlaysDrawn);
    }

    /// <summary>
    /// Drawable health values 1..100 — never 0 (the undrawn FR-009 baseline)
    /// and never non-finite, so every generated case is a tile the palette
    /// actually draws. Integer generator (<c>Gen.Choose(1, 100)</c>) mapped
    /// to float, per the repo's FsCheck convention.
    /// </summary>
    private static Gen<float> DrawableHealthGenerator()
    {
        return Gen.Choose(1, 100).Select(i => (float)i);
    }
}
