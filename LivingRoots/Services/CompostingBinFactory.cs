using LivingRoots.Domain.Models;

namespace LivingRoots.Services;

public class CompostingBinFactory
{
    public CompostingBinStateModel CreateBin(int tileX, int tileY)
    {
        return new CompostingBinStateModel
        {
            TileX = tileX,
            TileY = tileY,
            State = Domain.CompostingBinState.Empty,
            MaturationLevel = 1,
            ConsecutiveIdleDays = 0
        };
    }
}
