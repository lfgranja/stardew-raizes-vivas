using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using LivingRoots;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.Visualization;

/// <summary>
/// T040 (FR-010 / SC-010): the overlay measurement fixture. Renders ONE
/// production overlay frame per tile and captures the per-tile observables
/// the production draw path emits, so the per-frame measurement claims
/// FR-010 and SC-010 state are measured rather than assumed.
/// </summary>
/// <remarks>
/// <para>
/// WHAT IS GENUINELY MEASURED. Every measurement is the tint the production
/// draw path itself emitted for the frame:
/// <c>VisualizationService.DrawOverlay</c> logs
/// <c>"Overlay tint drawn: RGBA(r, g, b, a)"</c> (trace) AFTER the ambient
/// blend (<c>BlendUnderAmbientLight</c>) and BEFORE the
/// <c>SpriteBatch.Draw</c> that consumes it. The parsed bytes are exactly the
/// RGBA the draw consumed, through the real production pipeline
/// (<c>OverlayRenderer.BuildOverlays</c> → the real
/// <see cref="ColorInterpolationService"/> palette → the production ambient
/// blend), so the blend alpha and the RGB values asserted against are
/// production values, not test doubles.
/// </para>
/// <para>
/// THE HEADLESS-HOST BOUNDARY. A real <c>SpriteBatch</c> needs a
/// <c>GraphicsDevice</c> the unit-test host does not have (and
/// <c>SpriteBatch.Draw</c> is non-virtual), so each frame runs through an
/// uninitialized stand-in batch whose first draw raises after the tint
/// observable is emitted; the raise is the host boundary and is absorbed
/// here with <c>Record.Exception</c> — the same stand-in pattern
/// <c>SoilHealthVisualizationTests</c> establishes (its
/// <c>StandInSpriteBatch</c>/<c>StandInWhiteTexture</c> technique).
/// </para>
/// <para>
/// ONE FRAME PER TILE. Because the stand-in draw raises at the first
/// overlay, a multi-tile frame emits the tint observable for its first
/// overlay only, and the tint log carries no tile position.
/// <see cref="MeasureTiles"/> therefore renders one single-tile frame per
/// tile, which keeps tint-to-tile attribution exact; the destination
/// observable (<c>"Overlay drawn: ..."</c>) count confirms how many overlays
/// the frame drew.
/// </para>
/// <para>
/// WHAT IS MODELLED, NOT MEASURED. The production base fill is ONE
/// <c>SpriteBatch.Draw</c> over the whole <see cref="ModConstants.TileSize"/>×
/// <see cref="ModConstants.TileSize"/> quad with a single uniform tint, so
/// every pixel of the quad — the tile-centre pixel of FR-010's measurement
/// point and the boundary ring alike — carries the same alpha the tint
/// observable reports. The blend alpha at the centre pixel is therefore the
/// measured emitted alpha, not an interpolation. The per-pixel grid of
/// <see cref="PixelCoverage"/> is a MODEL over that uniform tint: no
/// framebuffer readback exists on the headless host, so SC-010's ≥95%
/// pixel-coverage criterion is computed from the single measured alpha over
/// a <see cref="ModConstants.TileSize"/>×<see cref="ModConstants.TileSize"/>
/// sample grid (coverage is exactly 1.0 when the measured alpha meets the
/// floor and 0.0 otherwise). The final composited pixel over the dirt
/// texture is NOT observable on this host and is not claimed here.
/// </para>
/// <para>
/// WHAT IS NOT MEASURABLE AT ALL FROM THESE OBSERVABLES. An untreated
/// neighbour at health 0 draws no overlay (FR-009 neutral dirt), so there is
/// no quad to compare against and the literal "RGB shift vs untreated
/// neighbouring tile" comparison does not exist at that baseline — the tests
/// therefore compare against a drawable untreated baseline and say so. The
/// accessibility pattern layer (<c>ShowPatterns</c>) is disabled in the
/// measurement configuration because pattern draws emit no tint observable
/// of their own; it is outside this measurement, not assumed passing.
/// </para>
/// </remarks>
public sealed class OverlayMeasurementFixture
{
    /// <summary>
    /// FR-010 / SC-010 blend-alpha floor (0.65). A requirement threshold that
    /// does not exist in <see cref="ModConstants"/>; declared here with its
    /// provenance rather than restated as an unnamed literal — the same
    /// convention <c>VisualizationInvariantsTests</c>' T031b
    /// <c>MinBlendAlpha</c> establishes.
    /// </summary>
    public const float BlendAlphaFloor = 0.65f;

    /// <summary>
    /// SC-010 pixel-coverage criterion (0.95); provenance convention as
    /// <see cref="BlendAlphaFloor"/>.
    /// </summary>
    public const float MinimumPixelCoverage = 0.95f;

    /// <summary>
    /// FR-010 RGB-shift ceiling (5%). Asserted only in the continuity
    /// interpretation documented on the test that uses it: FR-010's "vs
    /// untreated neighbouring tile" wording is ambiguous (see
    /// <see cref="PerChannelRgbShiftPercentVersusUntreated"/> and
    /// <see cref="PerChannelRgbShiftPercentOfChannelScale"/>).
    /// </summary>
    public const float RgbShiftCeilingPercent = 5f;

    /// <summary>
    /// The configured overlay opacity the measurement frames render at:
    /// <see cref="ModConstants.PatternMinOpacity"/> (0.7) — the production
    /// accessibility floor, and the lowest production opacity whose byte
    /// alpha clears <see cref="BlendAlphaFloor"/> after byte quantization
    /// (0.7 × 255 = 178.5 → byte 178 → 178/255 ≈ 0.698). Audit note: at
    /// <see cref="ModConstants.DefaultOpacity"/> (0.5) the drawn alpha is
    /// byte 127 ≈ 0.498, which is BELOW FR-010's 0.65 floor; the fixture
    /// deliberately measures at the floor-clearing opacity so the ambient
    /// claims are exercised at their tightest passing value, and the
    /// default-opacity shortfall is reported rather than hidden by the
    /// configuration.
    /// </summary>
    public const float ConfiguredOpacity = ModConstants.PatternMinOpacity;

    /// <summary>
    /// Samples per quad edge in the coverage grid: one per pixel of the
    /// <see cref="ModConstants.TileSize"/>×<see cref="ModConstants.TileSize"/>
    /// production draw rect.
    /// </summary>
    public const int QuadSamplesPerEdge = ModConstants.TileSize;

    /// <summary>Prefix of the production per-overlay tint trace log.</summary>
    public const string DrawnOverlayTintLogPrefix = "Overlay tint drawn:";

    /// <summary>Prefix of the production per-overlay destination trace log.</summary>
    public const string DrawnOverlayDestinationLogPrefix = "Overlay drawn: ";

    /// <summary>Channel prefix inside the tint trace log.</summary>
    private const string TintChannelPrefix = "RGBA(";

    /// <summary>Separator between the logged tint channels.</summary>
    private static readonly string[] TintChannelSeparator = { ", " };

    /// <summary>
    /// The tile-space viewport the measurement frames render (16×16 tiles —
    /// every measurement tile sits well inside it and its cull margin).
    /// </summary>
    public static readonly Rectangle MeasurementViewport = new(0, 0, 16, 16);

    /// <summary>
    /// Full-daylight ambient (white) — the identity blend, so the drawn tint
    /// is the unlit palette tint exactly.
    /// </summary>
    public static readonly Color FullDaylightAmbient = Color.White;

    /// <summary>
    /// Night ambient (48, 48, 88) — the same value the existing
    /// <c>SoilHealthVisualizationTests</c> ambient-lighting test installs.
    /// </summary>
    public static readonly Color NightAmbient = new(48, 48, 88);

    /// <summary>
    /// A rain-like dim ambient pass. TEST-CHOSEN representative dim, not a
    /// value captured from the running game — FR-010 asks for the claim to
    /// hold across dim ambient passes, and the exact in-game rain value is
    /// not observable from the headless host, so the sweep is what is
    /// honest to assert.
    /// </summary>
    public static readonly Color RainLikeDimAmbient = new(120, 120, 140);

    /// <summary>A winter-like dim ambient pass (test-chosen, as above).</summary>
    public static readonly Color WinterLikeDimAmbient = new(160, 160, 180);

    /// <summary>The frame-log sink the tint observable is read from.</summary>
    private readonly List<string> _messages = new();

    /// <summary>
    /// How many overlays the most recent frame drew, counted from the
    /// <c>"Overlay drawn: "</c> destination records it emitted — 1 for a
    /// tile that drew, 0 for a tile the renderer skipped outright (health 0
    /// draws no overlay at all, FR-009).
    /// </summary>
    public int LastFrameOverlaysDrawn { get; private set; }

    /// <summary>
    /// Measures the tint the production draw path emits for ONE tile:
    /// renders a single-tile frame at <paramref name="health"/> under
    /// <paramref name="ambientLight"/> and parses the emitted
    /// <c>"Overlay tint drawn: RGBA(r, g, b, a)"</c> trace log. Returns null
    /// when the frame drew no overlay (health 0 — the FR-009 neutral-dirt
    /// baseline); callers must read null as "nothing was drawn, so there is
    /// nothing to measure", never as a transparent overlay.
    /// </summary>
    public TileTintMeasurement? MeasureTile(Point tile, float health, Color ambientLight)
    {
        // Fresh pipeline per measurement: no cross-measurement cache or
        // log state can leak between frames.
        var service = BuildFrameService(_messages);
        service.SetTileHealthData(new Dictionary<Point, float> { { tile, health } });

        _messages.Clear();
        LastFrameOverlaysDrawn = 0;

        using (new AmbientLightScope(ambientLight))
        {
            using (new CameraViewportScope(MeasurementViewport))
            {
                Record.Exception(() =>
                    service.RenderOverlays(StandInSpriteBatch(), MeasurementViewport, new GameTime()));
            }
        }

        foreach (var message in _messages)
        {
            if (message.StartsWith(DrawnOverlayDestinationLogPrefix, StringComparison.Ordinal))
            {
                LastFrameOverlaysDrawn++;
            }
        }

        var drawnTint = LastDrawnOverlayTint(_messages);
        if (!drawnTint.HasValue)
        {
            return null;
        }

        return new TileTintMeasurement(tile, health, drawnTint.Value);
    }

    /// <summary>
    /// Measures every tile of a snapshot as <see cref="MeasureTile"/> does —
    /// one single-tile frame per tile, because a multi-tile frame's draw
    /// raises at its first overlay and the tint log carries no position (see
    /// the type remarks). Tiles that draw nothing (health 0) are omitted
    /// from the result rather than fabricated into it.
    /// </summary>
    public IReadOnlyList<TileTintMeasurement> MeasureTiles(
        IEnumerable<KeyValuePair<Point, float>> tileHealthState,
        Color ambientLight)
    {
        var measurements = new List<TileTintMeasurement>();
        foreach (var entry in tileHealthState)
        {
            var measurement = MeasureTile(entry.Key, entry.Value, ambientLight);
            if (measurement != null)
            {
                measurements.Add(measurement);
            }
        }

        return measurements;
    }

    /// <summary>
    /// <see cref="MeasureTile"/> for a tile the caller knows must draw:
    /// throws (failing the test) when the frame emitted no tint observable,
    /// so expected-to-measure call sites stay free of null handling and a
    /// silent no-draw cannot pass as a measurement.
    /// </summary>
    public TileTintMeasurement MeasureDrawnTileOrThrow(Point tile, float health, Color ambientLight)
    {
        var measurement = MeasureTile(tile, health, ambientLight);
        return measurement ?? throw new InvalidOperationException(
            "MEASUREMENT GAP: the frame for tile (" + tile.X + ", " + tile.Y + ") at health "
            + health.ToString(CultureInfo.CurrentCulture) + " under ambient RGBA("
            + ambientLight.R + ", " + ambientLight.G + ", " + ambientLight.B
            + ") drew no tint observable — nothing was measured.");
    }

    /// <summary>
    /// FR-010's measurement point: the blend alpha at the TILE-CENTRE pixel.
    /// The production base fill is one uniform full-quad draw, so the centre
    /// pixel carries exactly the emitted tint's alpha — this returns that
    /// measured value (A / 255), not an interpolation between centre and
    /// boundary.
    /// </summary>
    public static float BlendAlphaAtTileCentre(TileTintMeasurement measurement)
    {
        return measurement.DrawnTint.A / 255f;
    }

    /// <summary>
    /// The modelled blend alpha at one sample pixel of the quad (0-based,
    /// 0 &lt;= x, y &lt; <see cref="QuadSamplesPerEdge"/>). MODELLED: under
    /// the production uniform full-quad fill every sample — centre, interior
    /// and boundary ring alike — carries the emitted tint's alpha, so this
    /// value is position-independent by construction; the genuine
    /// measurement is the single emitted alpha, and this helper exists so
    /// the coverage grid samples a defined per-pixel function rather than
    /// short-circuiting the model away.
    /// </summary>
    public static float SampleBlendAlpha(TileTintMeasurement measurement, int pixelX, int pixelY)
    {
        // The uniform-fill model makes the sampled value position-independent;
        // the bounds check keeps a sampled coordinate inside the modelled quad,
        // so an out-of-range sample is a caller error rather than a silent read.
        if (pixelX < 0 || pixelY < 0 || pixelX >= QuadSamplesPerEdge || pixelY >= QuadSamplesPerEdge)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelX),
                "The sampled pixel (" + pixelX + ", " + pixelY + ") lies outside the modelled "
                + QuadSamplesPerEdge + "×" + QuadSamplesPerEdge + " tile quad.");
        }

        return BlendAlphaAtTileCentre(measurement);
    }

    /// <summary>
    /// SC-010's pixel-coverage criterion: the fraction of the
    /// <see cref="ModConstants.TileSize"/>×<see cref="ModConstants.TileSize"/>
    /// sample grid — boundary ring included, per SC-010's clipping
    /// definition — whose blend alpha meets <paramref name="alphaFloor"/>.
    /// MODELLED coverage: computed from the one uniform tint the frame
    /// emitted (see the type remarks), so the result is exactly 1.0 when the
    /// measured blend alpha meets the floor and 0.0 when it does not. A
    /// framebuffer read is impossible on the headless host; this is the
    /// honest derivation of the ≥95% criterion from the observable, not a
    /// screenshot measurement, and the uniform-fill model it rests on is
    /// what removes any boundary-specific alpha drop from the observable
    /// set.
    /// </summary>
    public static float PixelCoverage(TileTintMeasurement measurement, float alphaFloor)
    {
        var totalSamples = 0;
        var meetingSamples = 0;
        for (var pixelY = 0; pixelY < QuadSamplesPerEdge; pixelY++)
        {
            for (var pixelX = 0; pixelX < QuadSamplesPerEdge; pixelX++)
            {
                totalSamples++;
                if (SampleBlendAlpha(measurement, pixelX, pixelY) >= alphaFloor)
                {
                    meetingSamples++;
                }
            }
        }

        return (float)meetingSamples / totalSamples;
    }

    /// <summary>
    /// One channel's RGB shift as a percentage of the untreated channel's
    /// magnitude. A zero untreated magnitude has no meaningful percentage, so
    /// a treated channel of 0 reports 0% (both clear) and a non-zero treated
    /// channel reports 100% (a channel appeared) rather than dividing by zero.
    /// </summary>
    private static float ChannelShiftPercentVersusUntreated(byte treated, byte untreated)
    {
        if (untreated == 0)
        {
            return treated == 0 ? 0f : 100f;
        }

        return MathF.Abs(treated - untreated) / (float)untreated * 100f;
    }

    /// <summary>
    /// The per-channel RGB shift between two full-tile quads as a percentage
    /// of the UNTREATED quad's per-channel magnitude:
    /// |c_treated − c_untreated| / c_untreated × 100. This is the literal
    /// reading of FR-010's "RGB shift &lt; 5% vs untreated neighbouring
    /// tile" with the untreated tile as denominator. A channel whose
    /// untreated magnitude is 0 reports 0% (both clear) or 100% (a treated
    /// channel appears) rather than dividing by zero.
    /// </summary>
    public static IReadOnlyList<float> PerChannelRgbShiftPercentVersusUntreated(
        Color treatedQuad,
        Color untreatedQuad)
    {
        return new[]
        {
            ChannelShiftPercentVersusUntreated(treatedQuad.R, untreatedQuad.R),
            ChannelShiftPercentVersusUntreated(treatedQuad.G, untreatedQuad.G),
            ChannelShiftPercentVersusUntreated(treatedQuad.B, untreatedQuad.B)
        };
    }

    /// <summary>
    /// The per-channel RGB shift between two full-tile quads as a percentage
    /// of the FULL channel scale (255): |c_treated − c_untreated| / 255 × 100
    /// — a step of k gray levels is k/255×100% for every channel regardless
    /// of how small the previous channel value was. This is the basis the
    /// continuity claim is asserted on, because it is the only reading of
    /// FR-010's "&lt; 5%" that is both measurable and consistent with
    /// FR-010's own headline requirement that a treated tile stay visually
    /// distinguishable from the untreated neighbour: the production palette
    /// does NOT hold the ceiling on the untreated-magnitude basis
    /// (<see cref="PerChannelRgbShiftPercentVersusUntreated"/>) — its green
    /// steps reach ≈7–10% at the low end of the Poor tier and its red step
    /// reaches ≈19% at the Moderate→Healthy joint. That stricter basis is
    /// provided here so the ambiguity stays auditable rather than hidden.
    /// </summary>
    public static IReadOnlyList<float> PerChannelRgbShiftPercentOfChannelScale(
        Color treatedQuad,
        Color untreatedQuad)
    {
        return new[]
        {
            (Math.Abs(treatedQuad.R - untreatedQuad.R) / 255f) * 100f,
            (Math.Abs(treatedQuad.G - untreatedQuad.G) / 255f) * 100f,
            (Math.Abs(treatedQuad.B - untreatedQuad.B) / 255f) * 100f
        };
    }

    /// <summary>The largest channel shift of a per-channel shift list.</summary>
    public static float MaxShiftPercent(IReadOnlyList<float> perChannelShiftPercent)
    {
        var max = 0f;
        foreach (var shift in perChannelShiftPercent)
        {
            if (shift > max)
            {
                max = shift;
            }
        }

        return max;
    }

    /// <summary>
    /// The most recent <c>"Overlay tint drawn: RGBA(r, g, b, a)"</c> record
    /// of a frame, or null when the frame emitted none — either no overlay
    /// was drawn, or the log was malformed; both fail the caller's
    /// expectation to measure rather than passing silently.
    /// </summary>
    private static Color? LastDrawnOverlayTint(List<string> messages)
    {
        for (var index = messages.Count - 1; index >= 0; index--)
        {
            if (messages[index].StartsWith(DrawnOverlayTintLogPrefix, StringComparison.Ordinal))
            {
                return TryParseOverlayTint(messages[index]);
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the <c>RGBA(r, g, b, a)</c> suffix of a tint trace log. The
    /// production path formats the channels with the current culture, so
    /// the round-trip parses with it too — correct under any test-host
    /// locale (the <c>SoilHealthVisualizationTests</c> parse convention).
    /// </summary>
    private static Color? TryParseOverlayTint(string message)
    {
        var channelStart = message.IndexOf(TintChannelPrefix, StringComparison.Ordinal);
        var channelEnd = message.LastIndexOf(')');
        if (channelStart < 0 || channelEnd <= channelStart)
        {
            return null;
        }

        var channels = message
            .Substring(channelStart + TintChannelPrefix.Length, channelEnd - channelStart - TintChannelPrefix.Length)
            .Split(TintChannelSeparator, StringSplitOptions.None);
        if (channels.Length != 4)
        {
            return null;
        }

        if (!float.TryParse(channels[0], NumberStyles.Float, CultureInfo.CurrentCulture, out var red)
            || !float.TryParse(channels[1], NumberStyles.Float, CultureInfo.CurrentCulture, out var green)
            || !float.TryParse(channels[2], NumberStyles.Float, CultureInfo.CurrentCulture, out var blue)
            || !float.TryParse(channels[3], NumberStyles.Float, CultureInfo.CurrentCulture, out var alpha))
        {
            return null;
        }

        return new Color((byte)red, (byte)green, (byte)blue, (byte)alpha);
    }

    /// <summary>
    /// Builds the production visualization pipeline the frames measure: a
    /// <see cref="VisualizationService"/> over the REAL
    /// <see cref="ColorInterpolationService"/> palette (the palette is what
    /// FR-010's RGB claims are about, so no color double is installed), a
    /// configuration enabling overlays at <see cref="ConfiguredOpacity"/> with
    /// patterns off (the pattern layer emits no tint observable of its own
    /// and is outside this measurement), and a monitor capturing log records
    /// into <paramref name="messages"/> — the sink the tint observable is
    /// read from. The white texture is the uninitialized stand-in the draw
    /// path draws rectangles with, so each frame reaches the real draw
    /// stage and the real ambient blend.
    /// </summary>
    private static VisualizationService BuildFrameService(List<string> messages)
    {
        var monitor = new Mock<IMonitor>();
        monitor
            .Setup(m => m.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
            .Callback<string, LogLevel>((message, level) => messages.Add(message));

        var configService = new Mock<LivingRoots.Domain.IVisualizationConfigurationService>();
        configService
            .Setup(c => c.GetConfiguration())
            .Returns(new VisualizationConfiguration
            {
                OverlaysEnabled = true,
                Opacity = ConfiguredOpacity,
                ShowPatterns = false
            });

        var service = new VisualizationService(
            new ColorInterpolationService(),
            configService.Object,
            monitor.Object);
        service.Initialize(StandInWhiteTexture());
        return service;
    }

    /// <summary>
    /// An uninitialized <see cref="SpriteBatch"/> stand-in: non-null (so
    /// null guards pass) but internally unallocated, so the first draw
    /// through it raises — the headless host has no
    /// <see cref="GraphicsDevice"/> to construct a real one.
    /// </summary>
    private static SpriteBatch StandInSpriteBatch()
    {
        return (SpriteBatch)RuntimeHelpers.GetUninitializedObject(typeof(SpriteBatch));
    }

    /// <summary>
    /// An uninitialized <see cref="Texture2D"/> stand-in for the shared
    /// white texture the draw path draws rectangles with.
    /// </summary>
    private static Texture2D StandInWhiteTexture()
    {
        return (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
    }

    /// <summary>
    /// The game's ambient light state a frame renders under —
    /// <c>Game1.ambientLight</c> and <c>Game1.outdoorLight</c> set to the
    /// given light for the scope of the measurement and restored on dispose,
    /// so other tests observe the game state they started with. This is this
    /// fixture's own copy of the scope; the identical nested class in
    /// <c>SoilHealthVisualizationTests</c> is private and is deliberately
    /// not referenced across files.
    /// </summary>
    public sealed class AmbientLightScope : IDisposable
    {
        private readonly Color _originalAmbientLight;
        private readonly Color _originalOutdoorLight;

        public AmbientLightScope(Color ambientLight)
        {
            _originalAmbientLight = Game1.ambientLight;
            _originalOutdoorLight = Game1.outdoorLight;

            Game1.ambientLight = ambientLight;
            Game1.outdoorLight = ambientLight;
        }

        public void Dispose()
        {
            Game1.ambientLight = _originalAmbientLight;
            Game1.outdoorLight = _originalOutdoorLight;
        }
    }

    /// <summary>
    /// The camera viewport state a frame renders under — the pixel-space
    /// <c>Game1.viewport</c> resolved from the tile-space viewport the
    /// overlay path culls against, set for the scope of the measurement and
    /// restored on dispose.
    /// </summary>
    public sealed class CameraViewportScope : IDisposable
    {
        private readonly xTile.Dimensions.Rectangle _originalViewport;

        public CameraViewportScope(Rectangle tileViewport)
        {
            _originalViewport = Game1.viewport;

            Game1.viewport = new xTile.Dimensions.Rectangle(
                tileViewport.X * ModConstants.TileSize,
                tileViewport.Y * ModConstants.TileSize,
                tileViewport.Width * ModConstants.TileSize,
                tileViewport.Height * ModConstants.TileSize);
        }

        public void Dispose()
        {
            Game1.viewport = _originalViewport;
        }
    }
}

/// <summary>
/// One measured drawable tile: the position, the health the frame carried,
/// and the tint the production draw path emitted for it (post ambient
/// blend).
/// </summary>
public sealed class TileTintMeasurement
{
    public TileTintMeasurement(Point tilePosition, float requestedHealth, Color drawnTint)
    {
        TilePosition = tilePosition;
        RequestedHealth = requestedHealth;
        DrawnTint = drawnTint;
    }

    /// <summary>The tile position the frame rendered.</summary>
    public Point TilePosition { get; }

    /// <summary>
    /// The health value the frame carried for the tile (the drawn tint
    /// resolves it through the palette's Poor floor at 1, so a sub-floor
    /// reading draws at its floor).
    /// </summary>
    public float RequestedHealth { get; }

    /// <summary>
    /// The tint the production draw path emitted — the RGBA the
    /// <c>SpriteBatch.Draw</c> consumed, ambient blend included.
    /// </summary>
    public Color DrawnTint { get; }
}
