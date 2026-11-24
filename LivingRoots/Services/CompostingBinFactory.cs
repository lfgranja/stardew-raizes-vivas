using LivingRoots.Domain.Models;

namespace LivingRoots.Services;

public class CompostingBinFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API retained for test and DI compatibility.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarAnalyzer.CSharp", "S2325:Make method static", Justification = "Instance API retained for test and DI compatibility.")]
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
