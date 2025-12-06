using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    public interface ISoilHealthService
    {
        void LoadData(string saveId);
        void SaveData(string saveId);
        float GetSoilHealth(string locationName, Vector2 tile);
        void SetSoilHealth(string locationName, Vector2 tile, float value);
        void UpdateHealth(string locationName, Vector2 tile, float delta);
    }
}