using System;
using LivingRoots.Domain.Interfaces;

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

        public string ApiVersion => ApiVersionValue;

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
