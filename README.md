# Living Roots

A Stardew Valley mod that introduces sustainable farming practices through soil health management, composting, and agroecological principles.

## Features

### Soil Health System
- Each farm tile has a persistent soil health value (0-100%)
- Health values are saved and loaded with the game
- **NEW: Visual indicators show soil health status** - See soil health through hover tooltips, color-coded tile overlays, and hoe action feedback
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
4. Configure the mod (optional - see Configuration section below)
5. Start playing!

## Configuration

The mod can be customized through the `config.json` file in the mod folder. Key options include:

- **ShowTileOverlays**: Enable/disable color-coded tile overlays
- **ShowHoverTooltips**: Enable/disable hover tooltips showing soil health
- **ShowHoeFeedback**: Enable/disable visual feedback when using hoe
- **OverlayOpacity**: Adjust transparency of tile overlays (0.0-1.0)
- **Custom Colors**: Customize colors for Poor/Moderate/Healthy soil

Example configuration:
```json
{
  "ShowTileOverlays": true,
  "ShowHoverTooltips": true,
  "ShowHoeFeedback": true,
  "OverlayOpacity": 0.3,
  "PoorHealthColor": { "R": 139, "G": 69, "B": 19, "A": 255 },
  "ModerateHealthColor": { "R": 218, "G": 165, "B": 32, "A": 255 },
  "HealthyHealthColor": { "R": 85, "G": 107, "B": 47, "A": 255 }
}
```

For detailed configuration options, see the [Soil Health Visualization User Guide](LivingRoots/docs/SOIL_HEALTH_VISUALIZATION_USER_GUIDE.md).

## Development

This mod follows Domain-Driven Design (DDD) principles and implements a layered architecture with clear separation of concerns. The soil health system uses a robust persistence mechanism that ensures data integrity across game sessions.

### Architecture Highlights
- **Domain Layer**: Contains business logic and models
- **Services Layer**: Implements application services
- **Controllers Layer**: Handles game events
- **Test-Driven Development**: Comprehensive test coverage

### Documentation
- [Soil Health Visualization User Guide](LivingRoots/docs/SOIL_HEALTH_VISUALIZATION_USER_GUIDE.md) - Complete user documentation for visualization features
- [Soil Health Visualization Developer Guide](LivingRoots/docs/SOIL_HEALTH_VISUALIZATION_DEVELOPER_GUIDE.md) - Technical documentation for developers
- [Implementation Guide: US-01-02](LivingRoots/docs/IMPLEMENTATION_GUIDE_US-01-02.md) - Detailed implementation plan
- [Feature Summary: US-01-02](LivingRoots/docs/FEATURE_SUMMARY_US-01-02.md) - Feature overview and completion status
- [Epics and User Stories](LivingRoots/docs/EPICS_USER_STORIES.md) - Project planning and status
- [Architecture Documentation](LivingRoots/docs/ARCHITECTURE.md) - Overall system architecture

## Contributing

We welcome contributions! Please see our [contribution guidelines](CONTRIBUTING.md) for more information.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
