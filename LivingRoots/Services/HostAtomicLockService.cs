using System.Collections.Generic;
using LivingRoots.Domain.Interfaces;
using Microsoft.Xna.Framework;

namespace LivingRoots.Services
{
    public class HostAtomicLockService : IHostAtomicLockService
    {
        private readonly Dictionary<(string LocationName, Vector2 Coordinate), long> _locks;
        private readonly object _lockObject = new object();

        public HostAtomicLockService()
        {
            _locks = new Dictionary<(string LocationName, Vector2 Coordinate), long>();
        }

        public bool TryAcquireLock(string locationName, Vector2 coordinate, long farmerId)
        {
            var key = (locationName, coordinate);

            lock (_lockObject)
            {
                if (_locks.TryGetValue(key, out long existingHolder))
                {
                    if (existingHolder == farmerId)
                    {
                        return true;
                    }

                    return false;
                }

                _locks.Add(key, farmerId);
                return true;
            }
        }

        public void ReleaseLock(string locationName, Vector2 coordinate, long farmerId)
        {
            var key = (locationName, coordinate);

            lock (_lockObject)
            {
                if (_locks.TryGetValue(key, out long existingHolder) && existingHolder == farmerId)
                {
                    _locks.Remove(key);
                }
            }
        }

        public bool IsLocked(string locationName, Vector2 coordinate)
        {
            var key = (locationName, coordinate);

            lock (_lockObject)
            {
                return _locks.ContainsKey(key);
            }
        }

        public long? GetLockHolder(string locationName, Vector2 coordinate)
        {
            var key = (locationName, coordinate);

            lock (_lockObject)
            {
                if (_locks.TryGetValue(key, out long holder))
                {
                    return holder;
                }

                return null;
            }
        }
    }
}
