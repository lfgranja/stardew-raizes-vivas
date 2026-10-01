using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using LivingRoots.Tests.Stubs;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Minimal content manager that stands in for <c>Game1.content</c> in a unit-test host.
/// </summary>
/// <remarks>
/// The item context-tag pipeline (<c>Item.HasContextTag</c>) reads the localized string table
/// and the <c>Data/Machines</c> asset through <c>Game1.content</c>. Both are populated by the
/// game's content pipeline at boot, which never runs in a test host, so the pipeline throws
/// before the validator under test is ever reached.
/// <para>
/// This stand-in answers those lookups with empty results, which is the truthful answer for an
/// item that carries no context tags — the situation the category-based validation in
/// <see cref="Domain.Services.OrganicWasteValidator"/> is written for. It deliberately does not
/// invent item names or machine rules.
/// </para>
/// </remarks>
internal sealed class MinimalContentManager : LocalizedContentManager
{
    /// <summary>Creates the stand-in with a service provider that resolves no XNA services.</summary>
    public MinimalContentManager()
        : base(new NullServiceProvider(), "Content")
    {
    }

    /// <inheritdoc />
    public override string LoadString(string key)
    {
        return key;
    }

    /// <inheritdoc />
    public override T Load<T>(string assetName)
    {
        return CreateEmptyAsset<T>();
    }

    /// <inheritdoc />
    public override T Load<T>(string assetName, LanguageCode languageCode)
    {
        return CreateEmptyAsset<T>();
    }

    /// <summary>
    /// Returns an empty collection for collection-shaped assets and the default value otherwise,
    /// so callers such as <c>DataLoader.Machines(...).TryGetValue</c> see an empty game.
    /// </summary>
    /// <typeparam name="T">Asset type requested by the game.</typeparam>
    /// <returns>An empty collection when <typeparamref name="T"/> is one; otherwise the default.</returns>
    private static T CreateEmptyAsset<T>()
    {
        if (typeof(T).IsArray)
        {
            return (T)(object)Array.CreateInstance(typeof(T).GetElementType()!, 0);
        }

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return (T)Activator.CreateInstance(typeof(T))!;
        }

        return default!;
    }

    /// <summary>Service provider that resolves nothing; the game never reaches XNA through this path.</summary>
    private sealed class NullServiceProvider : IServiceProvider
    {
        /// <inheritdoc />
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}

/// <summary>
/// Installs the minimum amount of Stardew Valley global state that domain-level unit tests need.
/// </summary>
/// <remarks>
/// Several game services read process-wide statics that are only populated once the game has
/// booted. This fixture installs empty, truthful stand-ins for exactly those statics so the
/// production logic under test becomes observable. It uses only public game APIs — no reflection
/// into private state (NFR-4) and no production change.
/// <para>The installed state is:</para>
/// <list type="bullet">
///   <item><description><c>Game1.content</c> — see <see cref="MinimalContentManager"/>.</description></item>
///   <item><description>
///     <c>Game1.objectData</c> — an empty dictionary, so <c>ObjectDataDefinition.GetAllIds()</c>
///     reports no item ids instead of dereferencing an uninitialised game field.
///   </description></item>
///   <item><description>
///     <c>ItemRegistry</c> — the vanilla <c>(O)</c> object type definition, which
///     <c>ItemRegistry.RequireTypeDefinition("(O)")</c> needs before it can answer a context-tag
///     query.
///   </description></item>
///   <item><description><c>Game1.sounds</c> — see <see cref="Stubs.NoOpSoundsHelperStub"/>.</description></item>
///   <item><description>
///     <c>Game1.game1</c> — allocated without running the constructor, because the real one
///     dereferences the XNA content service provider that only MonoGame sets while booting.
///     Every game type whose constructor reads <c>Game1.currentLocation</c> needs this to exist,
///     even when the location it reads is null.
///   </description></item>
/// </list>
/// <para>
/// This state is process-global, so <see cref="Install"/> is idempotent and guarded by a lock
/// because xUnit runs test classes in parallel.
/// </para>
/// </remarks>
public static class GameStateFixture
{
    private static readonly object SyncRoot = new();
    private static bool _installed;

    /// <summary>
    /// Installs the minimum game state required by domain-level unit tests. Safe to call from
    /// every test constructor and from parallel test classes.
    /// </summary>
    public static void Install()
    {
        lock (SyncRoot)
        {
            if (_installed)
            {
                return;
            }

            Game1.content = new MinimalContentManager();
            Game1.objectData = new Dictionary<string, ObjectData>();
            ItemRegistry.AddTypeDefinition(new ObjectDataDefinition());
            Game1.sounds = new Stubs.NoOpSoundsHelperStub();

            // The real Game1 constructor dereferences Content.ServiceProvider, which MonoGame only
            // populates while booting the game. Allocating without it gives the static accessor a
            // target, so types like HoeDirt can resolve Game1.currentLocation (and get null).
            var game = (Game1)FormatterServices.GetUninitializedObject(typeof(Game1));
            typeof(Game1)
                .GetField("game1", BindingFlags.Public | BindingFlags.Static)!
                .SetValue(null, game);

            _installed = true;
        }
    }
}
