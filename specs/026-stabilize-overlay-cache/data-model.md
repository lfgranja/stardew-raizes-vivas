# Data Model: Stabilize overlay cache invalidation property

No persisted data. The property operates on these in-memory observations:

| Entity | Fields | Rules |
|--------|--------|-------|
| InterleavingEvent | kind ∈ {mutate, config-change, disable, enable}; raw int | `Mod(raw, 4)` selects kind; max 24 events per generated case |
| VisibleTile | `Point` in a 4×3 grid | 12 tiles; all inside the fixed viewport |
| HealthSnapshot | `Dictionary<Point, float>` | fresh instance per mutation; warm health 50 f; mutations in [1,49] |
| Consultation | `float` health passed to `GetCategoryForHealth` | exactly one per visible tile on a rebuild; zero while disabled |
| FrameWorkBound | `int` consultations in one frame | must be ≤ 12 (VisibleTileCount) |

State transitions: disabled frames never consult; a re-enable clears the cache and forces a rebuild; a mutation/config change marks the next frame as state-changed. These transitions are preserved unchanged.
