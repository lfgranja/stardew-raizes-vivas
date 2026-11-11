using LivingRoots;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Shared helper for creating temporary animated feedback sprites on tiles.
    /// </summary>
    internal static class FeedbackSpriteFactory
    {
        public static TemporaryAnimatedSprite CreateTileSprite(Vector2 tile, string text, Color color)
        {
            var worldPosition = new Vector2(tile.X * (float)ModConstants.TileSize + 32f, tile.Y * (float)ModConstants.TileSize);
            var sprite = new TemporaryAnimatedSprite(
                null,
                Rectangle.Empty,
                1000f,
                1,
                1,
                worldPosition,
                false,
                false);
            sprite.text = text;
            sprite.color = color;
            sprite.alphaFade = 0.02f;
            sprite.motion = new Vector2(0, -0.5f);
            return sprite;
        }
    }
}
