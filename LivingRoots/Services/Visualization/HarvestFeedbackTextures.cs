using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Procedural 16×16 feedback marker textures for harvest feedback (FR-005).
    /// Pixel data is deterministic and testable; Texture2D creation is lazy and
    /// safe when no GraphicsDevice is available (headless/tests).
    /// </summary>
    public static class HarvestFeedbackTextures
    {
        private static Texture2D? _markerPoor;
        private static Texture2D? _sparkleLiving;

        public static Color[] CreateMarkerPoorPixels()
        {
            var pixels = new Color[16 * 16];
            var border = new Color(180, 40, 40, 220);
            var fill = new Color(220, 70, 70, 180);
            var mark = new Color(255, 235, 235, 230);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int i = (y * 16) + x;
                    bool isBorder = x == 0 || y == 0 || x == 15 || y == 15;
                    bool isX = x == y || x == (15 - y);
                    bool inMark = isX && x >= 3 && x <= 12 && y >= 3 && y <= 12;
                    if (inMark)
                    {
                        pixels[i] = mark;
                    }
                    else if (isBorder)
                    {
                        pixels[i] = border;
                    }
                    else if (x >= 2 && x <= 13 && y >= 2 && y <= 13)
                    {
                        pixels[i] = fill;
                    }
                    else
                    {
                        pixels[i] = Color.Transparent;
                    }
                }
            }

            return pixels;
        }

        public static Color[] CreateSparkleLivingPixels()
        {
            var pixels = new Color[16 * 16];
            var core = new Color(140, 255, 170, 235);
            var glow = new Color(90, 220, 120, 170);
            var spark = new Color(245, 255, 245, 240);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int i = (y * 16) + x;
                    int dx = Math.Abs(x - 7);
                    int dy = Math.Abs(y - 7);
                    bool cross = (x == 7 || x == 8) && dy <= 5 || (y == 7 || y == 8) && dx <= 5;
                    bool diamond = (dx + dy) <= 3;
                    bool sparkle = (x == 3 && y == 4) || (x == 12 && y == 3) || (x == 4 && y == 12) || (x == 12 && y == 11);

                    if (sparkle)
                    {
                        pixels[i] = spark;
                    }
                    else if (diamond)
                    {
                        pixels[i] = core;
                    }
                    else if (cross)
                    {
                        pixels[i] = glow;
                    }
                    else
                    {
                        pixels[i] = Color.Transparent;
                    }
                }
            }

            return pixels;
        }

        public static Texture2D? GetMarkerPoorTexture()
        {
            return GetOrCreate(ref _markerPoor, CreateMarkerPoorPixels);
        }

        public static Texture2D? GetSparkleLivingTexture()
        {
            return GetOrCreate(ref _sparkleLiving, CreateSparkleLivingPixels);
        }

        public static Texture2D? GetTexture(string? spriteKey)
        {
            if (string.Equals(spriteKey, "marker-poor", StringComparison.Ordinal))
            {
                return GetMarkerPoorTexture();
            }

            if (string.Equals(spriteKey, "sparkle-living", StringComparison.Ordinal))
            {
                return GetSparkleLivingTexture();
            }

            return null;
        }

        internal static void ResetForTests()
        {
            _markerPoor = null;
            _sparkleLiving = null;
        }

        private static Texture2D? GetOrCreate(ref Texture2D? field, Func<Color[]> factory)
        {
            if (field != null && !field.IsDisposed)
            {
                return field;
            }

            try
            {
                var device = Game1.graphics?.GraphicsDevice;
                if (device == null)
                {
                    return null;
                }

                var texture = new Texture2D(device, 16, 16);
                texture.SetData(factory());
                field = texture;
                return field;
            }
            catch
            {
                return null;
            }
        }
    }
}
