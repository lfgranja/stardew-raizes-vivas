# Living Roots

A Stardew Valley mod that introduces sustainable farming practices through soil health management, composting, and agroecological principles.

## Features

### Soil Health System
- Each farm tile has a persistent soil health value (0-100%)
- Health values are saved and loaded with the game
- Visual indicators show soil health status
- Health degrades over time when soil is left bare
- Health improves with compost application

### Composting
- Create compost from organic materials
- Composting bin: craft, place, load with organic waste, harvest compost after 2 in-game days
- Apply compost to improve soil health

### Agroecological Practices
- Mulch soil cover: reduces/prevents soil decay on mulched tilled tiles
- Soil health influences crop growth speed
- Harvest quality scales with soil health tiers (Cornell CSHA 0–30 / 31–70 / 71–100)
- Joja agrochemical tradeoff item: bounded, recoverable soil-health cost with explicit feedback
- Educational elements and broader agroecological arcs (Planned)

> **Status (2026-11-10):** features above are implemented and covered by automated tests (**1997 passed / 4 skipped / 0 failed**). End-to-end gameplay verification is `machine-pass` (headless, host-only) for seven scenarios — compost, growth, mulch, harvest quality, Harmony seam, Joja tradeoff, and heirloom planting (**planting-only, not harvest/seed-save/natural acquisition**) — **not** human-verified. Live multiplayer convergence (D1) is not attested; no public release or git tag is published yet — the repository ships v0.1.0 internally; build from source. Release gate on HEAD `c07fbab` re-ran 2026-11-10T02:40Z (**ALL STEPS PASSED**). Status and 2026-11-10 freshness: `research/2026-11-10-progress-and-gaps.md`.

## Installation

> There is no public download yet. The `0.1.0` version lives in the repository only; build it from source.

1. Install [SMAPI](https://smapi.io/) 4.5.2 or later (requires Stardew Valley **1.6.14+**).
2. Clone this repository.
3. Build the mod (set `GamePath` if your game is not at the csproj default):
   ```bash
   dotnet build Stardew-LivingRoots.sln --configuration Release -m:1
   ```
4. Copy `LivingRoots/bin/LivingRoots 0.1.0.zip` into your Stardew Valley `Mods` folder and extract it, **or** copy the unpacked contents of `LivingRoots/bin/` into `Stardew Valley/Mods/LivingRoots/`.
5. Start the game through SMAPI.

## Development

This mod follows Domain-Driven Design (DDD) principles and implements a layered architecture with clear separation of concerns. The soil health system uses a robust persistence mechanism that ensures data integrity across game sessions.

### Architecture Highlights
- **Domain Layer**: Contains business logic and models
- **Services Layer**: Implements application services
- **Controllers Layer**: Handles game events
- **Test-Driven Development**: Comprehensive test coverage

## Contributing

We welcome contributions! Please see our [contribution guidelines](CONTRIBUTING.md) for more information.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
