# Contract: IVisualizationService

**Domain**: LivingRoots.Domain
**Implementation**: LivingRoots.Services.Visualization.VisualizationService

## Interface Definition

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Orchestrates rendering of soil health visualization overlays, tooltips, and hoe feedback.
    /// Called from game loop render events.
    /// </summary>
    public interface IVisualizationService
    {
        /// <summary>
        /// Renders soil health overlays for all visible tilled tiles.
        /// Called after game world rendering completes.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="viewport">Current viewport bounds in tile coordinates</param>
        /// <param name="gameTime">Current game time for animation</param>
        void RenderOverlays(SpriteBatch spriteBatch, Rectangle viewport, GameTime gameTime);

        /// <summary>
        /// Renders hover tooltip if cursor is over a tilled soil tile.
        /// Called after overlay rendering.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="cursorPosition">Current cursor position in screen coordinates</param>
        /// <param name="gameTime">Current game time</param>
        void RenderTooltip(SpriteBatch spriteBatch, Vector2 cursorPosition, GameTime gameTime);

        /// <summary>
        /// Renders hoe action feedback effects.
        /// Called after tooltip rendering.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="gameTime">Current game time for animation</param>
        void RenderHoeFeedback(SpriteBatch spriteBatch, GameTime gameTime);

        /// <summary>
        /// Triggers hoe feedback for a specific tile.
        /// Called when hoe action is detected.
        /// </summary>
        /// <param name="tilePosition">Tile coordinates targeted by hoe</param>
        /// <param name="healthValue">Current health value of the tile</param>
        void TriggerHoeFeedback(Point tilePosition, float healthValue);

        /// <summary>
        /// Invalidates cached overlay data, forcing recalculation on next render.
        /// Called when soil health data changes.
        /// </summary>
        void InvalidateCache();
    }
}
```

## Preconditions
- SpriteBatch is in valid state (Begin() called before, End() after)
- Viewport coordinates are in tile space (not pixel space)
- GameTime provides ElapsedGameTime for animation calculations

## Postconditions
- Overlays rendered for all visible tilled tiles within viewport
- Tooltip rendered only when cursor is over a valid tile with tooltips enabled
- Hoe feedback effects rendered with correct fade-out animation
- Cache invalidated when InvalidateCache() called

## Error Handling
- Null dependencies throw ArgumentNullException at construction
- Invalid viewport dimensions logged and skipped
- Missing soil health data renders Unknown color overlay

## Performance Constraints
- Total render time < 16.67ms for 1,000 visible tiles
- No allocations in render methods (use cached resources)
- Viewport culling must reduce visible tiles to screen-sized subset
