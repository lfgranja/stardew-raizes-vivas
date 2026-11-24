namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// Service for applying mulch to a tilled tile, following the DDD pattern
    /// (interface in Domain, implementation in Services).
    /// </summary>
    public interface IMulchApplicationService
    {
        void LoadData(string saveId);
        void SaveData(string saveId);

        bool TryApplyMulch(StardewValley.Farmer who, StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tile, out string messageKey);

        bool IsMulched(string locationName, Microsoft.Xna.Framework.Vector2 tile);

        void TickMulch(string locationName, Microsoft.Xna.Framework.Vector2 tile);

        int MulchVersion { get; }

        void Reset();
    }
}
