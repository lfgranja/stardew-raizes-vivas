using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// 017 RED-phase scaffold. Behavior is implemented in T026-T027.
/// </summary>
public class AgrochemicalApplicationService(ISoilHealthService soilHealthService) : IAgrochemicalApplicationService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));

    public AgrochemicalApplicationResult Apply(GameLocation location, Vector2 tile)
    {
        _ = _soilHealthService;
        throw new NotImplementedException("017 RED scaffold: agrochemical service behavior is not implemented.");
    }
}
