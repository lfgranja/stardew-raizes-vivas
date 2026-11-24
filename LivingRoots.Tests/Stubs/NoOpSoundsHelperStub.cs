using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Audio;

namespace LivingRoots.Tests.Stubs;

/// <summary>
/// No-op <see cref="ISoundsHelper"/> used to neutralise <c>Game1.playSound</c> in unit tests.
/// </summary>
/// <remarks>
/// Production code under test calls <c>Game1.playSound("cancel")</c> on rejection paths.
/// The game-installed <see cref="SoundsHelper"/> resolves its audio bank through the
/// <c>Game1</c> singleton and the XNA content pipeline, both of which are unavailable in a
/// unit-test host, so the real helper throws <see cref="NullReferenceException"/>.
/// <see cref="ISoundsHelper"/> is a public interface stored in the public static
/// <c>Game1.sounds</c> field, so substituting this stub needs neither reflection nor a
/// booted game. Playback is a presentation concern and is not part of any assertion here.
/// </remarks>
public sealed class NoOpSoundsHelperStub : ISoundsHelper
{
    /// <summary>Unused; kept for interface completeness.</summary>
    public bool LogSounds { get; set; }

    /// <summary>Always reports that local playback is unavailable.</summary>
    /// <param name="context">Sound context; ignored.</param>
    /// <returns>Always <c>false</c>.</returns>
    public bool ShouldPlayLocal(SoundContext context)
    {
        return false;
    }

    /// <summary>Reports zero attenuation.</summary>
    /// <param name="location">Originating location; ignored.</param>
    /// <param name="position">Playback position; ignored.</param>
    /// <returns>Always <c>0f</c>.</returns>
    public float GetVolumeForDistance(GameLocation location, Vector2? position)
    {
        return 0f;
    }

    /// <summary>Silently drops the cue.</summary>
    /// <param name="cueName">Requested cue; ignored.</param>
    /// <param name="location">Originating location; ignored.</param>
    /// <param name="position">Playback position; ignored.</param>
    /// <param name="pitch">Requested pitch; ignored.</param>
    /// <param name="context">Sound context; ignored.</param>
    /// <param name="cue">Always set to <c>null</c>.</param>
    /// <returns>Always <c>false</c>.</returns>
    public bool PlayLocal(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context, out ICue cue)
    {
        cue = null!;
        return false;
    }

    /// <summary>Silently drops the cue.</summary>
    /// <param name="cueName">Requested cue; ignored.</param>
    /// <param name="location">Originating location; ignored.</param>
    /// <param name="position">Playback position; ignored.</param>
    /// <param name="pitch">Requested pitch; ignored.</param>
    /// <param name="context">Sound context; ignored.</param>
    public void PlayAll(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context)
    {
    }

    /// <summary>No-op.</summary>
    /// <param name="cue">Cue to adjust; ignored.</param>
    /// <param name="pitch">Requested pitch; ignored.</param>
    /// <param name="isMusic">Whether the cue is music; ignored.</param>
    public void SetPitch(ICue cue, float pitch, bool isMusic)
    {
    }
}
