using System;
using System.Collections.Generic;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of <see cref="ILivingRootsModApi"/> with subscriber
    /// exception isolation and idempotent disposal.
    /// </summary>
    public sealed class LivingRootsModApi : ILivingRootsModApi, IDisposable
    {
        private const string ApiVersionValue = "1.0.0";

        private EventHandler<SoilGrowthModifierCompletedEventArgs>? _handlers;
        private readonly object _lock = new();
        private bool _disposed;

        private ISoilHealthService? _soilHealthService;
        private ICompostingBinService? _compostingBinService;
        private ILocationProvider? _locationProvider;
        private Func<string, int, int, bool>? _applyAgrochemical;
        private Func<string, int, int, bool>? _applyMulch;
        private Func<string, int, int, long, bool>? _acquireHostLock;
        private Action<string, int, int, long>? _releaseHostLock;

        public string ApiVersion => ApiVersionValue;

        /// <summary>
        /// Binds the production services used by the dev/CI verification mod.
        /// Called once from the composition root; the API stays event-only
        /// until this runs, so an unwired mod fails closed.
        /// </summary>
        public void BindVerificationServices(
            ISoilHealthService soilHealthService,
            ICompostingBinService compostingBinService,
            ILocationProvider? locationProvider)
        {
            lock (_lock)
            {
                if (_disposed) return;
                _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
                _compostingBinService = compostingBinService ?? throw new ArgumentNullException(nameof(compostingBinService));
                _locationProvider = locationProvider;
            }
        }

        /// <summary>
        /// Binds the dev/CI verification application delegate for the 017 Joja/agrochemical input.
        /// Called from the composition root after the controller exists, unconditionally at
        /// wiring time; binding completeness is not enforced here and binding is not
        /// host-restricted at this point. A partially wired composition root (or a missing
        /// controller delegate) fails closed by returning false instead of mutating exposed
        /// state. Host authority, target eligibility, held-item and lock gates are enforced
        /// downstream by ModController (<c>IsMainPlayerCheck</c> and the application route),
        /// so any in-process mod may call <see cref="ApplyAgrochemical"/> yet cannot produce
        /// an unsafe local mutation or bypass host authority. Dev/CI-only usage is the
        /// expected consumption of this member; it is not a privilege boundary.
        /// </summary>
        public void BindAgrochemicalApplication(Func<string, int, int, bool> applyAgrochemical)
        {
            if (applyAgrochemical == null)
            {
                throw new ArgumentNullException(nameof(applyAgrochemical));
            }

            lock (_lock)
            {
                if (_disposed) return;
                _applyAgrochemical = applyAgrochemical;
            }
        }

        /// <summary>
        /// Binds the dev/CI verification mulch application delegate (013).
        /// Host authority and eligibility are enforced by the bound path/service.
        /// </summary>
        public void BindMulchApplication(Func<string, int, int, bool> applyMulch)
        {
            if (applyMulch == null)
            {
                throw new ArgumentNullException(nameof(applyMulch));
            }

            lock (_lock)
            {
                if (_disposed) return;
                _applyMulch = applyMulch;
            }
        }

        /// <summary>
        /// Binds dev/CI host-lock seams (authorized). Deterministic contention testing only.
        /// </summary>
        public void BindHostLockSeams(Func<string, int, int, long, bool> acquire, Action<string, int, int, long> release)
        {
            if (acquire == null) throw new ArgumentNullException(nameof(acquire));
            if (release == null) throw new ArgumentNullException(nameof(release));

            lock (_lock)
            {
                if (_disposed) return;
                _acquireHostLock = acquire;
                _releaseHostLock = release;
            }
        }

        public bool IsVerificationReady
        {
            get
            {
                lock (_lock)
                {
                    return !_disposed && _soilHealthService != null && _compostingBinService != null;
                }
            }
        }

        public bool TryGetSoilHealth(string locationName, int tileX, int tileY, out float health)
        {
            health = 0f;
            ISoilHealthService? service;
            lock (_lock)
            {
                if (_disposed) return false;
                service = _soilHealthService;
            }

            if (service == null) return false;
            return service.TryGetSoilHealth(locationName, new Vector2(tileX, tileY), out health);
        }

        public void SetSoilHealth(string locationName, int tileX, int tileY, float health)
        {
            ISoilHealthService? service;
            lock (_lock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(LivingRootsModApi));
                service = _soilHealthService;
            }

            if (service == null)
            {
                throw new InvalidOperationException("Verification services are not bound.");
            }

            service.SetSoilHealth(locationName, new Vector2(tileX, tileY), health);
        }

        public IReadOnlyList<SoilHealthSnapshotEntry> SnapshotSoilHealth()
        {
            ISoilHealthService? service;
            lock (_lock)
            {
                if (_disposed) return Array.Empty<SoilHealthSnapshotEntry>();
                service = _soilHealthService;
            }

            if (service == null) return Array.Empty<SoilHealthSnapshotEntry>();
            return service.Snapshot();
        }

        public bool PlaceBin(string locationName, int tileX, int tileY)
        {
            ICompostingBinService? bins;
            ILocationProvider? locations;
            lock (_lock)
            {
                if (_disposed) return false;
                bins = _compostingBinService;
                locations = _locationProvider;
            }

            if (bins == null) return false;

            GameLocation? location = locations?.GetLocationByName(locationName);
            if (location == null)
            {
                try
                {
                    location = Game1.getLocationFromName(locationName);
                }
                catch
                {
                    location = null;
                }
            }

            if (location == null) return false;

            var tile = new Vector2(tileX, tileY);
            if (!location.isTileOnMap(tile)) return false;
            if (location.Objects.ContainsKey(tile)) return false;
            if (!location.IsFarm && !IsGreenhouseLike(location)) return false;
            if (location.buildings != null && location.buildings.Any(building => building != null && building.occupiesTile(tile))) return false;

            var waterMap = location.waterTiles;
            if (waterMap != null &&
                tileX >= 0 && tileY >= 0 &&
                tileX < waterMap.waterTiles.GetLength(0) &&
                tileY < waterMap.waterTiles.GetLength(1) &&
                waterMap[tileX, tileY])
            {
                return false;
            }

            var binItem = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId };
            binItem.bigCraftable.Value = true;

            var placedObject = new StardewValley.Object { ItemId = ModConstants.CompostingBinItemId, TileLocation = tile };
            placedObject.bigCraftable.Value = true;
            location.Objects.Add(tile, placedObject);

            bins.OnBinPlaced(location.Name, tile, binItem);
            return true;
        }

        public bool InsertWaste(string locationName, int tileX, int tileY, string itemId, int count)
        {
            ICompostingBinService? bins;
            lock (_lock)
            {
                if (_disposed) return false;
                bins = _compostingBinService;
            }

            if (bins == null) return false;
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0) return false;

            var tile = new Vector2(tileX, tileY);
            if (bins.GetBinState(locationName, tile) != CompostingBinState.Empty) return false;

            Item item;
            try
            {
                item = ItemRegistry.Create(itemId, count);
            }
            catch
            {
                return false;
            }

            if (item == null) return false;

            bins.AddWaste(locationName, tile, item);
            return bins.GetBinState(locationName, tile) == CompostingBinState.Processing;
        }

        /// <summary>
        /// Dev/CI-only host path for the 017 Joja/agrochemical input. The delegate
        /// is bound unconditionally at wiring time and binding is not
        /// host-restricted at this point. Host authority, target eligibility,
        /// held-item and lock gates are enforced downstream by ModController
        /// (the <c>IsMainPlayerCheck</c> authority gate and the application
        /// route). Any in-process mod may call this method, but no call can
        /// produce an unsafe local mutation or bypass host authority: a
        /// non-host caller is refused by the controller before any service,
        /// lock, soil, or consumption effect. Dev/CI-only usage is the
        /// expected consumption of this member; it is not a privilege boundary.
        /// Returns false when the delegate is not bound or the application is
        /// refused or fails; the API performs no soil mutation itself.
        /// </summary>
        public bool ApplyAgrochemical(string locationName, int tileX, int tileY)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return false;
            }

            Func<string, int, int, bool>? apply;
            lock (_lock)
            {
                if (_disposed) return false;
                apply = _applyAgrochemical;
            }

            if (apply == null)
            {
                return false;
            }

            try
            {
                return apply(locationName, tileX, tileY);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool ApplyMulch(string locationName, int tileX, int tileY)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return false;
            }

            Func<string, int, int, bool>? apply;
            lock (_lock)
            {
                if (_disposed) return false;
                apply = _applyMulch;
            }

            if (apply == null)
            {
                return false;
            }

            try
            {
                return apply(locationName, tileX, tileY);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryAcquireHostLock(string locationName, int tileX, int tileY, long holderFarmerId)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return false;
            }

            Func<string, int, int, long, bool>? acquire;
            lock (_lock)
            {
                if (_disposed) return false;
                acquire = _acquireHostLock;
            }

            if (acquire == null)
            {
                return false;
            }

            try
            {
                return acquire(locationName, tileX, tileY, holderFarmerId);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void ReleaseHostLock(string locationName, int tileX, int tileY, long holderFarmerId)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return;
            }

            Action<string, int, int, long>? release;
            lock (_lock)
            {
                if (_disposed) return;
                release = _releaseHostLock;
            }

            if (release == null)
            {
                return;
            }

            try
            {
                release(locationName, tileX, tileY, holderFarmerId);
            }
            catch
            {
                // dev/CI seam: swallow
            }
        }

        private static bool IsGreenhouseLike(GameLocation location)
        {
            return location.Name != null &&
                location.Name.Contains("Greenhouse", StringComparison.OrdinalIgnoreCase);
        }

        public event EventHandler<SoilGrowthModifierCompletedEventArgs>? SoilGrowthModifierCompleted
        {
            add
            {
                if (value == null) return;
                lock (_lock)
                {
                    if (_disposed) return;
                    _handlers += value;
                }
            }
            remove
            {
                if (value == null) return;
                lock (_lock)
                {
                    _handlers -= value;
                }
            }
        }

        /// <summary>
        /// Raises the event, invoking each subscriber in isolation so that
        /// one subscriber's exception cannot abort the others or escape.
        /// </summary>
        public void RaiseSoilGrowthModifierCompleted(SoilGrowthModifierCompletedEventArgs args)
        {
            EventHandler<SoilGrowthModifierCompletedEventArgs>? snapshot;
            lock (_lock)
            {
                if (_disposed) return;
                snapshot = _handlers;
            }

            if (snapshot == null) return;

            foreach (var handler in snapshot.GetInvocationList())
            {
                try
                {
                    ((EventHandler<SoilGrowthModifierCompletedEventArgs>)handler)(this, args);
                }
                catch
                {
                    // Isolate subscriber exceptions; never propagate.
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _handlers = null;
                _disposed = true;
            }
        }
    }
}
