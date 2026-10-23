using System;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for visualization configuration validation
/// (T115, FR-006). For any opacity float, any color bytes and any
/// accessibility-degradation mode string, an invalid entry falls back to the
/// ModConstants default while the valid fields of the same configuration are
/// preserved, and a missing persisted file yields the complete defaults.
/// </summary>
public class VisualizationConfigInvariantsTests
{
    private const float OpacityTolerance = 0.0001f;
    private const string SaveId = "invariants-save";

    // Documented contract of VisualizationConfiguration.AccessibilityDegradation:
    // one of "auto", "never" or "notify", matched case-insensitively (FR-006).
    private const string AutoMode = "auto";
    private const string NeverMode = "never";
    private const string NotifyMode = "notify";

    // The canonical default degradation mode, read from the configuration
    // type itself so the invariant never hardcodes the fallback value.
    private static readonly string DefaultAccessibilityDegradation =
        new VisualizationConfiguration().AccessibilityDegradation!;

    /// <summary>Degradation modes accepted by the configuration contract.</summary>
    public enum DegradationModeValue
    {
        Auto,
        Never,
        Notify
    }

    private static VisualizationConfigurationService CreateService(out Mock<IModDataService> mockDataService)
    {
        mockDataService = new Mock<IModDataService>();
        var mockMonitor = new Mock<IMonitor>();
        var mockSanitizer = new Mock<IFileNameSanitizationService>();
        mockSanitizer.Setup(s => s.Sanitize(It.IsAny<string>()))
            .Returns<string?>(id => id);

        return new VisualizationConfigurationService(
            mockDataService.Object,
            mockMonitor.Object,
            mockSanitizer.Object);
    }

    /// <summary>
    /// Builds a configuration whose every field is valid and deliberately
    /// non-default, so "valid fields preserved" is a meaningful assertion.
    /// </summary>
    private static VisualizationConfiguration CreateValidConfig()
    {
        return new VisualizationConfiguration
        {
            OverlaysEnabled = !ModConstants.OverlaysEnabledDefault,
            TooltipsEnabled = !ModConstants.TooltipsEnabledDefault,
            HoeFeedbackEnabled = !ModConstants.HoeFeedbackEnabledDefault,
            ShowPatterns = false,
            Opacity = ModConstants.DefaultOpacity,
            PoorColor = new ColorDTO(10, 20, 30, 255),
            ModerateColor = new ColorDTO(40, 50, 60, 255),
            HealthyColor = new ColorDTO(70, 80, 90, 255),
            UnknownColor = new ColorDTO(100, 110, 120, 255),
            AccessibilityDegradation = AutoMode
        };
    }

    /// <summary>
    /// Mirrors the production opacity contract: valid within 0.0-1.0, with
    /// NaN (and therefore infinities, which fail the range check) invalid.
    /// </summary>
    private static bool IsValidOpacity(float opacity)
    {
        return !float.IsNaN(opacity) && opacity >= 0f && opacity <= 1f;
    }

    /// <summary>
    /// Mirrors the production degradation-mode contract: non-blank and equal
    /// (case-insensitively) to one of the three accepted modes.
    /// </summary>
    private static bool IsValidDegradationMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return false;
        }

        return string.Equals(mode, AutoMode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(mode, NeverMode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(mode, NotifyMode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Checks whether a configuration equals the complete defaults.</summary>
    private static bool IsCompleteDefaults(VisualizationConfiguration configuration)
    {
        var defaults = new VisualizationConfiguration();
        return configuration.OverlaysEnabled == defaults.OverlaysEnabled
               && configuration.TooltipsEnabled == defaults.TooltipsEnabled
               && configuration.HoeFeedbackEnabled == defaults.HoeFeedbackEnabled
               && configuration.ShowPatterns == defaults.ShowPatterns
               && Math.Abs(configuration.Opacity - defaults.Opacity) < OpacityTolerance
               && configuration.PoorColor == defaults.PoorColor
               && configuration.ModerateColor == defaults.ModerateColor
               && configuration.HealthyColor == defaults.HealthyColor
               && configuration.UnknownColor == defaults.UnknownColor
               && configuration.AccessibilityDegradation == defaults.AccessibilityDegradation;
    }

    /// <summary>
    /// Invariant: for any opacity float, an opacity outside the valid 0.0-1.0
    /// range falls back to <see cref="ModConstants.DefaultOpacity"/> while
    /// every valid field of the same configuration is preserved unchanged
    /// (FR-006).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UpdateConfiguration_ForAnyOpacity_InvalidFallsBackToDefaultWhileValidFieldsPreserved(float opacity)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.Opacity = opacity;

        service.UpdateConfiguration(configuration);
        var result = service.GetConfiguration();

        var expectedOpacity = IsValidOpacity(opacity) ? opacity : ModConstants.DefaultOpacity;
        return Math.Abs(result.Opacity - expectedOpacity) < OpacityTolerance
               && result.OverlaysEnabled == configuration.OverlaysEnabled
               && result.TooltipsEnabled == configuration.TooltipsEnabled
               && result.HoeFeedbackEnabled == configuration.HoeFeedbackEnabled
               && result.ShowPatterns == configuration.ShowPatterns
               && result.PoorColor == configuration.PoorColor
               && result.ModerateColor == configuration.ModerateColor
               && result.HealthyColor == configuration.HealthyColor
               && result.UnknownColor == configuration.UnknownColor
               && result.AccessibilityDegradation == configuration.AccessibilityDegradation;
    }

    /// <summary>
    /// Invariant: for any color bytes, a color with a zero alpha channel (the
    /// invisible "unset" state — the only invalid color state, since ColorDTO
    /// channels are bytes and cannot leave the per-channel range) falls back
    /// to the ModConstants default color while every other field of the same
    /// configuration is preserved unchanged (FR-006).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UpdateConfiguration_ForAnyColorBytes_ZeroAlphaFallsBackToDefaultWhileValidFieldsPreserved(
        byte r,
        byte g,
        byte b,
        byte a)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.PoorColor = new ColorDTO(r, g, b, a);

        service.UpdateConfiguration(configuration);
        var result = service.GetConfiguration();

        var expectedPoorColor = a == 0
            ? new ColorDTO(ModConstants.PoorColor)
            : new ColorDTO(r, g, b, a);
        return result.PoorColor == expectedPoorColor
               && result.OverlaysEnabled == configuration.OverlaysEnabled
               && result.TooltipsEnabled == configuration.TooltipsEnabled
               && result.HoeFeedbackEnabled == configuration.HoeFeedbackEnabled
               && result.ShowPatterns == configuration.ShowPatterns
               && result.Opacity == configuration.Opacity
               && result.ModerateColor == configuration.ModerateColor
               && result.HealthyColor == configuration.HealthyColor
               && result.UnknownColor == configuration.UnknownColor
               && result.AccessibilityDegradation == configuration.AccessibilityDegradation;
    }

    /// <summary>
    /// Invariant: for any degradation mode string, a mode outside
    /// {auto, never, notify} (matched case-insensitively) falls back to the
    /// default mode while every valid field of the same configuration is
    /// preserved unchanged (FR-006).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UpdateConfiguration_ForAnyDegradationMode_UnknownFallsBackToDefaultWhileValidFieldsPreserved(string mode)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.AccessibilityDegradation = mode;

        service.UpdateConfiguration(configuration);
        var result = service.GetConfiguration();

        var expectedMode = IsValidDegradationMode(mode) ? mode : DefaultAccessibilityDegradation;
        return result.AccessibilityDegradation == expectedMode
               && result.OverlaysEnabled == configuration.OverlaysEnabled
               && result.TooltipsEnabled == configuration.TooltipsEnabled
               && result.HoeFeedbackEnabled == configuration.HoeFeedbackEnabled
               && result.ShowPatterns == configuration.ShowPatterns
               && result.Opacity == configuration.Opacity
               && result.PoorColor == configuration.PoorColor
               && result.ModerateColor == configuration.ModerateColor
               && result.HealthyColor == configuration.HealthyColor
               && result.UnknownColor == configuration.UnknownColor;
    }

    /// <summary>
    /// Invariant: the degradation-mode match is case-insensitive (FR-006): a
    /// known mode supplied in any casing is accepted and preserved exactly as
    /// supplied, not replaced by the lowercase default.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UpdateConfiguration_KnownDegradationModeInAnyCasing_IsPreserved(
        DegradationModeValue mode,
        bool toUpper)
    {
        var value = mode switch
        {
            DegradationModeValue.Auto => AutoMode,
            DegradationModeValue.Never => NeverMode,
            _ => NotifyMode
        };
        var cased = toUpper ? value.ToUpperInvariant() : value.ToLowerInvariant();

        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.AccessibilityDegradation = cased;

        service.UpdateConfiguration(configuration);

        return service.GetConfiguration().AccessibilityDegradation == cased;
    }

    /// <summary>
    /// Invariant: loading an arbitrary persisted entry applies the same
    /// per-field validation as updating: invalid fields fall back to the
    /// ModConstants defaults while the valid fields of the persisted entry are
    /// preserved (FR-006).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LoadConfiguration_ForAnyPersistedEntry_InvalidFieldsFallBackWhileValidFieldsPreserved(
        float opacity,
        byte r,
        byte g,
        byte b,
        byte a,
        string mode)
    {
        var persisted = CreateValidConfig();
        persisted.Opacity = opacity;
        persisted.PoorColor = new ColorDTO(r, g, b, a);
        persisted.AccessibilityDegradation = mode;

        var service = CreateService(out var mockDataService);
        mockDataService.Setup(m => m.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
            .Returns(persisted);

        service.LoadConfiguration(SaveId);
        var result = service.GetConfiguration();

        var expectedOpacity = IsValidOpacity(opacity) ? opacity : ModConstants.DefaultOpacity;
        var expectedPoorColor = a == 0
            ? new ColorDTO(ModConstants.PoorColor)
            : new ColorDTO(r, g, b, a);
        var expectedMode = IsValidDegradationMode(mode) ? mode : DefaultAccessibilityDegradation;

        return Math.Abs(result.Opacity - expectedOpacity) < OpacityTolerance
               && result.PoorColor == expectedPoorColor
               && result.AccessibilityDegradation == expectedMode
               && result.OverlaysEnabled == persisted.OverlaysEnabled
               && result.TooltipsEnabled == persisted.TooltipsEnabled
               && result.HoeFeedbackEnabled == persisted.HoeFeedbackEnabled
               && result.ShowPatterns == persisted.ShowPatterns
               && result.ModerateColor == persisted.ModerateColor
               && result.HealthyColor == persisted.HealthyColor
               && result.UnknownColor == persisted.UnknownColor;
    }

    /// <summary>
    /// Invariant: a missing persisted file (no entry under the save's data
    /// key) yields the complete default configuration — every field at its
    /// default, none left unset — for any save identifier (FR-006).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LoadConfiguration_MissingPersistedEntry_YieldsCompleteDefaults(string saveId)
    {
        var service = CreateService(out var mockDataService);
        mockDataService.Setup(m => m.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
            .Returns((VisualizationConfiguration?)null);

        service.LoadConfiguration(saveId);

        return IsCompleteDefaults(service.GetConfiguration());
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(-1f)]
    [InlineData(-0.001f)]
    [InlineData(1.001f)]
    [InlineData(2f)]
    public void UpdateConfiguration_OpacityOutsideValidRange_FallsBackToDefault(float opacity)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.Opacity = opacity;

        service.UpdateConfiguration(configuration);

        Assert.Equal(ModConstants.DefaultOpacity, service.GetConfiguration().Opacity);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void UpdateConfiguration_OpacityInsideValidRange_IsPreserved(float opacity)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.Opacity = opacity;

        service.UpdateConfiguration(configuration);

        Assert.Equal(opacity, service.GetConfiguration().Opacity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sometimes")]
    [InlineData("auto ")]
    [InlineData("AUTO!")]
    public void UpdateConfiguration_UnknownDegradationMode_FallsBackToDefault(string? mode)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.AccessibilityDegradation = mode;

        service.UpdateConfiguration(configuration);

        Assert.Equal(DefaultAccessibilityDegradation, service.GetConfiguration().AccessibilityDegradation);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("Auto")]
    [InlineData("never")]
    [InlineData("NEVER")]
    [InlineData("notify")]
    [InlineData("NoTiFy")]
    public void UpdateConfiguration_KnownDegradationModeAnyCasing_IsPreserved(string mode)
    {
        var service = CreateService(out _);
        var configuration = CreateValidConfig();
        configuration.AccessibilityDegradation = mode;

        service.UpdateConfiguration(configuration);

        Assert.Equal(mode, service.GetConfiguration().AccessibilityDegradation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("save-with-dashes")]
    [InlineData("save with spaces")]
    public void LoadConfiguration_MissingPersistedEntry_AnySaveId_YieldsCompleteDefaults(string? saveId)
    {
        var service = CreateService(out var mockDataService);
        mockDataService.Setup(m => m.LoadData<VisualizationConfiguration>(It.IsAny<string>()))
            .Returns((VisualizationConfiguration?)null);

        service.LoadConfiguration(saveId!);

        Assert.True(IsCompleteDefaults(service.GetConfiguration()));
    }
}
