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

> **Status (2026-11-09):** features above are implemented and covered by automated tests; end-to-end gameplay verification is `machine-pass` (headless, host-only), **not** human-verified. Live multiplayer convergence (D1) is not yet attested. No public release is published yet — the repository ships v0.1.0 internally; install from source.

## Installation

1. Install [SMAPI](https://smapi.io/)
2. Download the latest version of LivingRoots from [GitHub Releases](https://github.com/lfgranja/stardew-raizes-vivas/releases) page
3. Extract to your Stardew Valley `Mods` folder
4. Start playing!

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
