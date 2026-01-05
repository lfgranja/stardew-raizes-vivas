# Living Roots

A Stardew Valley mod that introduces sustainable farming practices through soil health management, composting, and agroecological principles.

## Features

### Soil Health System
- Each farm tile has a persistent soil health value (0-100%)
- Health values are saved and loaded with the game
- Visual indicators show soil health status (Planned)
- Health degrades over time when soil is left bare (Planned)
- Health improves with compost application (Planned)

### Composting
- Create compost from organic materials
- Apply compost to improve soil health
- Different materials provide different benefits

### Agroecological Practices
- Implement sustainable farming techniques
- Learn about real-world soil management
- Enhance your farming experience with educational elements

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
