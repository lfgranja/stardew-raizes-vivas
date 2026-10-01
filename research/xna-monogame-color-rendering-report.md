# XNA/MonoGame Color Rendering Technical Research

**Date:** 2026-09-09
**Context:** Stardew Valley mod rendering colored tile overlays (Living Roots mod)
**Scope:** Color interpolation, XNA Color struct, alpha blending, texture patterns, pixel-perfect rendering, performance, and color values

---

## 1. Color Interpolation

### Is Linear RGB Interpolation Perceptually Uniform?

**No.** Linear RGB interpolation is **not** perceptually uniform. This is a well-documented fact in color science.

When you interpolate linearly between two colors in RGB space (e.g., from red `#B91C1C` to green `#15803D`), the intermediate colors pass through the "gray zone" — a desaturated, muddy region in the middle of the interpolation path. This happens because RGB space does not account for human perception: equal distances in RGB do not correspond to equal perceived color differences.

> "HSL and HSV are simple transformations of RGB which preserve symmetries in the RGB cube unrelated to human perception, such that its R, G, and B corners are equidistant from the neutral axis, and equally spaced around it. If we plot the RGB gamut in a more perceptually-uniform space, such as CIELAB, it becomes immediately clear that the red, green, and blue primaries do not have the same lightness or chroma, or evenly spaced hues."
> — [Wikipedia: HSL and HSV](https://en.wikipedia.org/wiki/HSL_and_HSV)

### The "Gray Zone" Problem

Yes, RGB interpolation through the gray zone causes desaturated muddy colors. This is especially noticeable when interpolating between complementary or near-complementary colors. For example, interpolating from red to green in RGB produces brownish-gray intermediates rather than a smooth yellow-to-green transition.

### Alternatives

| Method | Perceptual Uniformity | Pros | Cons |
|--------|----------------------|------|------|
| **Linear RGB** | No | Fast, simple, built into `Color.Lerp` | Muddy intermediates, non-uniform |
| **HSL/HSV** | No (but better than RGB) | Intuitive hue control | Still not perceptually uniform; HSL confounds perceptual attributes |
| **CIELAB (L\*a\*b\*)** | Yes (approximate) | Perceptually uniform; good for interpolation | More complex math; not built into XNA |
| **CIELUV** | Yes (approximate) | Better uniformity than LAB in some regions | Similar complexity to LAB |
| **HSLuv** | Yes | Combines HSL intuitiveness with perceptual uniformity | Requires external library/conversion |
| **LCH (Lightness-Chroma-Hue)** | Yes | Cylindrical representation of LAB; good for gradients | Most complex; requires conversion |

> "This is why I much prefer the Lab color space over HSV or HSL — it's much better suited to interpolation and mixing."
> — [Hacker News discussion](https://news.ycombinator.com/item?id=21030051)

### Recommendation for Pixel Art Games

For a Stardew Valley mod rendering health band overlays:

1. **If performance is critical and transitions are between similar hues** (e.g., yellow→green, orange→red): Use `Color.Lerp` (linear RGB). It is fast, built into MonoGame, and the muddy intermediates are less noticeable when colors are adjacent on the color wheel.

2. **If quality is paramount and transitions span wide hue ranges** (e.g., red→green through yellow): Convert to a perceptually uniform space (LAB or LCH), interpolate there, then convert back to RGB. This avoids the gray zone entirely.

3. **Practical middle ground**: Interpolate in **HSL space by hue only**, keeping saturation and lightness fixed. This produces clean, vibrant transitions for health bands where you want to signal "good → bad" through hue rotation without desaturation.

The built-in `Color.Lerp` in MonoGame performs linear RGB interpolation:

```csharp
// From MonoGame source (Color.cs):
public static Color Lerp(Color value1, Color value2, Single amount)
{
    amount = MathHelper.Clamp(amount, 0, 1);
    return new Color(
        (int)MathHelper.Lerp(value1.R, value2.R, amount),
        (int)MathHelper.Lerp(value1.G, value2.G, amount),
        (int)MathHelper.Lerp(value1.B, value2.B, amount),
        (int)MathHelper.Lerp(value1.A, value2.A, amount));
}
```

> Source: [MonoGame Color.cs (GitHub)](https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Color.cs)

---

## 2. XNA Color Struct

### What is `Microsoft.Xna.Framework.Color`?

`Microsoft.Xna.Framework.Color` is a 32-bit packed color struct in the XNA/MonoGame framework. It stores four components (R, G, B, A) as bytes (0-255) and provides constructors, operators, and static predefined colors.

### Constructors

| Constructor | Description |
|------------|-------------|
| `Color(int r, int g, int b)` | RGB from 0-255, alpha = 255 |
| `Color(int r, int g, int b, int alpha)` | RGBA from 0-255 |
| `Color(float r, float g, float b)` | RGB from 0.0f-1.0f, alpha = 1.0f |
| `Color(float r, float g, float b, float alpha)` | RGBA from 0.0f-1.0f |
| `Color(byte r, byte g, byte b, byte alpha)` | RGBA bytes directly (fastest, no clamping) |
| `Color(Vector3 color)` | XYZ from vector (unit length), alpha = 255 |
| `Color(Vector4 color)` | XYZW from vector (unit length) |
| `Color(uint packedValue)` | 32-bit packed value (R in least significant octet) |
| `Color(Color color, int alpha)` | Copy RGB with new alpha (0-255) |
| `Color(Color color, float alpha)` | Copy RGB with new alpha (0.0f-1.0f) |

> Source: [MonoGame Color Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Color.html)

### How It Differs from `System.Drawing.Color`

| Feature | `Microsoft.Xna.Framework.Color` | `System.Drawing.Color` |
|---------|--------------------------------|------------------------|
| **Namespace** | `Microsoft.Xna.Framework` | `System.Drawing` |
| **Purpose** | GPU rendering (XNA/MonoGame) | GDI+ / Windows Forms |
| **Component type** | `byte` (0-255) | `byte` (0-255) via R/G/B/A properties |
| **Float constructors** | Yes (0.0f-1.0f) | No (uses `FromArgb` with ints) |
| **Vector constructors** | Yes (Vector3, Vector4) | No |
| **Packed value** | Yes (`uint` constructor) | Yes (`FromArgb(int)`) |
| **Named colors** | Static properties (e.g., `Color.Red`) | Static properties + `KnownColor` enum |
| **Lerp method** | Built-in `Color.Lerp()` | Not built-in |
| **Multiply operator** | Built-in (`color * scale`) | Not built-in |
| **Compatibility** | Not interchangeable | Not interchangeable |

> **Critical:** You **cannot** implicitly convert between `System.Drawing.Color` and `Microsoft.Xna.Framework.Color`. You must manually extract R, G, B, A values and construct the target type.

### Converting from Hex to XNA Color

There is **no built-in** hex parser for `Microsoft.Xna.Framework.Color`. You must write your own:

```csharp
public static class ColorBuilder
{
    public static Color FromHex(string color)
    {
        var hex = color.Replace("#", string.Empty);
        var h = System.Globalization.NumberStyles.HexNumber;

        var r = int.Parse(hex.Substring(0, 2), h);
        var g = int.Parse(hex.Substring(2, 2), h);
        var b = int.Parse(hex.Substring(4, 2), h);
        var a = 255;

        if (hex.Length == 8)
        {
            a = int.Parse(hex.Substring(6, 2), h);
        }

        return new Color(r, g, b, a);
    }
}

// Usage:
var red = ColorBuilder.FromHex("#B91C1C");
var green = ColorBuilder.FromHex("#15803D");
var amber = ColorBuilder.FromHex("#D97706");
var semiTransparent = ColorBuilder.FromHex("#B91C1C80"); // 50% alpha
```

> Source: [StackOverflow: Convert HEX to XNA Color](https://stackoverflow.com/questions/70593211/)

---

## 3. Alpha Blending in XNA/MonoGame

### How `BlendState.AlphaBlend` Works

`BlendState.AlphaBlend` is a built-in blend state that performs standard alpha blending using **premultiplied alpha**:

```
FinalColor = (source × SourceAlpha) + (destination × (1 - SourceAlpha))
```

In MonoGame's implementation:
- **Source blend factor:** `Blend.SourceAlpha`
- **Destination blend factor:** `Blend.OneMinusSourceAlpha`
- **Assumes:** Source texture colors are premultiplied by their alpha

> Source: [MonoGame BlendState Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BlendState.html)

### BlendState Options

| BlendState | Use Case |
|-----------|----------|
| `BlendState.AlphaBlend` | Standard transparency (premultiplied alpha) |
| `BlendState.Additive` | Additive blending (light effects, glow) |
| `BlendState.NonPremultiplied` | Non-premultiplied alpha (raw alpha in texture) |
| `BlendState.Opaque` | No blending (overwrite destination) |

### Rendering Semi-Transparent Overlays

```csharp
spriteBatch.Begin(
    blendState: BlendState.AlphaBlend,
    samplerState: SamplerState.PointClamp
);

// Draw semi-transparent overlay (50% opacity)
Color overlayColor = new Color(185, 28, 28, 128); // R, G, B, A (0-255)
spriteBatch.Draw(_whiteTexture, tileRect, overlayColor);

spriteBatch.End();
```

### Alpha and Pixel Art Aesthetic

For pixel art games like Stardew Valley:

1. **Use discrete alpha levels** (e.g., 25%, 50%, 75%) rather than continuous alpha — this preserves the crisp, intentional look of pixel art.
2. **Avoid full transparency** for overlays — instead use a semi-opaque fill that lets the underlying tile show through.
3. **Premultiplied alpha matters:** If you're creating textures with alpha, ensure they use premultiplied alpha for correct blending. The default `BlendState.AlphaBlend` expects premultiplied alpha.
4. **For tile overlays:** A common pattern is to use a 1x1 white texture tinted with the desired color and alpha, stretched over the tile. This is performant and gives consistent results.

> "BlendState.AlphaBlend means that the alpha channel of the sprite should be respected, and partially-transparent pixels will be blended with the background."
> — [Mysterious Space: Pixel Shaders in MonoGame](https://mysteriousspace.com/2019/01/05/pixel-shaders-in-monogame-a-tutorial-of-sorts-for-2019/)

---

## 4. Texture Creation for Patterns

### Approaches for Diagonal Stripes and Dot Patterns

For the design system's diagonal stripes and dot patterns, there are three main approaches:

#### Approach A: Pre-rendered Texture2D with Pixel Data

Create a small texture (e.g., 8x8 or 16x16) and set pixel data programmatically:

```csharp
public static Texture2D CreateStripePattern(GraphicsDevice graphicsDevice, int size = 8)
{
    var texture = new Texture2D(graphicsDevice, size, size);
    var data = new Color[size * size];

    for (int y = 0; y < size; y++)
    {
        for (int x = 0; x < size; x++)
        {
            // Diagonal stripe: alternate based on (x + y) parity
            bool isStipe = ((x + y) % 2) == 0;
            data[y * size + x] = isStipe ? Color.White : Color.Transparent;
        }
    }

    texture.SetData(data);
    return texture;
}

public static Texture2D CreateDotPattern(GraphicsDevice graphicsDevice, int size = 8)
{
    var texture = new Texture2D(graphicsDevice, size, size);
    var data = new Color[size * size];

    for (int y = 0; y < size; y++)
    {
        for (int x = 0; x < size; x++)
        {
            // Dot pattern: white dot on transparent background
            bool isDot = (x % 2 == 0) && (y % 2 == 0);
            data[y * size + x] = isDot ? Color.White : Color.Transparent;
        }
    }

    texture.SetData(data);
    return texture;
}
```

#### Approach B: White Texture Tinted with Color (Recommended)

Create a **single white pattern texture** and tint it at draw time:

```csharp
// Create once: white pattern on transparent background
Texture2D _stripePattern; // white stripes, transparent gaps

// Draw with tint:
spriteBatch.Draw(_stripePattern, destinationRect, color * opacity);
```

**This is the most performant approach** because:
- Only one texture is loaded into GPU memory
- Color changes are free (just a different draw color parameter)
- No per-pixel CPU work at draw time
- Pattern texture can be small and tiled using `SamplerState.PointWrap`

#### Approach C: Procedural in Shader (Advanced)

For complex or animated patterns, a custom shader can generate patterns procedurally. This is overkill for static health band overlays but useful for animated effects.

### Recommendation

**Use Approach B (white texture + tint)** for the Living Roots mod:
- Create small (8x8) pattern textures with white pattern elements on transparent background
- Tint with the health band color at draw time
- Use `SamplerState.PointWrap` for seamless tiling across large tiles
- This is the standard approach in MonoGame/XNA for colored patterns

> "Just create a 1px * 1px white texture and pass in the color as the color argument. You can draw a box using a precreated white pixel (1x1)..."
> — [GameDev StackExchange](https://gamedev.stackexchange.com/questions/163173/monogame-xna-texture2d-setdata-results-in-black-texture)

---

## 5. Pixel-Perfect Rendering

### Is `SamplerState.PointClamp` Correct for Pixel Art?

**Yes.** `SamplerState.PointClamp` is the correct choice for pixel art in MonoGame.

| SamplerState | Filter | Address | Use Case |
|-------------|--------|---------|----------|
| `PointClamp` | Nearest neighbor | Clamp | **Pixel art sprites** — crisp edges, no blur |
| `LinearClamp` | Bilinear | Clamp | Smooth scaling, UI elements |
| `PointWrap` | Nearest neighbor | Wrap | Tiled pixel art backgrounds |
| `LinearWrap` | Bilinear | Wrap | Smooth tiled backgrounds |

> "The default sampler state for SpriteBatch is SamplerState.LinearClamp in MonoGame, though SamplerState.PointClamp is often preferred for pixel art games to prevent blurring."
> — [MonoGame Texture Sampling Tutorial](https://docs.monogame.net/articles/tutorials/building_2d_games/18_texture_sampling/index.html)

### Integer Pixel Positions

**Yes, use integer pixel positions** for pixel art. Sub-pixel positions cause blurring even with point sampling because the GPU interpolates texture coordinates.

```csharp
// Correct: integer positions
int pixelX = (int)position.X;
int pixelY = (int)position.Y;
spriteBatch.Draw(texture, new Rectangle(pixelX, pixelY, width, height), color);

// Or use Math.Round for cleaner rounding:
int pixelX = (int)Math.Round(position.X);
int pixelY = (int)Math.Round(position.Y);
```

> "If you must have pixel-perfect accuracy in rendering, you will want to have a float value for position use integer values for the position of the sprite."
> — [GameDev StackExchange](https://gamedev.stackexchange.com/questions/45365/sprites-rendering-blurry-with-velocity)

### SpriteBatch Alignment Issues

MonoGame's `SpriteBatch` has known floating-point precision issues:

> "SpriteBatch disregardes floating point values · Issue #2978"
> — [MonoGame GitHub Issues](https://github.com/MonoGame/MonoGame/issues/2978)

**Best practices:**
1. Always pass integer positions to `SpriteBatch.Draw()`
2. Use `SamplerState.PointClamp` for pixel art
3. Avoid non-integer scaling (e.g., 1.5x) — use integer multiples (2x, 3x) instead
4. For camera/viewport scrolling, round the camera position to integers before applying

---

## 6. Performance of Overlay Rendering

### Realistic Draw Call Budget

For a Stardew Valley mod targeting 60 FPS (16.67ms per frame) with a **<4ms budget** for visualization:

| Metric | Value |
|--------|-------|
| **Frame budget** | 16.67ms (60 FPS) |
| **Visualization budget** | <4ms (24% of frame) |
| **Typical SpriteBatch draw calls** | 1,000-5,000 per frame (batched) |
| **Per-draw-call overhead** | ~0.5-2 μs (batched), ~10-50 μs (unbatched) |

> "I call SpriteBatch.Begin() 130 times per frame when next to lots of water..."
> — [MonoGame Community](https://community.monogame.net/t/carry-more-information-per-spritebatch-draw/20116)

> "...SpriteBatch.Draw() calls PER FRAME without any noticeable loss in performance (held 60FPS with no issues; this was using SpriteSortMode..."
> — [Capital G Studios](https://capitalgstudios.wordpress.com/2019/01/03/spritebatch-vs-vertexbuffer/)

### Draw Call Overheads in MonoGame

1. **Batched draws (same texture, same Begin/End block):** Very cheap — SpriteBatch batches up to 10,000 vertices (default buffer size) before flushing to GPU.

2. **Texture switches:** Each texture change forces a flush. Minimize by using texture atlases.

3. **Begin/End blocks:** Each `SpriteBatch.Begin()` + `SpriteBatch.End()` pair incurs overhead. Use as few as possible.

4. **BlendState/SamplerState changes:** Force a flush. Group draws by blend state.

### Practical Budget for Tile Overlays

For a typical Stardew Valley farm screen showing ~100-200 tiles:

- **200 tile overlays** = 200 `SpriteBatch.Draw()` calls
- If batched in a single Begin/End block: **<0.5ms**
- If each overlay uses a different color but same texture: **still <1ms** (color is a parameter, not a state change)
- **Conclusion:** 200-500 overlay draws per frame is easily achievable within the 4ms budget

### Performance Tips

1. **Use a single 1x1 white texture** for all colored overlays — no texture switches
2. **Batch all overlay draws** in one `SpriteBatch.Begin()`/`End()` block
3. **Avoid per-pixel `SetData()` calls** at runtime — create textures once at load time
4. **Cache interpolated colors** — don't recompute `Color.Lerp()` every frame for static health values
5. **Only redraw when health changes** — use dirty flags to skip rendering unchanged tiles

> Source: [MonoGame Performance Cheat Sheet](https://konradzaba.github.io/blog/tech/Monogame-and-XNA-performance-cheat-sheet-Draw-function/)

---

## 7. Color Values and Gamma Correction

### Are Hex Values sRGB?

**Yes.** Hex color values like `#B91C1C`, `#D97706`, `#15803D` are **sRGB** values. This is the standard color space for web colors, digital art, and most image editing software.

### Mapping to XNA Color

XNA/MonoGame's `Color` struct uses **0-255 byte values** that map directly to sRGB hex values:

| Hex | R | G | B | XNA Color |
|-----|---|---|---|-----------|
| `#B91C1C` | 185 | 28 | 28 | `new Color(185, 28, 28)` |
| `#D97706` | 217 | 119 | 6 | `new Color(217, 119, 6)` |
| `#15803D` | 21 | 128 | 61 | `new Color(21, 128, 61)` |

The mapping is **direct and unambiguous** — hex `B9` = decimal `185` = XNA `R` value.

### Gamma Correction Concerns

**This is a nuanced topic.** Here's what matters for a Stardew Valley mod:

1. **XNA/MonoGame's default behavior:** The framework does **NOT** perform automatic gamma correction by default. Colors are treated as raw sRGB values and displayed directly.

2. **The problem:** If you interpolate in sRGB space (which `Color.Lerp` does), the interpolation is technically incorrect because sRGB is gamma-compressed. Linear RGB interpolation should ideally happen in **linear space** (after converting sRGB → linear, interpolating, then converting back).

3. **Practical impact for a Stardew Valley mod:**
   - For **health band overlays**, the visual difference between sRGB interpolation and linear-space interpolation is **subtle** and unlikely to be noticeable in gameplay.
   - The "muddy intermediate" problem from RGB interpolation (Section 1) is a bigger visual issue than gamma correction.
   - Stardew Valley itself uses sRGB textures and does not perform gamma correction in its rendering pipeline.

4. **If you want gamma-correct interpolation:**
   ```csharp
   public static Color LerpLinear(Color c1, Color c2, float t)
   {
       // Convert sRGB to linear, interpolate, convert back
       float R1 = SrgbToLinear(c1.R / 255f);
       float G1 = SrgbToLinear(c1.G / 255f);
       float B1 = SrgbToLinear(c1.B / 255f);

       float R2 = SrgbToLinear(c2.R / 255f);
       float G2 = SrgbToLinear(c2.G / 255f);
       float B2 = SrgbToLinear(c2.B / 255f);

       float R = LinearToSRGB(R1 + (R2 - R1) * t);
       float G = LinearToSRGB(G1 + (G2 - G1) * t);
       float B = LinearToSRGB(B1 + (B2 - B1) * t);

       return new Color(
           (int)(R * 255),
           (int)(G * 255),
           (int)(B * 255));
   }

   private static float SrgbToLinear(float c) =>
       c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);

   private static float LinearToSRGB(float c) =>
       c <= 0.0031308f ? c * 12.92f : 1.055f * (float)Math.Pow(c, 1f / 2.4f) - 0.055f;
   ```

> "In Gamma color space rendering, the sRGB setting doesn't do anything. A color channel value of 127/255 is ~0.5 (actually 0.498 and change), and..."
> — [Unity Forums](https://discussions.unity.com/t/understanding-srgb-and-gamma-corrected-values-in-the-render-pipeline/766833)

> "Converts from linear RGB space to sRGB: `pow(color, 1/2.2f)`. Converts from sRGB space to linear RGB: `pow(color, 2.2f)`."
> — [Correcting XNA's Gamma Correction](https://therealmjp.github.io/posts/correcting-xnas-gamma-correction/)

### Recommendation

For the Living Roots mod:
- **Use direct sRGB → XNA Color mapping** (hex values map directly to 0-255 bytes)
- **Do NOT implement gamma correction** unless you notice visible banding or color inaccuracies — the added complexity is not worth it for a gameplay overlay
- **If you want better interpolation quality**, implement the `LerpLinear` function above — it provides smoother transitions without the muddy gray zone

---

## Summary of Recommendations

| Topic | Recommendation |
|-------|---------------|
| **Color Interpolation** | Use `Color.Lerp` for similar hues; consider HSL hue interpolation or LAB for wide hue ranges |
| **XNA Color** | Use `new Color(r, g, b)` with 0-255 ints; write a `FromHex()` helper |
| **Alpha Blending** | Use `BlendState.AlphaBlend` with `SamplerState.PointClamp`; use discrete alpha levels (25/50/75%) |
| **Texture Patterns** | Create white pattern textures once; tint at draw time with `color * opacity` |
| **Pixel-Perfect** | Use `SamplerState.PointClamp` + integer positions; round camera to integers |
| **Performance** | Batch all overlays in one Begin/End block; use 1x1 white texture; budget allows 200-500 draws |
| **Color Values** | Hex maps directly to 0-255; skip gamma correction for gameplay overlays |

---

## Sources

1. [MonoGame Color Struct Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Color.html)
2. [MonoGame Color.cs Source Code (GitHub)](https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Color.cs)
3. [MonoGame BlendState Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BlendState.html)
4. [MonoGame Texture Sampling Tutorial](https://docs.monogame.net/articles/tutorials/building_2d_games/18_texture_sampling/index.html)
5. [MonoGame SamplerState Blog Post](https://infinitespace-studios.co.uk/general/monogame-spritebatch-sampler-states)
6. [Correcting XNA's Gamma Correction](https://therealmjp.github.io/posts/correcting-xnas-gamma-correction/)
7. [Wikipedia: HSL and HSV](https://en.wikipedia.org/wiki/HSL_and_HSV)
8. [Perceptually Uniform Color Spaces](https://programmingdesignsystems.com/color/perceptually-uniform-color-spaces/)
9. [StackOverflow: Convert HEX to XNA Color](https://stackoverflow.com/questions/70593211/)
10. [StackOverflow: Convert HEX to .NET Color](https://stackoverflow.com/questions/2109756/how-do-i-get-the-color-from-a-hexadecimal-color-code-using-net)
11. [MonoGame Performance Cheat Sheet](https://konradzaba.github.io/blog/tech/Monogame-and-XNA-performance-cheat-sheet-Draw-function/)
12. [GameDev StackExchange: SpriteBatch.SetData black texture](https://gamedev.stackexchange.com/questions/163173/monogame-xna-texture2d-setdata-results-in-black-texture)
13. [GameDev StackExchange: Pixel-perfect rendering](https://gamedev.stackexchange.com/questions/45365/sprites-rendering-blurry-with-velocity)
14. [MonoGame Community: SpriteBatch draw calls](https://community.monogame.net/t/carry-more-information-per-spritebatch-draw/20116)
15. [MonoGame Community: Custom BlendState](https://community.monogame.net/t/custom-blendstate-how-do-color-and-alphablend-mix/10717)
16. [Mysterious Space: Pixel Shaders in MonoGame](https://mysteriousspace.com/2019/01/05/pixel-shaders-in-monogame-a-tutorial-of-sorts-for-2019/)
17. [Capital G Studios: SpriteBatch vs VertexBuffer](https://capitalgstudios.wordpress.com/2019/01/03/spritebatch-vs-vertexbuffer/)
18. [Hacker News: Lab color space](https://news.ycombinator.com/item?id=21030051)
19. [arXiv: Color Models in Image Processing](https://arxiv.org/html/2510.00584v1)
20. [GameDev StackExchange: AlphaBlend in XNA 4](https://gamedev.stackexchange.com/questions/64190/how-alphablend-blendstate-works-in-xna-4-when-accumulighting-light-into-a-render)
21. [Demofox: Pre-multiplied Alpha](https://blog.demofox.org/2015/06/19/what-is-pre-multiplied-alpha-and-why-does-it-matter/)
22. [MonoGame GitHub: SpriteBatch floating point issue #2978](https://github.com/MonoGame/MonoGame/issues/2978)
23. [MonoGame GitHub: BlendState alpha issue #6978](https://github.com/MonoGame/MonoGame/issues/6978)
24. [SDV-Radiance mod performance notes](https://www.nexusmods.com/stardewvalley/mods/49397)
