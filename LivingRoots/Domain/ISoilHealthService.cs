using System;
using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    public interface ISoilHealthService
    {
        /// <summary>
        /// Fired whenever a tile's soil health is mutated (FR-010).
        /// Subscribers — the visualization service — invalidate their overlay
        /// cache immediately, so the next frame renders the updated tier with
        /// zero polling lag. Raised outside the service's internal lock, on the
        /// mutating thread, exactly once per mutation.
        /// </summary>
        event Action<string, Vector2, float>? OnTileHealthChanged;

        void LoadData(string saveId);
        void SaveData(string saveId);
        float GetSoilHealth(string locationName, Vector2 tile);
        void SetSoilHealth(string locationName, Vector2 tile, float value);
        void UpdateHealth(string locationName, Vector2 tile, float delta);
        void Reset();
    }
}
