using System.Threading;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots
{
    /// <summary>
    /// Main entry point for the Living Roots mod
    /// Following the architecture pattern described in ARCHITECTURE.md
    /// Now serves as a Composition Root where all dependencies are configured and injected
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private ModController? _controller;

        // T137 (FR-012): the mod-level subscriptions Entry takes
        // out, retained as fields so Dispose can detach the exact
        // delegate instances. A method-group or anonymous-lambda
        // subscription has no readable reference and can never be
        // released, leaking the subscriber past mod unload.
        private IModHelper? _helper;
        private ISoilHealthService? _soilHealthService;
        private EventHandler<AssetRequestedEventArgs>? _onAssetRequestedHandler;
        private Action<string, Vector2, float>? _onTileHealthChangedHandler;

        private bool _disposed = false;  // Thread-safe visibility of disposed state - all access is protected by _disposeLock
        private readonly object _disposeLock = new();

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            _helper = helper;

            // Subscribe to asset requested events for recipes and machines (Tasks T030 & T031).
            // T137 (FR-012): subscribe through the stored handler field so Dispose
            // detaches the exact delegate instance — a method-group subscription
            // leaves no readable reference to release on unload.
            _onAssetRequestedHandler = OnAssetRequested;
            helper.Events.Content.AssetRequested += _onAssetRequestedHandler;

            // Create domain services - Composition Root
            var unicodeNormalizationService = new UnicodeNormalizationService();
            var reservedNameHandler = new ReservedNameHandler(unicodeNormalizationService);
            var fileNameSanitizationService = new FileNameSanitizationService(unicodeNormalizationService, reservedNameHandler);
            var pathValidationService = new PathValidationService(unicodeNormalizationService);
            var modLogic = new ModLogic(fileNameSanitizationService, pathValidationService);

            // Create application services
            var modDataService = new ModDataService(helper, this.Monitor, modLogic);

            // Create the soil health service with the required dependencies
            var soilHealthService = new SoilHealthService(modDataService, this.Monitor, fileNameSanitizationService);
            _soilHealthService = soilHealthService;

            // Create the save ID provider with monitor for logging
            var saveIdProvider = new SaveIdProvider(this.Monitor);

            // Create testable interface implementations (Fix-4)
            var timeProvider = new TimeProvider();
            var seasonProvider = new SeasonProvider();
            var playerProvider = new PlayerProvider();

            // T055 (U3, FR-019): ONE host-authoritative lock
            // service, created here and passed to both the
            // controller and the bin service. Two `?? new`
            // fallbacks would hand each class a disjoint
            // lock registry, so a tile one of them locked
            // would look free to the other.
            var hostAtomicLockService = new HostAtomicLockService();

            // Create composting services
            // T141: OrganicWasteValidator takes the monitor for trace
            // diagnostics of accepted/rejected waste (IOrganicWasteValidator).
            var organicWasteValidator = new OrganicWasteValidator(this.Monitor);
            var compostingBinFactory = new CompostingBinFactory();
            var playerInventory = new PlayerInventory();
            // T054 (FR-008a): SMAPI's translation helper, injected
            // into every service that renders a refusal message so
            // Game1.showRedMessage shows the player's language.
            var translationHelper = helper.Translation;
            var locationProvider = new LocationProvider();
            var compostingBinService = new CompostingBinService(modDataService, saveIdProvider, organicWasteValidator, this.Monitor, timeProvider, compostingBinFactory, playerInventory, locationProvider: locationProvider, hostAtomicLockService: hostAtomicLockService, translationHelper: translationHelper);
            var compostApplicationService = new CompostApplicationService(soilHealthService, playerProvider, this.Monitor, translationHelper: translationHelper);

            // Create soil decay service
            var seasonalDecayMultiplier = new SeasonalDecayMultiplier();
            var soilDecayService = new SoilDecayService(soilHealthService, this.Monitor, seasonalDecayMultiplier, seasonProvider, locationProvider, timeProvider);

            // Create visualization services (F4 — DI wiring per Phase 9/10)
            var colorInterpolationService = new ColorInterpolationService();
            var visualizationConfigService = new VisualizationConfigurationService(modDataService, this.Monitor, fileNameSanitizationService);
            var hudNotifier = new HudNotifierImpl();
            var visualizationService = new VisualizationService(colorInterpolationService, visualizationConfigService, this.Monitor, hudNotifier);

            // Wire reactive cache invalidation upon soil health change (T036, FR-010).
            // T137 (FR-012): a stored named handler rather than an anonymous
            // lambda — an anonymous subscription has no readable reference, so
            // it can never be detached and keeps the visualization service
            // alive through the event past mod unload.
            _onTileHealthChangedHandler = (_, _, _) => visualizationService.InvalidateCache();
            soilHealthService.OnTileHealthChanged += _onTileHealthChangedHandler;

            // Create controller with dependency injection.
            // T069 (FR-004): the color interpolator is the
            // SAME instance the visualization service holds,
            // so a configuration change applies the custom
            // category palette live (SetCategoryColors) —
            // without this wiring the controller's palette
            // application is a null-conditional no-op and
            // the interpolator keeps its ModConstants
            // defaults.
            // T055 (U3): the player provider (live hoe
            // feedback — a null provider silently skips
            // HandleHoeUsage) and the shared host atomic
            // lock service (the same instance the bin
            // service holds) are injected here, not left
            // to the constructor's `?? new` fallbacks.
            _controller = new ModController(helper, this.Monitor, this.ModManifest, soilHealthService, saveIdProvider, compostingBinService, soilDecayService, compostApplicationService, visualizationService, visualizationConfigService, hostAtomicLockService: hostAtomicLockService, playerProvider: playerProvider, colorInterpolationService: colorInterpolationService);

            // Register events through the Controller
            _controller.RegisterEvents();
        }

        /*********
        ** Protected methods
        ********/

        /// <summary>Clean up resources when the mod is unloaded.</summary>
        /// <param name="disposing">Whether to instance is being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            // Use a lock to ensure thread-safe disposal
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    // T137 (FR-012): release the mod-level event subscriptions
                    // Entry took out, before the controller tears down its own.
                    UnsubscribeModEvents();

                    _controller?.Dispose();
                    _controller = null;
                }

                _disposed = true;
            }

            // Call base.Dispose after releasing the lock to prevent potential deadlocks
            base.Dispose(disposing);
        }

        /*********
        ** Private methods
        *********/

        /// <summary>
        /// Releases the event subscriptions <see cref="Entry"/> took out
        /// (T137, FR-012): the content-pipeline asset handler and the
        /// soil-health cache-invalidation hook. Both detach through the
        /// stored handler fields, which are then nulled, so a second
        /// disposal is a no-op and no delegate outlives the mod.
        /// </summary>
        private void UnsubscribeModEvents()
        {
            if (_onAssetRequestedHandler != null)
            {
                _helper?.Events?.Content.AssetRequested -= _onAssetRequestedHandler;
                _onAssetRequestedHandler = null;
            }

            if (_onTileHealthChangedHandler != null)
            {
                _soilHealthService?.OnTileHealthChanged -= _onTileHealthChangedHandler;
                _onTileHealthChangedHandler = null;
            }

            _helper = null;
            _soilHealthService = null;
        }

        /// <summary>Raised when an asset is requested from the content pipeline.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    // T053 (FR-001/FR-006): segment 3 is `true` so the
                    // yield resolves as the (BC) big craftable registered
                    // in Data/BigCraftables (a `false` yield would resolve
                    // as an (O) object that no longer exists); segment 4
                    // is `default` so the recipe is learned on day one
                    // instead of requiring an unlock event.
                    data[ModConstants.CompostingBinRecipeId] = $"388 {ModConstants.CompostingBinWoodCost} 390 {ModConstants.CompostingBinStoneCost} 771 {ModConstants.CompostingBinFiberCost}/{ModConstants.CompostCraftingTab}/{ModConstants.CompostingBinItemId} 1/true/default/Composting Bin";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Objects.ObjectData>().Data;

                    // T053 (FR-001a/FR-006): compost is the only object
                    // this mod contributes. The composting bin moved to
                    // Data/BigCraftables — registering it here as well
                    // would fork one concept into two item types and
                    // point the recipe and the machine rules at
                    // different items.
                    // T078 (FR-005): the compost
                    // entry carries its own icon.
                    // Without Texture the game falls
                    // back to the vanilla springobjects
                    // sheet at SpriteIndex 0 — a
                    // weeds/stone-adjacent sprite that
                    // reads as another fertilizer. The
                    // texture is served by the
                    // CompostTextureAssetName branch
                    // below (LoadFromModFile), and the
                    // PNG is a single 16×16 icon, so
                    // SpriteIndex 0 fills the frame.
                    data[ModConstants.CompostItemId] = new StardewValley.GameData.Objects.ObjectData
                    {
                        Name = "Compost",
                        DisplayName = "Compost",
                        Description = $"Organic compost rich in microbial life. Restores {ModConstants.RestorationAmount:F0} soil health to tilled tiles.",
                        Type = "Basic",
                        Category = ModConstants.CompostCategoryId,
                        Price = 10,
                        Texture = ModConstants.CompostTextureAssetName,
                        SpriteIndex = 0
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.CompostTextureAssetName))
            {
                // T078 (FR-005): provide the compost
                // icon as a game asset so the
                // ObjectData.Texture pointer above
                // resolves. Asset names under Mods/
                // have no locale variants, so
                // NameWithoutLocale identifies the
                // asset exactly as Name would (the
                // existing test doubles only mock
                // NameWithoutLocale). LoadFromModFile
                // paths are mod-folder-relative;
                // Assets/compost.png is a 16×16
                // 32-bit RGBA PNG copied to the mod
                // folder by the csproj.
                e.LoadFromModFile<Texture2D>("Assets/compost.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.CompostingBinTextureAssetName))
            {
                // T099 (FR-001a): serve the
                // composting bin's empty-state
                // texture as a game asset so the
                // BigCraftableData.Texture pointer
                // above resolves through the game
                // content pipeline. Same conventions
                // as the compost icon branch: asset
                // names under Mods/ have no locale
                // variants, and the load path is
                // mod-folder-relative —
                // Assets/composting_bin_empty.png is
                // the 16×32 RGBA empty-state sprite
                // the csproj copies to the mod
                // folder. No ContentPatcher
                // dependency: e.LoadFromModFile is
                // the pure-SMAPI equivalent (T053
                // spike decision,
                // briefings/spike-decision.md).
                e.LoadFromModFile<Texture2D>("Assets/composting_bin_empty.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.CompostingBinProcessingSpriteAssetName))
            {
                // T099 (FR-001a): serve the
                // composting bin's processing-state
                // progress sprite as a game asset so
                // the native machine interface can
                // render the working state from the
                // mod's own sprite — the same
                // LoadFromModFile convention as the
                // bin texture branch above.
                // Assets/composting_bin_processing.png
                // is the 16×32 RGBA processing-state
                // sprite the csproj copies to the mod
                // folder.
                e.LoadFromModFile<Texture2D>("Assets/composting_bin_processing.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(ModConstants.CompostingBinReadySpriteAssetName))
            {
                // T099 (FR-001a): serve the
                // composting bin's ready-state
                // progress sprite as a game asset so
                // the native machine interface can
                // render the finished state from the
                // mod's own sprite — the same
                // LoadFromModFile convention as the
                // bin texture branch above.
                // Assets/composting_bin_ready.png is
                // the 16×32 RGBA ready-state sprite
                // the csproj copies to the mod folder.
                e.LoadFromModFile<Texture2D>("Assets/composting_bin_ready.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftables.BigCraftableData>().Data;

                    // T053 (FR-001a/FR-001b): the bin as a native big
                    // craftable — the machine type every vanilla
                    // machine (keg, worm bin, crystalarium) uses. Key
                    // is the unqualified item ID; the machine behaviour
                    // lives in Data/Machines under the qualified (BC)
                    // key. Fragility 0 keeps the bin pick-up-able with
                    // any tool (FR-005c); indoor placement covers the
                    // Greenhouse (FR-002), outdoor the farm.
                    // T099 (FR-001a): the entry carries its own
                    // texture asset path, so the native machine
                    // interface renders the mod's bin instead of
                    // falling back to the vanilla big-craftable
                    // sheet — the asset ModEntry serves through the
                    // CompostingBinTextureAssetName branch below
                    // (LoadFromModFile), the T078 convention the
                    // compost icon already uses.
                    data[ModConstants.CompostingBinItemId] = new StardewValley.GameData.BigCraftables.BigCraftableData
                    {
                        Name = "CompostingBin",
                        DisplayName = "Composting Bin",
                        Description = "Converts organic waste into rich compost over time. One batch yields one compost.",
                        Price = 0,
                        Texture = ModConstants.CompostingBinTextureAssetName,
                        Fragility = 0,
                        CustomFields = new Dictionary<string, string>
                        {
                            [ModConstants.ModDataInputIdKey] = "",
                            [ModConstants.ModDataDaysRemainingKey] = ""
                        },
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = true
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data;
                    var binId = $"(BC){ModConstants.MachineCompostingBinId}";
                    data[binId] = new StardewValley.GameData.Machines.MachineData
                    {
                        // T053 (FR-001b): fairy dust would accelerate the
                        // batch outside the mod's timing authority.
                        AllowFairyDust = false,
                        OutputRules = new System.Collections.Generic.List<StardewValley.GameData.Machines.MachineOutputRule>
                        {
                            new StardewValley.GameData.Machines.MachineOutputRule
                            {
                                Id = "Default",
                                // T053 (FR-003): the native input filter mirrors
                                // OrganicWasteValidator's hybrid validation:
                                // inclusion tag "compostable_item" OR vanilla
                                // category IDs (-74 seeds, -75 vegetables,
                                // -79 fruits, -80 flowers, -81 greens),
                                // with exclusion tag "not_compostable"
                                // overriding everything. Each trigger ANDs its
                                // tags; any matching trigger starts the rule.
                                // The positive tag set mirrors OrganicWasteValidator's
                                // inherited qualifying set (spec 002 FR-008)
                                // so the native machine path and the mod's
                                // AddWaste path accept the same items, and
                                // !not_compostable keeps explicitly excluded
                                // items out even when they match a category.
                                // (T133 FR-003) Machine rule triggers use the validator-owned waste set:
                                // inclusion tag "compostable_item" OR vanilla category IDs (-74 seeds, -75 vegetables,
                                // -79 fruits, -80 flowers, -81 greens), with exclusion tag "not_compostable" overriding
                                // everything. Each trigger ANDs its tags; any matching trigger starts the rule.
                                // The positive tag set mirrors OrganicWasteValidator's
                                // inherited qualifying set (spec 002 FR-008) so the native machine path and the mod's
                                // AddWaste path accept the same items, and
                                // !not_compostable keeps explicitly excluded items out even when they match a category.
                                // (T133 FR-003) Machine rule triggers use the validator-owned waste set:
                                // the inclusion and exclusion tags are the validator's own constants
                                // (OrganicWasteValidator.CompostableItemTag / NotCompostableTag), so the
                                // native machine path can never restate or drift from the qualifying
                                 // set the mod's AddWaste path accepts (T120 asserts parity).
                                 Triggers = new System.Collections.Generic.List<StardewValley.GameData.Machines.MachineOutputTriggerRule>
                                 {
                                     new StardewValley.GameData.Machines.MachineOutputTriggerRule { Id = "Compost", Trigger = StardewValley.GameData.Machines.MachineOutputTrigger.ItemPlacedInMachine, RequiredTags = new System.Collections.Generic.List<string> { Domain.Services.OrganicWasteValidator.CompostableItemTag }, RequiredCount = 1 },
                                     // (T133 FR-003) Modded trigger uses the validator-owned waste set as the canonical tag-based filter.
                                     new StardewValley.GameData.Machines.MachineOutputTriggerRule { Id = "Modded", Trigger = StardewValley.GameData.Machines.MachineOutputTrigger.ItemPlacedInMachine, RequiredTags = new System.Collections.Generic.List<string> { Domain.Services.OrganicWasteValidator.CompostableItemTag, "!" + Domain.Services.OrganicWasteValidator.NotCompostableTag }, RequiredCount = 1 }
                                 },
                                OutputItem = new System.Collections.Generic.List<StardewValley.GameData.Machines.MachineItemOutput>
                                {
                                    // Native payout disabled — rely on
                                    // CollectCompost interception instead.
                                    // Item output intentionally omitted to
                                    // prevent dual-payout (T119).
                                },
                                // T053 (FR-001b): the native processing
                                // duration — without it the output would be
                                // ready instantly.
                                DaysUntilReady = ModConstants.MaturationDays
                            }
                        }
                    };
                });
            }
        }
    }
}
